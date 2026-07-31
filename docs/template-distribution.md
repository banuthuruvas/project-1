# NIE Template — Distribution Model

> **Audience:** maintainers + tech leads in derived repos.
> **Status:** v1.1, last updated 2026-05-03 — Copier flow + Python tools verified end-to-end.

This document describes how the NIE Template is distributed to derived repositories, how derived repos stay aligned over time, and where each tool fits in the model. It complements [`.ai/common/09-template-versioning.md`](../.ai/common/09-template-versioning.md) (which covers *authoring* a release) by covering the *distribution* and *fleet* sides.

---

## The three planes

```
┌──────────────────────────────────────────────────────────────────┐
│  AUTHORING  (this repo)                                          │
│  • .ai/features/<feature>/   — per-feature dossiers              │
│  • .ai/tasks/NNNN-<slug>/    — units of change                   │
│  • docs/template-releases/   — release manifests                 │
│  • tools/template-versioning/release.py  — release CLI (Python)  │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  DISTRIBUTION  (Copier — external tool)                          │
│  • copier.yml                          — questions + excludes    │
│  • [[ _copier_conf.answers_file ]].jinja — answers persistence   │
│  • tools/template-rename/rename.py    — namespace substitution   │
│  • tools/template-align/align.py      — post-copy/update task    │
│                                          scan + empty-dir prune  │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  FLEET  (every derived repo + registry + bot)                    │
│  • .nie-template-version.json    — what's applied here           │
│  • .copier-answers.yml           — what was answered at scaffold │
│  • .github/workflows/audit.yml   — reusable CI gate +            │
│                                     optional telemetry POST      │
│  • tools/template-registry/registry.py — SQLite + JWT + HTML     │
│                                     dashboard + Prometheus       │
│  • tools/template-bot/bot.py     — auto-PR security tasks across │
│                                     the fleet (always-draft)     │
│  • catalog-info.yaml + backstage/software-template.yaml          │
│                                  — Backstage entry points        │
└──────────────────────────────────────────────────────────────────┘
```

The split matters: each plane can evolve independently. You can change *how* releases are authored (Authoring) without touching the *how* derived repos pull (Distribution). You can add fleet visibility (Fleet) without changing the release format.

---

## Lifecycles

### A. Creating a new derived repo

```bash
# 1. Scaffold via Copier (3-way-merge aware; replaces manual `git clone`)
pip install --user copier   # one-off
copier copy --trust gh:NIE/nie-template ./my-app
cd ./my-app
# (Answers stored in .copier-answers.yml; align.py runs as a post-task and
#  reports any pending derived-repo tasks.)

# 2. Initial commit (required for `copier update` to work later)
git init && git add . && git commit -m "chore: scaffold from NIE template"

# 3. Self-check via .ai/ALIGN.md
#    Paste the prompt into Claude/Copilot/Gemini/Kiro and let it walk any
#    baseline tasks the align.py report flagged.
```

**`--trust` is required** because Copier runs an opt-in post-scaffold task (`python tools/template-align/align.py`). Without `--trust`, Copier refuses to execute arbitrary commands.

The legacy `tools/template-scaffold/Program.cs` is retained as reference but no longer recommended — Copier subsumes it.

### B. Adopting a new template release

```bash
# In the derived repo:
copier update --trust                          # pulls latest template; 3-way merge
git diff                                       # review the merge
python tools/template-audit/audit.py           # local audit
git add . && git commit -m "chore(template): adopt 2026.05.03.1"
git push
# CI runs the reusable audit.yml; on green, merge.
```

**`copier update` does a real 3-way merge** — verified end-to-end on 2026-05-03:

- File modified only in template → applied cleanly
- File modified only in derived repo → preserved
- File modified in **both** on the same line → standard `<<<<<<<` / `>>>>>>>` conflict markers, `git status` shows `UU` (both-modified). Resolve like any git merge, then commit.

For existing repos that haven't scaffolded via Copier:

```bash
# One-time bootstrap (writes .copier-answers.yml against current state):
copier copy --trust --vcs-ref=main gh:NIE/nie-template . \
  --data-file=existing-answers.yml --force
```

Or skip Copier entirely and use `.ai/ALIGN.md` for AI-driven manual updates — both paths are supported.

### C. Pushing a security task to the fleet

When a `type: "security"` task ships in the template:

1. Maintainer authors the task in `.ai/tasks/NNNN-<slug>/` and cuts a release.
2. The (planned) `template-bot` GitHub App watches the template repo.
3. On release detected, the bot opens a PR in every derived repo it knows about:
   - PR title: `chore(template): apply task NNNN — <title>`
   - PR body: the contents of `apply.md` + the output of `verify.sh` run in CI
   - PR is **never auto-merged** — the derived-repo owners review and merge.
4. CI on the derived repo runs `audit.yml` to verify the change before review.

Until the bot exists, this is a manual notification: maintainers post in the team channel + tag derived-repo owners.

---

## What each tool owns

