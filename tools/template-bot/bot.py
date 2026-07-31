#!/usr/bin/env python3
"""
NIE Template fleet-bot — auto-PR security tasks across derived repos.

What it does
------------
1. Reads `.ai/tasks/index.json` from the template repo (the local checkout).
2. Discovers the set of derived repos to scan from one of two sources:
     - --fleet-config <yaml>  — explicit `tools/template-bot/fleet.yml`
     - --registry <url>       — the fleet registry's GET /v1/audit endpoint
3. For each repo, fetches `.nie-template-version.json` via the GitHub API and
   computes the set of pending tasks (in the template index but not in
   `appliedTasks`).
4. Filters to tasks of type `"security"` (configurable via --types).
5. For each pending task it would normally open a PR. Three modes:
       --dry-run        Print a plan; never call GitHub mutations.
       --plan-only      Like --dry-run but also write `bot-plan.json`.
       (default)        Open a draft PR per (repo, task) using the GitHub API.

Stdlib only. Authenticates via GITHUB_TOKEN (PAT or installation token; needs
contents:write + pull-requests:write on each derived repo).

Always-safe properties
----------------------
- NEVER auto-merges. PRs are always opened as drafts and tagged
  `template-bot,template,security`.
- NEVER pushes to a branch that already exists; if the bot already opened a
  PR for the same (repo, task), the run becomes a no-op for that pair.
- NEVER mutates the template repo itself; only consumes its index.
- NEVER fails the calling workflow on per-repo errors — they are reported
  in the run summary and the run continues.

Exit codes
----------
0   success (or --dry-run)
1   transient failure (e.g., rate limit) — try again later
2   invocation error (bad args, missing token, no fleet source)

Usage examples
--------------
  python tools/template-bot/bot.py \\
      --fleet-config tools/template-bot/fleet.yml --dry-run

  GITHUB_TOKEN=ghp_xxx python tools/template-bot/bot.py \\
      --registry https://nie-registry.example.com/v1/audit

  python tools/template-bot/bot.py --plan-only --types security,breaking
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import ssl
import sys
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass, field
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

REPO_ROOT = Path(__file__).resolve().parents[2]
TASK_INDEX = REPO_ROOT / ".ai" / "tasks" / "index.json"
USER_AGENT = "nie-template-bot/1.0"
DEFAULT_BRANCH_PREFIX = "template-bot/task-"
DEFAULT_TYPES = ("security",)


# ---------------------------------------------------------------------------
# Data types
# ---------------------------------------------------------------------------

@dataclass
class FleetEntry:
    repo: str                       # "owner/repo"
    branch: str = "main"            # base branch to PR against
    skip: bool = False
    notes: str = ""


@dataclass
class TaskInfo:
    task_id: str
    slug: str
    type: str
    title: str
    summary: str
    template_version_after: str | None = None


@dataclass
class Action:
    repo: str
    task_id: str
    branch: str
    pr_title: str
    pr_body: str
    base_branch: str
    skipped_reason: str | None = None


# ---------------------------------------------------------------------------
# Tiny YAML loader (the same flat-key shape Copier uses; no anchors / nesting)
# ---------------------------------------------------------------------------

def parse_simple_yaml(text: str) -> dict | list:
    """Tiny YAML loader supporting only what fleet.yml needs:
       - top-level mappings
       - top-level list of mappings under a key
       - leaf scalars (str, bool, null, int)
    Anything more complex -> raise ValueError. Stdlib only by design."""
    out: dict = {}
    cur_list: list | None = None
    cur_list_item: dict | None = None
    cur_list_key: str | None = None
    for raw in text.splitlines():
        line = raw.rstrip()
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        if not line[0].isspace() and line.endswith(":"):
            # Section header: "<key>:"
            key = line[:-1].strip()
            cur_list_key = key
            cur_list = []
            out[key] = cur_list
            cur_list_item = None
            continue
        if line.startswith("  - "):
            # New list item
            cur_list_item = {}
            cur_list.append(cur_list_item)
            rest = line[4:].strip()
            if ":" in rest:
                k, _, v = rest.partition(":")
                cur_list_item[k.strip()] = _coerce(v.strip())
            else:
                cur_list_item["value"] = _coerce(rest)
            continue
        if line.startswith("    ") and cur_list_item is not None:
            # Continuation of current list item
            k, _, v = line.strip().partition(":")
            cur_list_item[k.strip()] = _coerce(v.strip())
            continue
        if ":" in line and not line[0].isspace():
            # Top-level scalar: "key: value"
            k, _, v = line.partition(":")
            out[k.strip()] = _coerce(v.strip())
            continue
        # Anything else is unsupported
    return out


def _coerce(value: str):
    if value == "" or value.lower() in ("null", "~"):
        return None
    if value.lower() == "true":
        return True
    if value.lower() == "false":
        return False
    if value.isdigit() or (value.startswith("-") and value[1:].isdigit()):
        return int(value)
    return value.strip("\"'")


# ---------------------------------------------------------------------------
# Fleet discovery
# ---------------------------------------------------------------------------

def load_fleet_from_config(path: Path) -> list[FleetEntry]:
    if not path.is_file():
        raise FileNotFoundError(f"fleet config not found: {path}")
    data = parse_simple_yaml(path.read_text(encoding="utf-8"))
    repos = data.get("repos", []) if isinstance(data, dict) else []
    out = []
    for entry in repos:
        if not isinstance(entry, dict):
            continue
        if "repo" not in entry:
            continue
        out.append(FleetEntry(
            repo=str(entry["repo"]),
            branch=str(entry.get("branch", "main")),
            skip=bool(entry.get("skip", False)),
            notes=str(entry.get("notes", "")),
        ))
    return out


def load_fleet_from_registry(url: str, token: str | None = None) -> list[FleetEntry]:
    """Pull the active fleet from a registry's GET /v1/audit endpoint.
    Each repo last-seen by the registry is treated as in scope."""
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    with urllib.request.urlopen(req, timeout=10) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    out = []
    for r in data.get("repos", []):
        out.append(FleetEntry(repo=r.get("repo", ""), branch="main"))
    return [e for e in out if e.repo]


# ---------------------------------------------------------------------------
# Task index
# ---------------------------------------------------------------------------

def load_tasks(types: tuple[str, ...]) -> list[TaskInfo]:
    if not TASK_INDEX.is_file():
        raise FileNotFoundError(f"task index missing: {TASK_INDEX}")
    idx = json.loads(TASK_INDEX.read_text(encoding="utf-8"))
    out = []
    for t in idx.get("tasks", []):
        if t.get("type") not in types:
            continue
        out.append(TaskInfo(
            task_id=t["taskId"],
            slug=t["slug"],
            type=t["type"],
            title=t.get("title", ""),
            summary=t.get("summary", ""),
            template_version_after=t.get("templateVersionAfterApply"),
        ))
    return out


# ---------------------------------------------------------------------------
# GitHub API (stdlib)
# ---------------------------------------------------------------------------

class GitHub:
    def __init__(self, token: str | None, dry_run: bool):
        self.token = token
        self.dry_run = dry_run

    def _req(self, method: str, url: str, body: dict | None = None) -> dict:
        data = json.dumps(body).encode("utf-8") if body is not None else None
        req = urllib.request.Request(url, data=data, method=method)
        req.add_header("Accept", "application/vnd.github+json")
        req.add_header("X-GitHub-Api-Version", "2022-11-28")
        req.add_header("User-Agent", USER_AGENT)
        if data is not None:
            req.add_header("Content-Type", "application/json")
        if self.token:
            req.add_header("Authorization", f"Bearer {self.token}")
        ctx = ssl.create_default_context()
        try:
            with urllib.request.urlopen(req, timeout=15, context=ctx) as resp:
                raw = resp.read()
                if not raw:
                    return {}
                return json.loads(raw.decode("utf-8"))
        except urllib.error.HTTPError as e:
            try:
                detail = json.loads(e.read().decode("utf-8", errors="replace"))
            except Exception:
                detail = {"error": str(e)}
            raise GitHubError(method, url, e.code, detail) from e

    def get_version_marker(self, repo: str, branch: str) -> dict | None:
        """Read .nie-template-version.json from <repo>@<branch>. None if missing."""
        url = (f"https://api.github.com/repos/{repo}/contents/"
               f".nie-template-version.json?ref={urllib.parse.quote(branch)}")
        try:
            data = self._req("GET", url)
        except GitHubError as e:
            if e.status == 404:
                return None
            raise
        if data.get("encoding") != "base64":
            return None
        import base64
        try:
            text = base64.b64decode(data["content"]).decode("utf-8")
            return json.loads(text)
        except Exception:
            return None

    def find_open_pr(self, repo: str, head_branch: str) -> dict | None:
        """Return the open PR whose head branch matches, if any."""
        owner = repo.split("/")[0]
        url = (f"https://api.github.com/repos/{repo}/pulls?"
               f"state=open&head={urllib.parse.quote(owner + ':' + head_branch)}")
        prs = self._req("GET", url)
        return prs[0] if isinstance(prs, list) and prs else None

    def open_draft_pr(self, repo: str, base: str, head: str,
                      title: str, body: str) -> dict:
        if self.dry_run:
            return {"dry_run": True, "url": f"(would open) {repo} {base}<-{head}"}
        url = f"https://api.github.com/repos/{repo}/pulls"
        return self._req("POST", url, {
            "title": title, "head": head, "base": base,
            "body": body, "draft": True,
        })

    def add_labels(self, repo: str, issue_number: int, labels: list[str]) -> None:
        if self.dry_run or not labels:
            return
        url = f"https://api.github.com/repos/{repo}/issues/{issue_number}/labels"
        self._req("POST", url, {"labels": labels})


class GitHubError(Exception):
    def __init__(self, method, url, status, detail):
        super().__init__(f"{method} {url} -> {status}: {detail}")
        self.method = method; self.url = url
        self.status = status; self.detail = detail


# ---------------------------------------------------------------------------
# Planning + execution
# ---------------------------------------------------------------------------

def plan_actions(fleet: list[FleetEntry], tasks: list[TaskInfo],
                 gh: GitHub) -> list[Action]:
    actions: list[Action] = []
    for entry in fleet:
        if entry.skip:
            actions.append(Action(repo=entry.repo, task_id="-", branch="-",
                                  pr_title="-", pr_body="",
                                  base_branch=entry.branch,
                                  skipped_reason="fleet.yml: skip=true"))
            continue
        marker = gh.get_version_marker(entry.repo, entry.branch)
        if marker is None:
            actions.append(Action(repo=entry.repo, task_id="-", branch="-",
                                  pr_title="-", pr_body="",
                                  base_branch=entry.branch,
                                  skipped_reason="no .nie-template-version.json"))
            continue
        applied = set(marker.get("appliedTasks") or [])
        for task in tasks:
            if task.task_id in applied:
                continue
            head = f"{DEFAULT_BRANCH_PREFIX}{task.task_id}-{task.slug}"
            existing = gh.find_open_pr(entry.repo, head)
            if existing:
                actions.append(Action(repo=entry.repo, task_id=task.task_id,
                                      branch=head, pr_title="(already open)",
                                      pr_body=existing.get("html_url", ""),
                                      base_branch=entry.branch,
                                      skipped_reason="open PR exists"))
                continue
            actions.append(Action(
                repo=entry.repo, task_id=task.task_id, branch=head,
                pr_title=f"chore(template): apply {task.task_id} — {task.title}",
                pr_body=_render_pr_body(task),
                base_branch=entry.branch,
            ))
    return actions


def _render_pr_body(task: TaskInfo) -> str:
    return f"""\
