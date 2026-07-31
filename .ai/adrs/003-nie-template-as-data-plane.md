# ADR 003 — nie-template is the data plane; nie-ignite is the UX plane

- **Status:** Accepted (2026-05-03)
- **Context:** A gap analysis between `nie-template` and `nie-ignite` (sibling repos in `niegithub.nie.edu.sg/NIE/`) found significant structural duplication. Both independently maintain (a) a list of features/modules, (b) project module-selection state, (c) recipe versioning, and (d) per-feature apply/verify runbooks. New tasks were being encoded in both repos by hand, which has already started to drift.
- **Drivers:** Reduce maintenance load. Eliminate two-write race condition for security tasks. Preserve both audiences (CLI engineers + non-engineers/UI users).
- **Decision:** Treat the two repos as different layers of the same product:
  - **nie-template** owns *recipes* (tasks, feature dossiers, release manifests, audit/registry/bot tooling, the canonical Copier scaffold).
  - **nie-ignite** owns *UX* (web wizard, AI entity builder, project history, generation orchestration). It consumes nie-template's recipes; it does not maintain its own.
- **Consequences:**
  - nie-ignite stops accepting new entries in `instructions/`. The folder is frozen as of `2026-05-03`. New tasks land only in `nie-template/.ai/tasks/`.
  - nie-ignite gains a `TemplateRecipeAdapter` that loads nie-template's `.ai/tasks/index.json` + `.ai/features/*/manifest.yaml` and adapts it to its `InstructionSetModel` shape at seed time.
  - Long-term, nie-ignite's `GenerationEngine` is replaced with a thin wrapper around `copier copy`. nie-ignite keeps the wizard UI, AI assist, and project history — that's the value-add that justifies its operational cost.
  - A project scaffolded via the Ignite UI must be byte-identical to the same project scaffolded via `copier copy --data ...` from CLI.
- **Out of scope:** UI/UX changes to the Ignite wizard, removal of nie-ignite's web app, deprecation of any of nie-template's existing tooling.
- **Phased migration:**
  1. Stop the bleeding (this ADR + freeze marker + README updates).
  2. Bridge — nie-ignite reads nie-template at seed time via `TemplateRecipeAdapter`.
  3. Replace nie-ignite's renderer with `IgniteCopierService` behind a feature flag.
  4. Ship `audit.yml` in nie-ignite's generation output so generated projects join the same fleet as CLI-scaffolded ones.
  5. Mark nie-ignite's legacy `instructions/` + generation engines `[Obsolete]`.
- **Reversal cost:** Phase 2 onwards is reversible by toggling a feature flag back to `legacy`. The seeder's pre-Phase-2 disk path is preserved for rollback.

## See also
- `docs/SPLIT-PLAN.md` — branch split plan covering the immediate cleanup
- `docs/template-distribution.md` — full distribution model
- `nie-ignite/external/nie-template/` — git submodule (added in Phase 2)
- `.ai/features/*/manifest.yaml` — structured feature data the bridge consumes (added in Phase 2)