| Tool | Owns | Does NOT own |
|---|---|---|
| **Copier (external)** + [`copier.yml`](../copier.yml) | Distribution: scaffold, exclude per feature, write answers, 3-way merge updates | Authoring or audit |
| [`tools/template-rename/rename.py`](../tools/template-rename/rename.py) | Namespace substitution (NieTemplate → project name) in src/build/devcontainer/vscode | Anything outside those four roots |
| [`tools/template-align/align.py`](../tools/template-align/align.py) | Post-copy/update task discovery + marker file care + empty-dir pruning | Auto-applying tasks |
| [`tools/template-audit/audit.py`](../tools/template-audit/audit.py) (+ optional `ast_check.py`) | Compliance check (5 categories); AST checks for C# auth + TS quality when `--ast` is set | Auto-fixing findings |
| [`tools/template-versioning/release.py`](../tools/template-versioning/release.py) | Cut release: manifest + notes + index + marker + CHANGELOG, atomically; validate consistency | Distribution to derived repos |
| [`tools/template-registry/registry.py`](../tools/template-registry/registry.py) | Production-grade fleet-telemetry receiver: SQLite, JWT/bearer auth, HTML dashboard, Prometheus metrics, Docker-ready | Authentication provider lifecycle |
| [`tools/template-registry/receiver.py`](../tools/template-registry/receiver.py) | Pilot/legacy per-file receiver (kept for "single-laptop, no auth" use cases) | — |
| [`tools/template-bot/bot.py`](../tools/template-bot/bot.py) + [`.github/workflows/template-bot.yml`](../.github/workflows/template-bot.yml) | Open draft PRs across the fleet for unapplied `type: security` tasks. Reads fleet from explicit YAML or from registry. | Merging — always draft, owners decide |
| [`.github/workflows/audit.yml`](../.github/workflows/audit.yml) | Reusable CI gate; PR comment with audit summary; optional telemetry POST | Pushing changes back |
| [`catalog-info.yaml`](../catalog-info.yaml) + [`backstage/software-template.yaml`](../backstage/software-template.yaml) | Backstage entry points (consumed when an instance is provisioned) | Running Backstage itself |
| `.ai/ALIGN.md` | AI-driven manual task application (interactive) | Automated fleet sync |
| `tools/template-{scaffold,audit,versioning}/*.csproj` | **Removed 2026-05-03.** Replaced by Copier + Python scripts above. | — |

---

## Versioning + identity

Each derived repo carries three identifiers:

| File | Set when | Purpose |
|---|---|---|
| `.nie-template-version.json:templateVersion` | At scaffold + on each `copier update` / task adoption | "Which template release am I aligned to?" |
| `.nie-template-version.json:appliedTasks` | When each task's `verify.sh` exits 0 | "Which incremental tasks have I taken?" |
| `.copier-answers.yml` | At scaffold + retained on every update | "What feature toggles did I pick?" |

These are the contract for any future fleet registry: a derived repo POSTs the three to a registry endpoint from CI, the registry shows drift across the fleet.

---

## Roadmap

What's already shipped:

- ✅ Tasks 0010–0014 (chat, workflow, PDF, storage, MyInfo) registered as opt-in features
- ✅ Copier-based scaffolding (`copier.yml`) with feature-toggle excludes
- ✅ Reusable `audit.yml` workflow + example caller (Python-based, no .NET)
- ✅ Python tooling stack: `align.py`, `audit.py`, `release.py`, `rename.py`
- ✅ Legacy .NET tooling (`*.csproj`) removed
- ✅ Namespace rename (`rename.py`) — Copier-aware + standalone
- ✅ Telemetry POST from `audit.yml` + reference receiver (`receiver.py`)
- ✅ Audit recognises class-level `[Authorize]`/`[RequireAccessFunction]`, skips Migrations
- ✅ **Production registry** (`registry.py`) — SQLite, JWT (HS256, stdlib), bearer, HTML dashboard, `/metrics`, Dockerfile + compose
- ✅ **Template-bot** (`bot.py` + `template-bot.yml`) — auto-PR for security tasks across the fleet, always-draft, fleet from yaml or registry
- ✅ **AST audit** (`ast_check.py`) — tree-sitter checks for C# controller authz + `.Take(N)`, TS `as any` + unguarded `.json()`. Wired via `audit.py --ast`; falls back to regex if libs missing
- ✅ **Backstage stubs** (`catalog-info.yaml` + `backstage/software-template.yaml`) — ready to register when an instance is provisioned

What's next (smaller scope):

1. **Production deploy of `registry.py`** behind TLS. The Dockerfile is rootless; pair it with a reverse proxy (nginx, Cloudflare Tunnel, Cloud Run). Mind that JWT issuance still needs a real provider — the receiver only verifies HS256 against a shared secret; production should layer an OIDC IdP that mints those tokens (or move to RS256, trivial swap).
2. **GitHub App for `template-bot`** instead of PAT. The workflow already supports both paths via `BOT_APP_ID` + `BOT_APP_KEY`. App is recommended once the bot is opening PRs on >5 repos — token rotation is automatic.
3. **More AST rules.** Easy wins: detect `await` chains that never check for errors; detect `Console.WriteLine` in production code paths; detect missing `[ProducesResponseType]` on controller actions.
4. **Backstage instance** — only when the fleet exceeds ~50 repos. The stubs are already here.

---

## FAQ

**Q. Why both Copier and the existing task system?**
Copier handles *file-level distribution* (what bytes go from template to derived repo). Tasks handle *semantic change* (apply this migration, run this verification). They compose: Copier brings files in, then `align.py` (run as a Copier task) discovers which task dossiers still need to be applied semantically. One source of truth for "what changed," two ways to deliver it.

**Q. Do existing repos have to migrate to Copier?**
No. `.ai/ALIGN.md` continues to work for repos that don't want the Copier dependency. Copier is the *recommended* path for new repos because it handles updates better.

**Q. How do we prevent feature drift across derived repos?**
Three layers, in order of strength: (1) `audit.yml` reusable workflow as a CI gate; (2) the planned template-bot opens PRs for security tasks (forcing visibility); (3) the planned registry surfaces fleet-wide drift on a dashboard. Today only layer 1 exists; build out 2 and 3 as the fleet grows.

**Q. Who owns merging template PRs in a derived repo?**
The derived-repo team. The template-bot opens PRs but never merges them. This preserves team autonomy — the template recommends, projects decide.