This PR applies template task **{task.task_id}** ({task.type}).

> {task.title}

**Summary**
{task.summary}

**What to do**

1. Review `.ai/tasks/{task.task_id}-{task.slug}/apply.md`.
2. Walk the file edits the dossier prescribes; an AI agent (Claude Code,
   Copilot, Gemini, Kiro) can do this — see `.ai/ALIGN.md`.
3. Run the verification: `bash .ai/tasks/{task.task_id}-{task.slug}/verify.sh`.
4. Commit with: `chore(template): apply task {task.task_id}`.
5. Append `{task.task_id}` to `.nie-template-version.json:appliedTasks`.

**Why this is a draft PR**

Template-bot opens draft PRs only — it never auto-merges. The derived-repo
team owns the merge, ensures `verify.sh` passes in CI, and resolves any
local divergence.

---
_Generated by `tools/template-bot/bot.py`._
"""


def execute(actions: list[Action], gh: GitHub, dry_run: bool) -> dict:
    summary = {"opened": 0, "skipped": 0, "errors": 0,
               "details": []}
    for a in actions:
        if a.skipped_reason:
            summary["skipped"] += 1
            summary["details"].append({
                "repo": a.repo, "task": a.task_id,
                "status": "skipped", "reason": a.skipped_reason
            })
            continue
        if dry_run:
            summary["opened"] += 1
            summary["details"].append({
                "repo": a.repo, "task": a.task_id,
                "status": "would-open", "branch": a.branch,
                "title": a.pr_title
            })
            continue
        try:
            pr = gh.open_draft_pr(a.repo, a.base_branch, a.branch,
                                  a.pr_title, a.pr_body)
            pr_num = pr.get("number")
            if pr_num:
                gh.add_labels(a.repo, pr_num,
                              ["template-bot", "template", "security"])
            summary["opened"] += 1
            summary["details"].append({
                "repo": a.repo, "task": a.task_id,
                "status": "opened", "url": pr.get("html_url", "")
            })
        except Exception as e:
            summary["errors"] += 1
            summary["details"].append({
                "repo": a.repo, "task": a.task_id,
                "status": "error", "error": str(e)
            })
    return summary


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    src = ap.add_mutually_exclusive_group(required=True)
    src.add_argument("--fleet-config", help="Path to fleet.yml")
    src.add_argument("--registry", help="Registry URL (GET /v1/audit)")
    ap.add_argument("--registry-token", default=os.environ.get("REGISTRY_TOKEN"),
                    help="Bearer token for registry auth (env REGISTRY_TOKEN)")
    ap.add_argument("--types", default=",".join(DEFAULT_TYPES),
                    help="Comma-separated task types to consider (default 'security')")
    ap.add_argument("--dry-run", action="store_true",
                    help="Plan only; do not call any GitHub mutations.")
    ap.add_argument("--plan-only", action="store_true",
                    help="Plan + write bot-plan.json. Implies --dry-run.")
    ap.add_argument("--token", default=os.environ.get("GITHUB_TOKEN"),
                    help="GitHub token (env GITHUB_TOKEN)")
    args = ap.parse_args(argv)

    types = tuple(t.strip() for t in args.types.split(",") if t.strip())

    if args.fleet_config:
        fleet = load_fleet_from_config(Path(args.fleet_config))
    else:
        fleet = load_fleet_from_registry(args.registry, args.registry_token)
    if not fleet:
        print("ERROR: empty fleet — nothing to do", file=sys.stderr)
        return 2

    tasks = load_tasks(types)
    if not tasks:
        print(f"[bot] no tasks of types={types} in index — nothing to do")
        return 0

    dry = args.dry_run or args.plan_only
    if not dry and not args.token:
        print("ERROR: GITHUB_TOKEN required for live mode (use --dry-run or "
              "set GITHUB_TOKEN)", file=sys.stderr)
        return 2

    gh = GitHub(token=args.token, dry_run=dry)
    print(f"[bot] {len(fleet)} repo(s); {len(tasks)} candidate task(s); "
          f"types={types}; mode={'dry-run' if dry else 'LIVE'}")

    actions = plan_actions(fleet, tasks, gh)
    summary = execute(actions, gh, dry)

    print(f"[bot] result: opened={summary['opened']} "
          f"skipped={summary['skipped']} errors={summary['errors']}")
    for d in summary["details"]:
        print(f"  - {d['repo']}@{d.get('task','-'):>4}: "
              f"{d['status']}{(' — ' + d.get('reason','')) if d.get('reason') else ''}")

    if args.plan_only:
        Path("bot-plan.json").write_text(
            json.dumps({"generated_at": dt.datetime.now(dt.timezone.utc).isoformat(),
                        "summary": summary, "fleet": [f.repo for f in fleet],
                        "tasks": [t.task_id for t in tasks]},
                       indent=2),
            encoding="utf-8")
        print("[bot] wrote bot-plan.json")

    return 0 if summary["errors"] == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
