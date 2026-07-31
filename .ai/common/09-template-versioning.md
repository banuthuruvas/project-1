# 09 — Template Versioning (Task-Oriented)

Template versioning is **separate** from API versioning, application versioning, and deployment image tags. It tracks "what releases of the NIE template has this repo adopted."

## Two contracts

| Contract | Purpose | Where |
| --- | --- | --- |
| Release | Human-readable history — what a release contains as a bundle | `CHANGELOG.md`, `docs/template-releases/` |
| Task | Machine-executable unit — exact files to delete/edit/create with verification | `.ai/tasks/NNNN-<slug>/` |

A release MAY bundle multiple tasks. A derived repo agent applies tasks one at a time, oldest to newest.

## Version format

`YYYY.MM.DD.N` in `Asia/Singapore`. `N` starts at 1 each Singapore day and increments for additional same-day releases.

Example: `2026.04.28.1`.

## Canonical files

| File | Purpose |
| --- | --- |
| `.nie-template-version.json` | Current version marker. Carried into derived repos. |
| `CHANGELOG.md` | Human-readable release history. |
| `docs/template-releases/index.json` | Ordered list of releases. |
| `docs/template-releases/<version>.json` | Decision-complete release manifest (refers to tasks). |
| `docs/template-releases/<version>.md` | Human-readable release note. |
| `.ai/tasks/index.json` | Ordered list of all tasks across the template's history. |
| `.ai/tasks/NNNN-<slug>/task.json` | Per-task machine-readable manifest. |
| `.ai/tasks/NNNN-<slug>/apply.md` | Step-by-step apply guide for an AI agent. |
| `.ai/tasks/NNNN-<slug>/verify.sh` | Post-apply validation script. |

## Task lifecycle

1. Maintainer makes the template change.
2. Maintainer creates `.ai/tasks/NNNN-<slug>/` from `.ai/tasks/_TEMPLATE/`.
3. Maintainer fills `task.json`, writes `apply.md`, writes `verify.sh`.
4. Maintainer runs `tools/template-versioning/...create-release` (extended to register the new task).
5. Maintainer commits everything together.
6. Husky pre-commit + Jenkins validate metadata.

## Derived-repo upgrade flow

A derived repo agent runs:

```text
1. Read .nie-template-version.json → maxAppliedTaskId
2. Read .ai/tasks/index.json
3. For each task with id > maxAppliedTaskId, in order:
     a. Run pre-checks (does this apply to my repo?)
     b. Follow apply.md exhaustively
     c. Run verify.sh — must exit 0
     d. Append the taskId to .nie-template-version.json:appliedTasks
4. Update templateVersion to the latest applied task's templateVersionAfterApply
5. Commit "chore: adopt template tasks 0002..0007"
```

The full executable prompt for derived-repo agents is `.ai/ALIGN.md`.

## Required `task.json` schema

```json
{
  "taskId": "0002",
  "slug": "remove-sample-model",
  "title": "Remove SampleModel scaffolding from derived repo",
  "type": "cleanup | feature | refactor | security | breaking",
  "breaking": false,
  "runOnClone": true,
  "supersedes": null,
  "dependsOn": ["0001"],
  "appliesIf": {
    "anyFileExists": ["src/backend/Libraries/Domain/Models/SampleModel.cs"]
  },
  "filesDeleted": ["..."],
  "filesEdited": [{ "path": "...", "reason": "..." }],
  "filesCreated": [],
  "verification": [
    { "type": "command", "run": "dotnet build src/backend/<sln>", "expectExit": 0 },
    { "type": "grep", "pattern": "SampleModel", "paths": ["src/"], "expectMatches": 0 }
  ],
  "minTemplateVersion": "2026.04.13.1",
  "templateVersionAfterApply": "2026.04.28.1",
  "docs": [".ai/features/_samples/sample-model/remove.md"]
}
```

## Source commit semantics

`sourceCommit` = the Git HEAD when the release artifacts were generated. It is provenance, not a self-referential merge hash. This avoids commit-amend workflows.
