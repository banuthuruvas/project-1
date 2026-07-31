# nie-ignite ↔ nie-template Bridge — Integration Guide

> **Audience:** maintainers operating both repos.
> **Status:** Phases 1–5 of [ADR 003](../.ai/adrs/003-nie-template-as-data-plane.md) landed 2026-05-03. The actual *flip* (turning the bridge on as the default) is operational, not code.

## What this document covers

How to verify the bridge end-to-end on a real machine, what to flip when, and how to roll back if anything regresses.

## Repo layout assumed

```
~/Dev/niegithub.nie.edu.sg/NIE/
├── nie-template/                     ← data plane (this repo)
│   ├── .ai/tasks/index.json          ← canonical task list
│   ├── .ai/features/<name>/manifest.yaml
│   ├── .nie-template-version.json    ← release marker
│   └── copier.yml                    ← scaffold questions + excludes
└── nie-ignite/                       ← UX plane
    ├── instructions/FROZEN.md        ← old recipes (frozen Phase 1)
    ├── nie-template-pin.json         ← which template version we mirror
    └── external/nie-template/        ← (Phase 2+) git submodule, OR
                                        the bridge falls back to the
                                        sibling path above
```

## The four switches

Everything is config-driven so you can flip incrementally without redeploying:

| Setting (in nie-ignite's `appsettings.json`) | Phase | What it does |
|---|---|---|
| `Ignite:Instructions:Source` | 2 | `legacy` (default) reads `./instructions/`. `templateBridge` reads from nie-template via `TemplateBridgeProvider`. |
| `Ignite:TemplateBridge:SubmodulePath` | 2 | Path to nie-template checkout. Default `external/nie-template`. |
| `Ignite:TemplateBridge:RequirePinMatch` | 2 | If true, the bridge throws if `nie-template-pin.json` doesn't match the actual checkout's `templateVersion`. |
| `Ignite:CopierEngine:CopierExecutable` | 3 | Path to the `copier` CLI. Default expects it on PATH. |

Plus one DB row to enable the Copier engine for a project's revision: insert into `AIEngineConfigs` where `EngineCode = 'copier'` and `Enabled = true`.

## End-to-end smoke test

This is the test plan I'd run on the maintainer's machine before promoting the bridge to a real environment. **All optional — none of the code paths need this to be done now.**

### Pre-reqs

```bash
# Ensure copier is on PATH (used by Phase 3)
pip install --user copier
copier --version    # 9.2+

# Ensure nie-template is at a clean HEAD; build clean
cd ~/Dev/niegithub.nie.edu.sg/NIE/nie-template
python tools/template-versioning/release.py validate
python tools/template-audit/audit.py --repo .

# Ensure nie-ignite is on the consolidation branch + builds
cd ../nie-ignite
dotnet build src/backend/NieTemplate.sln
```

### Phase 2 verification — bridge seeds from nie-template

```bash
# 1. Add the submodule (first-time only)
cd ~/Dev/niegithub.nie.edu.sg/NIE/nie-ignite
git submodule add ../nie-template external/nie-template
git submodule update --init --recursive

# 2. Flip the source
# In src/backend/API/appsettings.Development.json:
#   "Ignite": {
#     "Instructions": { "Source": "templateBridge" },
#     "TemplateBridge": { "RequirePinMatch": false }
#   }

# 3. Wipe the InstructionSet rows so the seeder re-creates them
psql nie-ignite-dev -c "DELETE FROM \"InstructionSetItems\"; DELETE FROM \"InstructionSets\"; DELETE FROM \"Instructions\";"

# 4. Restart the API and check the seed log
dotnet run --project src/backend/API
# Expected log line:
#   info: Seeded N instruction(s) from nie-template@<sha> (templateVersion 2026.04.28.2).
```

### Phase 3 verification — Copier engine generates a real project

```bash
# 1. Insert the engine config (one-off)
psql nie-ignite-dev <<SQL
INSERT INTO "AIEngineConfigs" ("EngineCode", "ProviderType", "Enabled", "PriorityOrder", "DisplayName", "CreatedOn", "UpdatedOn")
VALUES ('copier', 0, true, 0, 'Copier (nie-template)', NOW(), NOW());
SQL

# 2. Through the Ignite UI: create a draft project, select modules,
#    publish a revision, request generation. Ignite should:
#    - Resolve the engine via GenerationEngineRegistry → CopierGenerationEngine
#    - Spawn `copier` against external/nie-template
#    - Slurp the resulting tree into GeneratedFileContent rows
#    - Surface them in the Workspace for review

# 3. Quick smoke that doesn't need the UI: invoke the engine programmatically.
#    See tests/specs/api/generation-copier.api.spec.ts (skeleton; not yet
#    asserting against real DB rows — fill in when running this test).

# 4. Compare output: scaffold the same project via the CLI to verify
#    byte-equivalence.
copier copy --trust ../nie-template /tmp/cli-equivalent \
  --data project_name=test-app --data project_title="Test App" \
  --data dotnet_root_namespace=TestApp --defaults --vcs-ref=HEAD
diff -ru /tmp/cli-equivalent <ignite-generated-output-path>   # should be empty
```

### Phase 4 verification — derived projects ship audit.yml

```bash
# After a Copier generation succeeds:
cat <ignite-output-path>/.github/workflows/audit.yml
# Should contain `uses: NIE/nie-template/.github/workflows/audit.yml@main`
# and NOT be the reusable workflow itself (which is excluded in copier.yml).

# CLI scaffold should produce the same:
ls /tmp/cli-equivalent/.github/workflows/audit.yml
```

### Phase 5 verification — legacy path still rolls back

```bash
# Flip back to legacy
# appsettings: Ignite:Instructions:Source = "legacy"

# Restart, confirm seeder used legacy path
# log line:
#   warn: Template bridge not available: ... → falls through to
#   the legacy [Obsolete] path. Compiler warnings on the call site are
#   wrapped with #pragma; the rollback does NOT change behaviour.
```

## What's intentionally not done in this branch

- **The git submodule is not added** — that's a real `git submodule add` on the maintainer's machine. The bridge code falls back to the sibling path (`../nie-template`) when the submodule isn't there, so dev-loop works without it.
- **The `AIEngineConfigs` row for `copier` is not seeded** — that's a one-off DB insert. We deliberately don't auto-seed it because flipping it on prematurely would default-route generation through Copier before the maintainer is ready.
- **The Copier engine has not been exercised against a real Ignite generation job.** It builds, but the integration test (`generation-copier.api.spec.ts`) is a skeleton. Run the verification above on the first attempt.
- **No data migration from legacy `instructions/` rows to the bridge-sourced ones.** The seeder upserts by `ItemCode`, so on first bridge run any existing rows are *updated* in place rather than replaced. That's correct behaviour but worth eyeballing on the first deploy.

## Rollback

Each phase has a clean rollback:

| Phase | Rollback |
|---|---|
| 1 | Move `instructions/FROZEN.md` aside, resume editing `instructions/`. The bridge config is opt-in. |
| 2 | Set `Ignite:Instructions:Source` to `legacy`. Old code path is intact. |
| 3 | Set the `copier` row in `AIEngineConfigs.Enabled = false`. Other engines pick up. |
| 4 | Remove `audit.yml` from generation output (CLI side: undo the `_exclude` rule in `copier.yml`). |
| 5 | The `[Obsolete]` attribute is non-breaking; the wrapping `#pragma` keeps the build clean. To delete legacy code, remove the method + its `[Obsolete]` annotation in one PR. |

## What you should not need to do

- **Don't** edit `instructions/manifest.json` or anything under `instructions/items/`. Those are frozen — changes go in nie-template.
- **Don't** maintain `EModuleType` and `.ai/features/*/manifest.yaml` in lockstep by hand. The plan is to derive `EModuleType` from the manifest list in a future phase. Until then, when you add a new feature, add the manifest in nie-template AND extend `EModuleType` once.
- **Don't** delete the legacy `SeedFromLegacyDirectoryAsync` until the bridge has been the default for ≥2 release cycles in production.

## Reference paths

- nie-template ADR: [`.ai/adrs/003-nie-template-as-data-plane.md`](../.ai/adrs/003-nie-template-as-data-plane.md)
- nie-ignite freeze marker: `instructions/FROZEN.md` (sibling repo)
- Bridge provider: `nie-ignite/src/backend/Libraries/Services/Services/TemplateBridge/TemplateBridgeProvider.cs`
- Seeder: `nie-ignite/src/backend/API/HostedServices/InstructionSeederService.cs`
- Copier engine: `nie-ignite/src/backend/Libraries/Services/Services/GenerationEngine/CopierGenerationEngine.cs`
- Engine registry: `nie-ignite/src/backend/Libraries/Services/Services/GenerationEngine/GenerationEngineRegistry.cs`
- Pin file: `nie-ignite/nie-template-pin.json`
