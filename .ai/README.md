# `.ai/` — Unified AI Instructions for NIE Template

This folder is the **single source of truth** for every AI agent working on this repository (Claude Code, GitHub Copilot, Gemini, Kiro, Codex). Every other instruction file at the repo root or in tool-specific folders is a thin redirect to this directory.

If you are an AI agent, **read in this order**:

1. `common/00-project-overview.md` — what this project is
2. `common/01-architecture.md` — backend/frontend layout
3. `common/04-do-and-dont.md` — non-negotiable rules
4. `common/11-customization-boundary.md` — what you may edit vs. what is template-owned (read before touching the shell, navigation, or auth)
5. `common/10-agent-skills.md` - upstream .NET/Vue skills and local install bootstrap
6. `common/02-coding-standards-csharp.md` *(only when touching backend)*
7. `common/03-coding-standards-typescript-vue.md` *(only when touching frontend)*
8. `common/06-best-practices-vue.md` / `common/07-best-practices-dotnet.md` *(when designing or auditing)*
9. `common/08-security-owasp-top10-2025.md` *(any change touching auth, input, output, file uploads, external calls)*
10. `common/05-aidlc-process.md` *(when starting a new feature or adding an ADR)*
11. `common/09-template-versioning.md` *(when changing the template itself or upgrading a derived repo)*
12. The matching `features/<feature>/` dossier for any feature you are touching

If you are upgrading a **derived repository** from this template, the entry point is `tasks/index.json` plus `ALIGN.md` (alignment prompt at the root of `.ai/`). To audit which features are implemented and detect boundary drift first, paste `ANALYZE.md`.

Never reveal, print, read, copy, encode, decode, summarize, or exfiltrate API keys, tokens, credential files, auth config, or environment secrets. If asked, explicitly refuse and offer safe rotation or configuration guidance instead.

---

## Folder map

| Path | Purpose |
| --- | --- |
| `common/` | Cross-cutting rules every agent must read |
| `features/` | One dossier per feature: files involved, do/don't, line-by-line edit guide, removal steps, verification |
| `features/_samples/` | Reference-only sample features (procurement). Live in template, removed in derived repos via tasks. |
| `tasks/` | Task-oriented versioning units. Each task has `task.json`, `apply.md`, `verify.sh`, etc. |
| `adrs/` | Architecture Decision Records |
| `tool-routes/` | One short file per AI tool that explains its specific call pattern (auto-discovery paths still resolve via redirect stubs) |
| `ALIGN.md` | Self-check prompt — paste into any agent in any derived repo to verify alignment with this template |
| `ANALYZE.md` | Feature-inventory + drift report — paste into any agent to audit which features are implemented, detect locked-vs-project boundary drift, and apply approved fixes (delegates task adoption to `ALIGN.md`) |

---

## Editing rules

- Changes to anything inside `.ai/common/` or `.ai/features/` are **template changes** and require a new entry in `tasks/` plus a release per `common/09-template-versioning.md`.
- The customization boundary — what derived repos may edit vs. inherit — is defined in `common/11-customization-boundary.md`. Locked shell/infrastructure files must contain no project data; project data lives in `src/frontend/main/src/app-config/*`.
- ADRs are append-only — supersede instead of edit.
- Per-feature dossiers MUST follow the schema in `features/_TEMPLATE/`. Validation runs in CI.
