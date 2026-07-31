# NIE Template Tooling

Developer tools for managing the NIE template and derived repositories.

> **2026-05-03 update:** the canonical tools are now **Python scripts** (`*.py`) requiring only Python 3.11 stdlib. The original `.NET` projects (`*.csproj`) have been **removed** — see the migration table at the bottom. Scaffolding is delegated to **[Copier](https://copier.readthedocs.io/)** — see [`../copier.yml`](../copier.yml).

## Overview

| Concern | Tool | What it does |
|---|---|---|
| **Scaffolding** | `copier` (external) + [`../copier.yml`](../copier.yml) | Create a new project: questions → files copied + answers persisted in `.copier-answers.yml` |
| **Updates** | `copier update` | Re-render with current template state; 3-way merge into derived repo |
| **Naming** | [`template-rename/rename.py`](./template-rename/rename.py) | Substitute `NieTemplate` → project namespace across src/ build/ devcontainer/ vscode/. Idempotent. Runs as a Copier task automatically. |
| **Alignment** | [`template-align/align.py`](./template-align/align.py) | Post-copy/update task discovery; reports unapplied tasks; prunes empty dirs Copier exclusion leaves behind |
| **Audit** | [`template-audit/audit.py`](./template-audit/audit.py) | Compliance check (5 categories: structure / metadata / features / code / security) |
| **Releases** | [`template-versioning/release.py`](./template-versioning/release.py) | Mint releases, validate manifests, propose next version |
| **Fleet registry** | [`template-registry/registry.py`](./template-registry/registry.py) | Production-grade receiver: SQLite + JWT/bearer auth + HTML dashboard at `/dashboard` + Prometheus `/metrics`. Dockerfile + compose included. |
| **Pilot registry** | [`template-registry/receiver.py`](./template-registry/receiver.py) | Minimal per-file receiver. Use `registry.py` instead unless single-machine ephemeral is what you want. |
| **Fleet bot** | [`template-bot/bot.py`](./template-bot/bot.py) | Stdlib script that scans the fleet (config or registry-discovered) and opens draft PRs for unapplied `type: security` tasks via the GitHub API. Always-draft, never auto-merge. |
| **AST audit** | [`template-audit/ast_check.py`](./template-audit/ast_check.py) | Optional tree-sitter check pack (C# + TS). Wired into `audit.py --ast`; falls back to regex if libs missing. |

## Prerequisites

- Python 3.11+ (`python --version`)
- Copier 9.2+ for scaffolding (`pip install --user copier`)
- `git` (for `copier update`'s 3-way merge)

No .NET SDK required. (The legacy .NET tools are dotnet-8.0+, kept around as reference.)

## Quick Start

### For new projects — scaffold via Copier

```bash
# Install Copier once
pip install --user copier

# Scaffold a new project
copier copy --trust gh:NIE/nie-template ./my-app

# Initialise as git repo (required before first `copier update`)
cd ./my-app
git init && git add . && git commit -m "chore: scaffold from NIE template"
```

The `--trust` flag allows the post-scaffold `_tasks` (running `align.py`) to execute. Required because Copier treats arbitrary shell commands as untrusted by default.

### For existing projects — pull updates

```bash
cd ./my-app
copier update --trust            # 3-way-merge new template state in
git diff                         # review the merge
python tools/template-audit/audit.py
git add . && git commit -m "chore: adopt template <version>"
```

If the merge introduces conflicts (file edited in both the template and the derived repo on the same line), you get standard `<<<<<<<` / `=======` / `>>>>>>>` markers. Resolve them like any git merge.

### For audit — gate CI

```bash
# Default: fails only on critical findings
python tools/template-audit/audit.py --repo .

# Strict mode: fails on any non-critical warning too
python tools/template-audit/audit.py --repo . --strict

# Machine-readable
python tools/template-audit/audit.py --repo . --json
```

The CI-side path is the reusable workflow at [`../.github/workflows/audit.yml`](../.github/workflows/audit.yml). Drop a one-line caller into the derived repo (see `audit.example.yml`).

### For template maintenance — cut a release

```bash
# What's the current version?
python tools/template-versioning/release.py current

# What would the next version be (uses YYYY.MM.DD.N in Asia/Singapore)?
python tools/template-versioning/release.py propose

# Mint a release including specific tasks
python tools/template-versioning/release.py create-release \
  --summary "Add chat, workflow, PDF, file storage, MyInfo as opt-in features" \
  --release-type feature \
  --task 0010 --task 0011 --task 0012 --task 0013 --task 0014

# Verify all four version artefacts are in sync
python tools/template-versioning/release.py validate
```

## Typical Workflow — Onboarding a New Project

```text
1. Scaffold
   copier copy --trust gh:NIE/nie-template ./my-app
   cd ./my-app && git init && git add . && git commit -m "scaffold"

2. Audit (initial)
   python tools/template-audit/audit.py
   → expect warnings (namespace not yet renamed, etc.) — fix in next steps

3. Customise
   - Rename namespace from EmsPortal to <YourProject>
   - Configure DB connection string + secret store integration
   - Optionally apply pending tasks from `python tools/template-align/align.py`
     (or paste .ai/ALIGN.md into Claude/Copilot/Gemini)

4. Audit (final)
   python tools/template-audit/audit.py --strict
   → all green before first deploy

5. Stay aligned
   copier update --trust   # any time the template ships a new release
```

## Typical Workflow — Maintaining the Template

```text
1. Author the change
   - Add/modify files
   - Create or update a task dossier in .ai/tasks/<NNNN>-<slug>/

2. Cut a release
   python tools/template-versioning/release.py create-release \
     --summary "..." --release-type feature --task NNNN

3. Validate
   python tools/template-versioning/release.py validate

4. Tag + push
   git tag <version> && git push origin <version>

5. Notify derived repos
   - Pilot: open a PR in one repo via `copier update`
   - Once stable: the planned template-bot opens PRs across the fleet
```

## Tool internals

### `template-align/align.py`

Two responsibilities:

1. **Marker care** — ensure `.nie-template-version.json` exists with the keys `align.py` needs (`appliedTasks`, etc.) without ever stripping fields the template release process wrote.
2. **Pending-task discovery** — read `.ai/tasks/index.json`, evaluate each task's `appliesIf`, and report tasks that are not yet in `appliedTasks`. **Does not auto-apply tasks** — that's the `.ai/ALIGN.md` AI-driven flow's job.

Detects "I'm running in the template repo" via the presence of `docs/template-releases/index.json` + `.ai/tasks/_TEMPLATE/` and skips marker mutation in that case.

### `template-audit/audit.py`

Five categories, ~13 checks total:

| Category | Examples |
|---|---|
| **structure** | `.ai/`, `.ai/common/`, `.ai/features/`, `.ai/tasks/` exist; CLAUDE.md present |
| **metadata** | `.nie-template-version.json` shape valid; CHANGELOG mentions current version |
| **features** | every `.ai/features/<feature>/` has README.md + files.md + do-dont.md |
| **code** | no hardcoded enum strings (heuristic); API responses validated (heuristic) |
| **security** | task 0005/0006/0007/0009 artefacts present; controllers carry `[Authorize]` or `[RequireAccessFunction]` (improved over the .NET version — recognises class-level attribute application) |

Exit codes: `0` no critical findings, `1` at least one critical, `2` invocation error. `--strict` flips warnings into failures.

### `template-rename/rename.py`

Substitutes a placeholder name (`NieTemplate` by default) with the project's chosen `dotnet_root_namespace`. Three behaviours:

1. **Run by Copier automatically** — listed in `copier.yml:_tasks`. Reads the answer from `.copier-answers.yml`.
2. **Run manually after a non-Copier clone** — `python tools/template-rename/rename.py --to MyApp`.
3. **Dry-run preview** — `python tools/template-rename/rename.py --to MyApp --dry-run`.

Only walks `src/`, `build/`, `.devcontainer/`, `.vscode/`, root-level `README.md`/`AGENTS.md`/`GEMINI.md`. Template-meta paths (`.ai/`, `tools/`, `docs/`, `.github/`) are explicitly skipped — those should keep referring to the canonical "NieTemplate" name.

Renames three known files: `NieTemplate.sln`, `NieTemplate.slnLaunch`, `NieTemplateDbContext.cs`. Substitutes ~30 string occurrences across appsettings, Swagger titles, env files, and `Directory.Build.props`. Idempotent.

### `template-registry/registry.py` (recommended)

Production-grade receiver. Stdlib only (no external Python deps). What's in:

- **SQLite persistence** with WAL mode; one row per payload, indexed for "latest per repo" lookups
- **Two auth modes** — bearer token (`REGISTRY_TOKEN`) and HS256 JWT (`REGISTRY_JWT_SECRET`); both can be enabled simultaneously; JWT verification is stdlib-only with `exp`/`nbf` enforcement
- **HTML drift dashboard** at `/dashboard` — fleet-wide table grouped by templateVersion, critical-status-first sort
- **Prometheus metrics** at `/metrics` (counter for total payloads, gauges for repos / criticals / version distribution)
- **Hard limits** — 1 MB payload cap, 200-row history per repo
- **Container-ready** — [`Dockerfile`](./template-registry/Dockerfile) (rootless, non-root UID, healthcheck) and [`docker-compose.yml`](./template-registry/docker-compose.yml). The DB lives on a volume so the image stays read-only

Schema is contractual: [`template-registry/REGISTRY-SCHEMA.md`](./template-registry/REGISTRY-SCHEMA.md). Production deployments can swap the backend (Postgres, DynamoDB, GitHub Pages static index) without changing the CI side.

### `template-registry/receiver.py` (legacy)

The original 150-line per-file receiver. Kept as the simplest "I just want to see if telemetry posts work" path. Use `registry.py` for anything beyond a single laptop.

### `template-bot/bot.py`

Scans a fleet of derived repos and opens draft PRs for unapplied security tasks.

Two fleet sources:

- `--fleet-config tools/template-bot/fleet.yml` — explicit list (good for ≤20 repos)
- `--registry https://nie-registry.../v1/audit` — auto-discover via the registry (the recommended path at scale)

Three execution modes:

- `--dry-run` — plan only; no GitHub mutations. Reads markers via the GitHub API in read-only mode.
- `--plan-only` — like `--dry-run` plus writes `bot-plan.json` for CI artifact upload.
- (default) — opens draft PRs labeled `template-bot,template,security`. **Never auto-merges.** **Never overwrites an existing open PR for the same task.**

Driven by [`.github/workflows/template-bot.yml`](../.github/workflows/template-bot.yml) on a weekly cron + on `release.published` + manual `workflow_dispatch`. Auth uses either a PAT (`BOT_GITHUB_TOKEN` secret) or a GitHub App installation token (recommended at scale; configured via `BOT_APP_ID` var + `BOT_APP_KEY` secret).

### `template-audit/ast_check.py`

Tree-sitter-based check pack. Optional add-on to [`audit.py`](./template-audit/audit.py); enabled with `--ast` (falls back to regex with a warning if `tree_sitter`, `tree_sitter_c_sharp`, or `tree_sitter_typescript` is not installed).

Adds four checks the regex pass cannot accurately compute:

| Rule | What it catches |
|---|---|
| `cs/missing-authorize` | Public `*Controller` methods without `[Authorize]` / `[RequireAccessFunction]` / `[AllowAnonymous]` at method OR class level. Pinpoints exact method names + line numbers. |
| `cs/unbounded-take` | `.Take(N)` calls where N>100 — flag for review against `PagedSearchDto`. |
| `ts/as-any` | `as any` casts in TS source. |
| `ts/unvalidated-json` | `.json()` calls without a parse / type guard nearby. |

Install:

```bash
pip install tree_sitter tree_sitter_c_sharp tree_sitter_typescript
python tools/template-audit/audit.py --repo . --ast
```

### `template-versioning/release.py`

`create-release` writes four artefacts atomically:

- `docs/template-releases/<version>.json` — manifest
- `docs/template-releases/<version>.md` — human-readable notes
- `docs/template-releases/index.json` — appended; `currentVersion` bumped
- `.nie-template-version.json` — marker updated
- `CHANGELOG.md` — new section prepended

`validate` checks all four files reference the same current version and that every release in the index has its manifest + notes file on disk.

## Legacy .NET tools

**Removed in 2026-05-03.** The original `tools/template-{audit,scaffold,versioning}/*.csproj` and `Program.cs` files have been deleted. Their replacements:

| Legacy command | Python equivalent |
|---|---|
| `dotnet run --project tools/template-versioning -- validate --head` | `python tools/template-versioning/release.py validate` |
| `dotnet run --project tools/template-versioning -- create-release ...` | `python tools/template-versioning/release.py create-release ...` |
| `dotnet run --project tools/template-audit -- check --repo .` | `python tools/template-audit/audit.py --repo .` |
| `dotnet run --project tools/template-scaffold -- create` | `copier copy --trust gh:NIE/nie-template ./my-app` |

## Design philosophy

- **Stdlib only** for the Python scripts — no `pip install` for derived repos beyond Copier itself
- **Idempotent** — re-running `align.py` / `audit.py` / `release.py validate` is always safe
- **Non-destructive by default** — scripts that mutate state warn before doing so; `align.py` never strips fields
- **Composable** — JSON output (`--json`) on every script for piping into dashboards or CI

## CI integration

Use the reusable workflow at [`.github/workflows/audit.yml`](../.github/workflows/audit.yml). Caller example at [`.github/workflows/audit.example.yml`](../.github/workflows/audit.example.yml).

## Contributing

When adding a new template feature:

1. Create the feature dossier (`.ai/features/<feature>/{README,files,do-dont,verify}.md`)
2. Create a task dossier (`.ai/tasks/NNNN-<slug>/{task.json,apply.md,verify.sh}`)
3. Run `python tools/template-versioning/release.py create-release --task NNNN ...`
4. Commit everything together

See [.ai/README.md](../.ai/README.md) for the full instruction structure.
