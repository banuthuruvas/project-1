#!/usr/bin/env python3
"""
NIE Template — release tool (Python port).

Replaces tools/template-versioning/Program.cs with a stdlib-only script.

Subcommands:
  validate                  Check the four version files agree on a single current version
  current                   Print the current templateVersion
  propose                   Print the next templateVersion using YYYY.MM.DD.N (Asia/Singapore)
  create-release ...        Mint a new release: writes manifests, updates index + CHANGELOG

`create-release` flags:
  --summary "..."           One-line summary (required)
  --release-type feature|fix|security|breaking|refactor (required)
  --task ID  (repeatable)   Task IDs included in this release (must already exist in .ai/tasks/index.json)
  --breaking                Mark as a breaking release (also set by --release-type breaking)
  --dry-run                 Print the planned changes; do not write
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import re
import subprocess
import sys
from pathlib import Path

REPO = Path.cwd()
VERSION_FILE = REPO / ".nie-template-version.json"
RELEASES_DIR = REPO / "docs" / "template-releases"
RELEASES_INDEX = RELEASES_DIR / "index.json"
TASK_INDEX = REPO / ".ai" / "tasks" / "index.json"
CHANGELOG = REPO / "CHANGELOG.md"

SGT = dt.timezone(dt.timedelta(hours=8), name="Asia/Singapore")
VERSION_RE = re.compile(r"^(\d{4})\.(\d{2})\.(\d{2})\.(\d+)$")


# ---------------------------------------------------------------------------
# helpers
# ---------------------------------------------------------------------------

def _load(p: Path) -> dict:
    return json.loads(p.read_text(encoding="utf-8"))


def _save(p: Path, data: dict) -> None:
    p.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def _today_sgt() -> dt.date:
    return dt.datetime.now(SGT).date()


def _git(*args: str) -> str | None:
    try:
        return subprocess.check_output(["git", *args], cwd=REPO, text=True).strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        return None


def _propose_next(today: dt.date, used: set[str]) -> str:
    base = f"{today.year:04d}.{today.month:02d}.{today.day:02d}"
    seq = 1
    while f"{base}.{seq}" in used:
        seq += 1
    return f"{base}.{seq}"


def _used_versions() -> set[str]:
    if not RELEASES_INDEX.is_file():
        return set()
    idx = _load(RELEASES_INDEX)
    return {r["templateVersion"] for r in idx.get("releases", [])}


# ---------------------------------------------------------------------------
# subcommands
# ---------------------------------------------------------------------------

def cmd_current(_args) -> int:
    if not VERSION_FILE.is_file():
        print("(no .nie-template-version.json)")
        return 0
    print(_load(VERSION_FILE).get("templateVersion", "unknown"))
    return 0


def cmd_propose(_args) -> int:
    print(_propose_next(_today_sgt(), _used_versions()))
    return 0


def cmd_validate(args) -> int:
    """Verify that all version artefacts agree on the same currentVersion.

    Smart about template vs derived repos: if `docs/template-releases/` does
    not exist, this is a derived repo and we only check the marker file.
    """
    is_template_repo = RELEASES_INDEX.is_file() or RELEASES_DIR.is_dir()

    errors: list[str] = []

    if not VERSION_FILE.is_file():
        errors.append(f"missing: {VERSION_FILE.relative_to(REPO)}")
        print("validate: FAIL")
        for e in errors:
            print(f"  - {e}")
        return 1

    if not is_template_repo:
        # Derived repo — only validate the marker file's basic shape.
        try:
            data = _load(VERSION_FILE)
        except json.JSONDecodeError as e:
            print(f"validate: FAIL (marker JSON parse error: {e})")
            return 1
        if not data.get("templateVersion"):
            print("validate: FAIL (marker has no templateVersion)")
            return 1
        if data.get("timezone") != "Asia/Singapore":
            print(f"validate: WARN (marker timezone={data.get('timezone')!r}, "
                  f"expected 'Asia/Singapore')")
        print(f"validate: OK (derived repo — marker shape valid; "
              f"templateVersion={data['templateVersion']})")
        return 0

    if not RELEASES_INDEX.is_file():
        errors.append(f"missing: {RELEASES_INDEX.relative_to(REPO)}")
    if errors:
        print("validate: FAIL")
        for e in errors:
            print(f"  - {e}")
        return 1

    marker = _load(VERSION_FILE)
    idx = _load(RELEASES_INDEX)
    marker_v = marker.get("templateVersion")
    index_current = idx.get("currentVersion")
    if marker_v != index_current:
        errors.append(f"version mismatch: marker={marker_v} index.currentVersion={index_current}")

    # Every release in index.json should have a manifest + notes file
    for r in idx.get("releases", []):
        v = r["templateVersion"]
        if not VERSION_RE.match(v):
            errors.append(f"version '{v}' does not match YYYY.MM.DD.N")
        man = REPO / r.get("manifestPath", "")
        notes = REPO / r.get("notesPath", "")
        if not man.is_file():
            errors.append(f"{v}: manifestPath missing ({r.get('manifestPath')})")
        if not notes.is_file():
            errors.append(f"{v}: notesPath missing ({r.get('notesPath')})")

    # CHANGELOG should mention the current version
    if CHANGELOG.is_file():
        text = CHANGELOG.read_text(encoding="utf-8", errors="ignore")
        if marker_v and (f"## [{marker_v}]" not in text and f"## {marker_v}" not in text):
            errors.append(f"CHANGELOG.md missing entry for {marker_v}")

    if errors:
        print("validate: FAIL")
        for e in errors:
            print(f"  - {e}")
        return 1
    print(f"validate: OK ({marker_v} consistent across marker, index, manifests, CHANGELOG)")
    return 0


def cmd_create_release(args) -> int:
    if not args.summary:
        print("ERROR: --summary required", file=sys.stderr)
        return 2
    if not args.release_type:
        print("ERROR: --release-type required", file=sys.stderr)
        return 2

    today = _today_sgt()
    used = _used_versions()
    new_version = args.version or _propose_next(today, used)
    if new_version in used:
        print(f"ERROR: version {new_version} already in releases index", file=sys.stderr)
        return 1

    breaking = args.breaking or args.release_type == "breaking"
    released_at = dt.datetime.now(SGT).isoformat(timespec="seconds")
    source_commit = _git("rev-parse", "HEAD") or "UNRESOLVED"
    source_repo = _git("config", "--get", "remote.origin.url") \
        or "https://niegithub.nie.edu.sg/NIE/nie-template.git"

    # Validate referenced tasks exist
    tasks = args.task or []
    if tasks and TASK_INDEX.is_file():
        known = {t["taskId"] for t in _load(TASK_INDEX).get("tasks", [])}
        unknown = [t for t in tasks if t not in known]
        if unknown:
            print(f"ERROR: unknown task IDs: {unknown}", file=sys.stderr)
            return 1

    manifest = {
        "templateName": "NIE Template",
        "templateVersion": new_version,
        "releasedAtSgt": released_at,
        "timezone": "Asia/Singapore",
        "releaseType": args.release_type,
        "breaking": breaking,
        "summary": args.summary,
        "tasks": tasks,
        "sourceCommit": source_commit,
        "sourceTemplateRepo": source_repo,
    }

    notes = (
        f"# NIE Template {new_version}\n\n"
        f"_Released_: {released_at}  \n"
        f"_Type_: {args.release_type}{' (breaking)' if breaking else ''}\n\n"
        f"## Summary\n\n{args.summary}\n\n"
        f"## Tasks\n\n"
        + ("\n".join(f"- {t}" for t in tasks) if tasks else "_(none — manifest-only release)_")
        + "\n\n## Source\n\n"
        f"- commit `{source_commit}`\n"
        f"- repo `{source_repo}`\n"
    )

    # Update index
    if RELEASES_INDEX.is_file():
        idx = _load(RELEASES_INDEX)
    else:
        idx = {"templateName": "NIE Template", "timezone": "Asia/Singapore", "releases": []}

    prev = idx.get("currentVersion")
    idx["currentVersion"] = new_version
    idx.setdefault("releases", []).append({
        "templateVersion": new_version,
        "releasedAtSgt": released_at,
        "summary": args.summary,
        "releaseType": args.release_type,
        "breaking": breaking,
        "manifestPath": f"docs/template-releases/{new_version}.json",
        "notesPath": f"docs/template-releases/{new_version}.md",
        "supersedesVersion": prev,
    })

    # Marker
    marker = _load(VERSION_FILE) if VERSION_FILE.is_file() else {
        "templateName": "NIE Template",
        "timezone": "Asia/Singapore",
    }
    marker.update({
        "templateVersion": new_version,
        "releasedAtSgt": released_at,
        "releaseType": args.release_type,
        "breaking": breaking,
        "sourceCommit": source_commit,
        "sourceTemplateRepo": source_repo,
        "releaseNotesPath": f"docs/template-releases/{new_version}.md",
    })

    # CHANGELOG prepend
    changelog_entry = (
        f"## [{new_version}] — {today.isoformat()}\n\n"
        f"**Type:** {args.release_type}"
        f"{' (breaking)' if breaking else ''}\n\n"
        f"{args.summary}\n\n"
    )
    if tasks:
        changelog_entry += "Tasks: " + ", ".join(tasks) + "\n\n"

    if args.dry_run:
        print(f"DRY RUN: would create {new_version}")
        print(f"  - manifest: docs/template-releases/{new_version}.json")
        print(f"  - notes:    docs/template-releases/{new_version}.md")
        print(f"  - index:    {RELEASES_INDEX.relative_to(REPO)} (currentVersion -> {new_version})")
        print(f"  - marker:   .nie-template-version.json (templateVersion -> {new_version})")
        if CHANGELOG.is_file():
            print("  - CHANGELOG.md: prepend new entry")
        return 0

    RELEASES_DIR.mkdir(parents=True, exist_ok=True)
    _save(RELEASES_DIR / f"{new_version}.json", manifest)
    (RELEASES_DIR / f"{new_version}.md").write_text(notes, encoding="utf-8")
    _save(RELEASES_INDEX, idx)
    _save(VERSION_FILE, marker)

    if CHANGELOG.is_file():
        old = CHANGELOG.read_text(encoding="utf-8")
        # Insert after the first H1 if present, otherwise at top
        lines = old.splitlines(keepends=True)
        for i, ln in enumerate(lines):
            if ln.startswith("# "):
                lines.insert(i + 1, "\n" + changelog_entry)
                break
        else:
            lines.insert(0, changelog_entry)
        CHANGELOG.write_text("".join(lines), encoding="utf-8")
    else:
        CHANGELOG.write_text("# Changelog\n\n" + changelog_entry, encoding="utf-8")

    print(f"create-release: OK — minted {new_version}")
    return 0


# ---------------------------------------------------------------------------
# entry
# ---------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)

    sub.add_parser("current").set_defaults(func=cmd_current)
    sub.add_parser("propose").set_defaults(func=cmd_propose)
    sub.add_parser("validate").set_defaults(func=cmd_validate)

    cr = sub.add_parser("create-release")
    cr.add_argument("--summary", required=True)
    cr.add_argument("--release-type", choices=["feature", "fix", "security", "breaking", "refactor"], required=True)
    cr.add_argument("--task", action="append", default=[],
                    help="Task ID included in this release (repeatable)")
    cr.add_argument("--breaking", action="store_true")
    cr.add_argument("--version",
                    help="Override the proposed version (otherwise computed)")
    cr.add_argument("--dry-run", action="store_true")
    cr.set_defaults(func=cmd_create_release)

    args = ap.parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
