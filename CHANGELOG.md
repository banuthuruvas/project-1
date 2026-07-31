# Changelog

## [2026.06.04.1] — 2026-06-04

**Type:** security

Tighten AI-agent instructions to require Context7 for current external context and explicitly refuse requests to reveal, read, print, copy, encode, decode, summarize, or exfiltrate secrets.

Tasks: 0027


## [2026.05.29.6] — 2026-05-29

**Type:** feature

Add LocalStack S3 for local development: a dev-container localstack service (S3 on :4566, auto-created bucket), an S3FileStorageService that honors a custom ServiceUrl + creds, dev appsettings pre-pointed at LocalStack, and .gitattributes to keep shell scripts LF.

Tasks: 0026


## [2026.05.29.5] — 2026-05-29

**Type:** feature

Implement real S3 file storage (AWSSDK.S3) from the nie-ai-sandbox reference: IFileStorageService + FileStorageService (local) + S3FileStorageService (S3) + FileStorageContentTypes, selected by FileStorage:Provider; supersedes 0024.

Tasks: 0025


## [2026.05.29.4] — 2026-05-29

**Type:** refactor

Organize FileStorage config into nested Provider + Local + S3 sections (env-var friendly), with a backward-compatible fallback to the legacy flat key.

Tasks: 0024


## [2026.05.29.3] — 2026-05-29

**Type:** feature

Implement the shell-guardrail contract from task 0021 (locked-file git-diff gate wired into Husky + the audit.yml PR gate), and close deferred polish: auth login-logo parity, complete feature manifests, and a corrected PWA manifest.

Tasks: 0023


## [2026.05.29.2] — 2026-05-29

**Type:** feature

Add ANALYZE.md analysis playbook (feature inventory + drift report with a clearance gate) and a deterministic drift category in the template audit.

Tasks: 0022


## [2026.05.29.1] — 2026-05-29

**Type:** refactor

Separate project config from the locked shell into app-config/* for copy-paste inheritance; add the customization-boundary doc + app-shell-navigation dossier; physically isolate the procurement backend sample; scaffold shell guardrails.

Tasks: 0020, 0021


## [2026.05.25.2] — 2026-05-25

**Type:** refactor

Protect frontend shell and shared Vue components from feature edits

Tasks: 0019


## [2026.05.25.1] — 2026-05-25

**Type:** refactor

Remove frontend env files and adopt runtime frontend configuration

Tasks: 0018


## [2026.05.23.3] — 2026-05-23

**Type:** fix

Fixes Reports implementation gaps found by Vue and .NET skill review.

Tasks: 0017


## [2026.05.23.2] — 2026-05-23

**Type:** feature

Hardens .NET 10 OpenAPI and access-function coverage, completes report PDF preview UI, and fills feature dossier gaps.

Tasks: 0016


## [2026.05.23.1] - 2026-05-23 17:18 SGT

**Type:** feature

Adds external .NET and Vue Agent Skill bootstrap instructions, including Codex routing and local installation guidance for missing skills.

Tasks: 0015


All releases before 2026.04.13.1 are legacy pre-versioning history.

## [2026.04.28.2] - 2026-04-28 11:42 SGT
- Summary: Implements 5 audit follow-up tasks (0005-0009): SecurityHeadersMiddleware, SsrfGuard (MyInfo wired), IOwnedEntity + RequireOwnership + EnsureOwnedAsync, EApprovalStage enum (BE+FE), PagedSearchDto. Closes audit gaps W-A05, API8, W-A10, API7 (partial), API1, N-19, API4, N-17.
- Release type: `fix`
- Breaking: No
- Detailed notes: [docs/template-releases/2026.04.28.2.md](docs/template-releases/2026.04.28.2.md)
- Task 0005 — SecurityHeadersMiddleware + SecurityHeadersOptions registered in Program.cs after UseCors and before UseResponseCaching
- Task 0006 — SsrfGuard helper at Shared/Helpers; wired into MyInfoService constructor (validates discovery URL once). PortalSsoService integration deferred.
- Task 0007 — IOwnedEntity marker, RequireOwnershipAttribute (IFilterFactory), generic OwnedEntityActionFilter<TEntity>, BaseController.EnsureOwnedAsync helper. DbContext registered as scoped alias for MainDbContext.
## [2026.04.28.1] - 2026-04-28 10:49 SGT
- Summary: Unified .ai/ instruction folder; task-oriented versioning model with ALIGN.md self-check prompt; remove SampleModel + introduce polymorphic Document; per-feature dossiers; Vue/.NET/OWASP audit checklists; Procurement reclassified as reference sample.
- Release type: `feature`
- Breaking: No
- Detailed notes: [docs/template-releases/2026.04.28.1.md](docs/template-releases/2026.04.28.1.md)
- Created .ai/ as the single source of truth for AI agent instructions (common/, features/, tasks/, adrs/, tool-routes/, ALIGN.md).
- Wrote 10 cross-cutting docs in .ai/common/ covering project overview, architecture, C# + TS/Vue coding standards, do-and-dont, AIDLC, Vue 3 audit checklist, .NET 10 audit checklist, OWASP 2025 audit checklist, template versioning.
- Introduced task-oriented versioning model in .ai/tasks/ with index.json, _TEMPLATE/ scaffold, and four shipped tasks (0001-introduce-template-versioning, 0002-remove-sample-model, 0003-remove-procurement-samples, 0004-unify-ai-instructions).
## [2026.04.13.1] - 2026-04-13 15:09 SGT
- Summary: Introduced template release versioning, release manifests, and validation tooling for downstream AI-driven upgrades.
- Release type: `feature`
- Breaking: No
- Detailed notes: [docs/template-releases/2026.04.13.1.md](docs/template-releases/2026.04.13.1.md)
- Added .nie-template-version.json, CHANGELOG.md, and docs/template-releases/ as the formal template release contract.
- Added a cross-platform .NET tool to create and validate template release metadata in Singapore local time.
- Wired release metadata validation into Husky, Jenkins, and the shared agent guidance files.
