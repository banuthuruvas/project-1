#!/usr/bin/env python3
"""
Post-copy / post-update alignment for NIE Template derived repos.

Runs as a Copier _task after every `copier copy` and `copier update`. Two jobs:

  1. Initialise / refresh `.nie-template-version.json` so the derived repo
     records which template version it's aligned with.
  2. Discover any tasks in `.ai/tasks/index.json` that are not yet recorded in
     `appliedTasks` and report them — non-blocking, just informational. The
     actual task application is delegated to the AI-driven .ai/ALIGN.md flow
     (which knows how to walk apply.md interactively).

Stdlib only — no dependencies.

Usage:
  python tools/template-align/align.py            # full report
  python tools/template-align/align.py --quiet    # only print non-empty actions
  python tools/template-align/align.py --json     # machine-readable output
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

# Windows consoles default to cp1252; force UTF-8 so Unicode in task titles
# (e.g. "HTML→PDF") doesn't crash the script.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

REPO_ROOT = Path.cwd()
VERSION_FILE = REPO_ROOT / ".nie-template-version.json"
TASK_INDEX = REPO_ROOT / ".ai" / "tasks" / "index.json"


def _load_json(path: Path) -> dict | None:
    if not path.is_file():
        return None
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as e:
        print(f"ERROR: {path} is not valid JSON ({e})", file=sys.stderr)
        return None


def _save_json(path: Path, data: dict) -> None:
    # ensure_ascii=True matches the .NET serializer's `+`-style output, so
    # running align.py in the template repo doesn't churn the marker file.
    path.write_text(json.dumps(data, indent=2, ensure_ascii=True) + "\n",
                    encoding="utf-8")


def _is_template_repo() -> bool:
    """We are in the template repo (not a derived one) when the canonical
    template-only artefacts exist. Used to skip marker mutations that only
    make sense in derived repos."""
    return (REPO_ROOT / "docs" / "template-releases" / "index.json").is_file() \
        and (REPO_ROOT / ".ai" / "tasks" / "_TEMPLATE").is_dir()


def ensure_version_file() -> dict:
    """Make sure .nie-template-version.json exists and has the fields align.py
    relies on. Preserves every existing field unchanged — we never strip data
    from the marker, only add defaults for missing keys.

    In the template repo (where the marker is the canonical release manifest
    and has no `appliedTasks` concept), we read but do not mutate."""
    existing = _load_json(VERSION_FILE) or {}
    if _is_template_repo():
        # In the template repo we read but never mutate. Synthesise the keys
        # align.py needs in-memory only; the file on disk stays untouched.
        view = dict(existing)
        view.setdefault("templateVersion", "unknown")
        view.setdefault("appliedTasks", [])
        return view

    record = dict(existing)  # shallow copy preserves all existing keys
    record.setdefault("templateName", "NIE Template")
    record.setdefault("templateVersion", "unknown")
    record.setdefault("timezone", "Asia/Singapore")
    record.setdefault("appliedTasks", [])
    record.setdefault("adoptedAtSgt", None)
    record.setdefault("sourceTemplateRepo",
                      "https://niegithub.nie.edu.sg/NIE/nie-template.git")
    record.setdefault("localNotes", [])

    if record != existing:
        _save_json(VERSION_FILE, record)
    return record


def pending_tasks(applied: list[str]) -> list[dict]:
    """Return tasks in the index whose taskId isn't in `applied`, with appliesIf evaluated."""
    idx = _load_json(TASK_INDEX)
    if not idx:
        return []
    out = []
    for task in idx.get("tasks", []):
        tid = task["taskId"]
        if tid in applied:
            continue
        # Evaluate appliesIf if the task.json is local (it is, post-copy).
        task_path = REPO_ROOT / task["path"]
        task_json = _load_json(task_path / "task.json") or {}
        applies_if = task_json.get("appliesIf") or {}
        if not _applies(applies_if):
            continue
        out.append({
            "taskId": tid,
            "title": task.get("title"),
            "type": task.get("type"),
            "applyGuide": str(task_path / "apply.md"),
            "verifyScript": str(task_path / "verify.sh"),
            "runOnClone": task.get("runOnClone", False),
            "status": task.get("status", "released"),
        })
    return out


