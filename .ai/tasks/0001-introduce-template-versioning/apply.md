# Task 0001 — Introduce Template Release Versioning

This task is mostly historical: it shipped in template release `2026.04.13.1`. A derived repo created from a template version ≥ `2026.04.13.1` already has these files. The task is here so the index is complete and so an agent can repair a repo that lost the metadata.

## Pre-checks

```bash
[ ! -f .nie-template-version.json ] || { echo "Already adopted; skipping."; exit 0; }
```

## Files to copy from the template repo

```text
.nie-template-version.json
CHANGELOG.md
docs/template-releases/index.json
docs/template-releases/2026.04.13.1.json
docs/template-releases/2026.04.13.1.md
tools/template-versioning/        # entire folder
.husky/pre-commit                 # add or merge
```

## Files to edit

None — this task is pure addition.

## Verification

```bash
dotnet run --project tools/template-versioning/TemplateVersioning.csproj -- validate --head
test -f .nie-template-version.json
```

## Rollback

```bash
git restore --staged --worktree .nie-template-version.json CHANGELOG.md docs/template-releases/ tools/template-versioning/ .husky/pre-commit
```
