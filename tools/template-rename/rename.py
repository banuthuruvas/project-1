#!/usr/bin/env python3
"""
NIE Template — namespace / branding rename.

Replaces template placeholder strings with project-specific names across:

  - `NieTemplate` -> `dotnet_root_namespace`
  - `NIE Template` -> `project_title`
  - File CONTENTS in src/, build/, .devcontainer/, .vscode/, README.md, AGENTS.md
  - File NAMES (e.g. NieTemplate.sln -> EmsPortal.sln,
    NieTemplateDbContext.cs -> EmsPortalDbContext.cs)

Used in two ways:

  1. As a Copier `_tasks` step after `copier copy` / `copier update`.
     The task reads `dotnet_root_namespace` and `project_title` from the answers file.
  2. As a standalone CLI for repos that didn't scaffold via Copier:
       python tools/template-rename/rename.py --to MyApp

Stdlib only. Idempotent — running twice with the same `--to` is a no-op.

Paths NEVER touched (template metadata stays as `NieTemplate`):
  .git/  node_modules/  bin/  obj/  dist/
  .ai/   tools/   docs/   .github/   docs/template-releases/

Exit codes:
  0  changes made (or none needed)
  1  invocation error
  2  validation error (--to is invalid, etc.)
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

# UTF-8 stdout on Windows
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

REPO = Path.cwd()

# Paths to walk for substitution. Anything outside these is left alone.
INCLUDE_ROOTS = [
    "src",
    "build",
    ".devcontainer",
    ".vscode",
]
INCLUDE_FILES_AT_ROOT = [
    "README.md",
    "AGENTS.md",
    "GEMINI.md",
]

# Globs to skip even within an included root.
SKIP_DIRS = {".git", "node_modules", "bin", "obj", "dist", "Migrations"}

# Binary or generated extensions never modified.
SKIP_SUFFIXES = {".png", ".jpg", ".jpeg", ".gif", ".ico", ".webp",
                 ".pdf", ".zip", ".gz", ".tar", ".7z",
                 ".dll", ".pdb", ".exe", ".so", ".dylib",
                 ".pem", ".pfx", ".key", ".crt", ".cer",
                 ".db", ".sqlite", ".bin",
                 ".lock", ".lockb"}

NAME_RE = re.compile(r"^[A-Z][A-Za-z0-9]{2,40}$")
TITLE_RE = re.compile(r'^[^"\\<>\r\n]{3,80}$')


def _is_valid_name(name: str) -> bool:
    return bool(NAME_RE.match(name))


def _is_valid_title(title: str) -> bool:
    return bool(TITLE_RE.match(title))


def _read(path: Path) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError):
        return None


def _write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8")


def _walk(roots: list[Path]) -> list[Path]:
    out: list[Path] = []
    for root in roots:
        if not root.is_dir():
            continue
        for p in root.rglob("*"):
            if any(part in SKIP_DIRS for part in p.parts):
                continue
            if p.suffix.lower() in SKIP_SUFFIXES:
                continue
            if p.is_file():
                out.append(p)
    return out


def _read_copier_answers() -> dict[str, str]:
    """Return the flat Copier answers used by the rename task."""
    answers = REPO / ".copier-answers.yml"
    if not answers.is_file():
        return {}
    out: dict[str, str] = {}
    for line in answers.read_text(encoding="utf-8").splitlines():
        if ":" not in line or line.lstrip().startswith("#"):
            continue
        key, raw_value = line.split(":", 1)
        key = key.strip()
        if key not in {"dotnet_root_namespace", "project_title"}:
            continue
        value = raw_value.strip().strip("'\"")
        if value:
            out[key] = value
    return out


def _read_copier_answer() -> str | None:
    """If running in a Copier-scaffolded repo, return dotnet_root_namespace."""
    answers = REPO / ".copier-answers.yml"
    if not answers.is_file():
        return None
    # Tiny YAML reader — Copier writes flat key: value pairs
    for line in answers.read_text(encoding="utf-8").splitlines():
        if line.startswith("dotnet_root_namespace:"):
            value = line.split(":", 1)[1].strip().strip("'\"")
            return value or None
    return None


def replace_contents(target_files: list[Path], from_name: str, to_name: str,
                     dry_run: bool) -> tuple[int, int]:
    """Substitute from_name -> to_name in file contents.
    Returns (files_modified, total_replacements)."""
    files_modified = 0
    total_replacements = 0
    for f in target_files:
        text = _read(f)
        if text is None or from_name not in text:
            continue
        replacements = text.count(from_name)
        new_text = text.replace(from_name, to_name)
        if not dry_run:
            _write(f, new_text)
        files_modified += 1
        total_replacements += replacements
    return files_modified, total_replacements


def rename_files(target_files: list[Path], from_name: str, to_name: str,
                 dry_run: bool) -> int:
    """Rename files whose basename contains from_name.
    Returns the count of renamed files."""
    renamed = 0
    for f in target_files:
        if from_name not in f.name:
            continue
        new_name = f.name.replace(from_name, to_name)
        new_path = f.parent / new_name
        if new_path.exists():
            print(f"WARN: target {new_path.relative_to(REPO)} exists; skipping",
                  file=sys.stderr)
            continue
        if not dry_run:
            f.rename(new_path)
        print(f"  renamed: {f.relative_to(REPO)} -> {new_name}")
        renamed += 1
    return renamed


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--to", default=None,
                    help="Target name (e.g. EmsPortal). If omitted, reads "
                         "dotnet_root_namespace from .copier-answers.yml.")
    ap.add_argument("--title", default=None,
                    help="Display title (e.g. EMS Portal). If omitted, reads "
                         "project_title from .copier-answers.yml.")
    ap.add_argument("--from", dest="from_name", default="NieTemplate",
                    help="Source placeholder name (default NieTemplate).")
    ap.add_argument("--dry-run", action="store_true",
                    help="Show what would change without modifying anything.")
    ap.add_argument("--quiet", action="store_true",
                    help="Suppress info output.")
    args = ap.parse_args(argv)

    copier_answers = _read_copier_answers()
    to_name = args.to or copier_answers.get("dotnet_root_namespace")
    to_title = args.title or copier_answers.get("project_title")
    if not to_name:
        if not args.quiet:
            print("[rename] no --to given and .copier-answers.yml has no "
                  "dotnet_root_namespace; nothing to do.")
        return 0

    if to_name == args.from_name and (not to_title or to_title == "NIE Template"):
        if not args.quiet:
            print(f"[rename] target name '{to_name}' equals source — no-op.")
        return 0

    if not _is_valid_name(to_name):
        print(f"ERROR: target name '{to_name}' must match {NAME_RE.pattern}",
              file=sys.stderr)
        return 2

    if to_title and not _is_valid_title(to_title):
        print(f"ERROR: display title '{to_title}' must match {TITLE_RE.pattern}",
              file=sys.stderr)
        return 2

    # Build the file list
    roots = [REPO / r for r in INCLUDE_ROOTS]
    target_files = _walk(roots)
    for fname in INCLUDE_FILES_AT_ROOT:
        f = REPO / fname
        if f.is_file():
            target_files.append(f)

    if not args.quiet:
        print(f"[rename] {args.from_name} -> {to_name} "
              f"(scanning {len(target_files)} files)")

    files_modified, replacements = replace_contents(
        target_files, args.from_name, to_name, args.dry_run)
    if to_title and to_title != "NIE Template":
        title_files_modified, title_replacements = replace_contents(
            target_files, "NIE Template", to_title, args.dry_run)
        files_modified += title_files_modified
        replacements += title_replacements

    # Re-walk after content edits because filenames change too
    target_files = _walk(roots)
    for fname in INCLUDE_FILES_AT_ROOT:
        f = REPO / fname
        if f.is_file():
            target_files.append(f)
    renamed = rename_files(target_files, args.from_name, to_name, args.dry_run)

    if args.quiet:
        return 0

    verb = "would modify" if args.dry_run else "modified"
    print(f"[rename] {verb} {files_modified} file(s) "
          f"({replacements} replacement(s)); {verb} {renamed} filename(s)")
    if args.dry_run:
        print("[rename] dry run — no changes written")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