def _applies(applies_if: dict) -> bool:
    any_files = applies_if.get("anyFileExists") or []
    all_files = applies_if.get("allFilesExist") or []
    none_files = applies_if.get("noneFileExist") or []

    if any_files and not any((REPO_ROOT / p).exists() for p in any_files):
        return False
    if all_files and not all((REPO_ROOT / p).exists() for p in all_files):
        return False
    if none_files and any((REPO_ROOT / p).exists() for p in none_files):
        return False
    return True


def promote_audit_example() -> bool:
    """Phase 4 of the consolidation: derived repos receive `audit.example.yml`
    and need to use it as `audit.yml`. Promote the example file to the active
    name iff `audit.yml` doesn't already exist in the derived repo.

    Returns True if a promotion happened."""
    workflows = REPO_ROOT / ".github" / "workflows"
    example = workflows / "audit.example.yml"
    target = workflows / "audit.yml"
    if not example.is_file() or target.is_file():
        return False
    try:
        target.write_bytes(example.read_bytes())
        example.unlink()
        return True
    except OSError:
        return False


def prune_empty_feature_dirs() -> list[Path]:
    """Remove empty directories under common feature locations. Copier leaves
    these behind when every file in a directory matches an `_exclude` pattern.
    Returns the list of pruned paths."""
    candidates_roots = [
        REPO_ROOT / ".ai" / "features",
        REPO_ROOT / ".ai" / "tasks",
        REPO_ROOT / "src" / "backend" / "Libraries" / "Services" / "Services",
    ]
    pruned: list[Path] = []
    for root in candidates_roots:
        if not root.is_dir():
            continue
        for sub in sorted(root.iterdir()):
            if not sub.is_dir():
                continue
            # Only prune if entirely empty (no files, no subdirs)
            try:
                if not any(sub.iterdir()):
                    sub.rmdir()
                    pruned.append(sub)
            except OSError:
                pass
    return pruned


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--quiet", action="store_true",
                    help="Suppress output when there's nothing to do.")
    ap.add_argument("--json", action="store_true",
                    help="Emit a machine-readable JSON report.")
    args = ap.parse_args()

    if not TASK_INDEX.exists():
        msg = "WARNING: .ai/tasks/index.json missing — derived repo not aligned with any template release"
        if args.json:
            print(json.dumps({"status": "no-task-index", "message": msg}))
        else:
            print(f"[align] {msg}")
        return 0

    record = ensure_version_file()
    # Clean up empty directories left by Copier exclusions (only meaningful in
    # derived repos; in the template repo every directory has content).
    if not _is_template_repo():
        pruned = prune_empty_feature_dirs()
        if pruned and not args.quiet and not args.json:
            print(f"[align] pruned {len(pruned)} empty feature/task/service "
                  f"directories left by Copier exclusions")
        # Promote audit.example.yml -> audit.yml so derived repos have a
        # working CI gate immediately. Phase 4 of ADR 003.
        if promote_audit_example() and not args.quiet and not args.json:
            print("[align] promoted .github/workflows/audit.example.yml "
                  "-> audit.yml")
    pending = pending_tasks(record["appliedTasks"])

    if args.json:
        print(json.dumps({
            "templateVersion": record["templateVersion"],
            "appliedTasks": record["appliedTasks"],
            "pending": pending,
        }, indent=2))
        return 0

    if not pending:
        if not args.quiet:
            print(f"[align] up to date with template {record['templateVersion']} "
                  f"({len(record['appliedTasks'])} tasks applied)")
        return 0

    print(f"[align] template version: {record['templateVersion']}")
    print(f"[align] {len(record['appliedTasks'])} tasks applied, "
          f"{len(pending)} pending:")
    for t in pending:
        flag = " [runOnClone]" if t["runOnClone"] else ""
        sflag = f" [{t['status']}]" if t["status"] != "released" else ""
        print(f"  - {t['taskId']} ({t['type']}){flag}{sflag}: {t['title']}")
        print(f"      apply:  {t['applyGuide']}")
    print()
    print("Walk these via .ai/ALIGN.md (paste into Claude/Copilot/Gemini/Kiro)")
    print("or read each apply.md manually. Each task records itself in")
    print(".nie-template-version.json:appliedTasks on successful verify.sh.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
