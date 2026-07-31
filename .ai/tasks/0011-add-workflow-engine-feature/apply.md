# Task 0011 — Add Workflow Engine Feature

> **Status:** scaffolded — derived repos that already have a bespoke workflow implementation should NOT apply this. Audit your existing approval flow first.

> **Why:** Six production apps (isaac-adm, i3g, istar, isaac-lite, ai-registry, sscol) each rebuilt a workflow state machine. This task ships the canonical version: enum + transition table + state log + service + UI components, all driven from `WorkflowTransitions` so adding a new state is a seed, not a code change.

The full feature dossier is [`.ai/features/workflow-engine/`](../../features/workflow-engine/). Read [`files.md`](../../features/workflow-engine/files.md) and [`do-dont.md`](../../features/workflow-engine/do-dont.md) before starting.

## Pre-checks

```bash
test ! -f src/backend/Libraries/Services/Services/Workflow/IWorkflowService.cs \
  || { echo "Already added; skipping."; exit 0; }
```

## 1. Files to create

Copy from the template repo at the matching template version (paths from `.ai/features/workflow-engine/files.md`):

```text
src/backend/Libraries/Domain/Enum/EWorkflowState.cs
src/backend/Libraries/Domain/Models/WorkflowTransition.cs
src/backend/Libraries/Domain/Models/WorkflowStateLog.cs
src/backend/Libraries/Services/Services/Workflow/IWorkflowService.cs
src/backend/Libraries/Services/Services/Workflow/WorkflowService.cs
src/backend/API/Controllers/WorkflowController.cs
src/frontend/main/src/types/workflow.ts
src/frontend/main/src/services/workflowService.ts
src/frontend/main/src/components/workflow/WorkflowTimeline.vue
src/frontend/main/src/components/workflow/WorkflowActionBar.vue
```

## 2. Files to edit

### `src/backend/API/Program.cs`

```diff
+ builder.Services.AddScoped<IWorkflowService, WorkflowService>();
```

### `src/backend/Libraries/Data/Data/NieTemplateDbContext.cs`

```diff
+ public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
+ public DbSet<WorkflowStateLog>   WorkflowStateLogs   => Set<WorkflowStateLog>();
…
+ // OnModelCreating
+ modelBuilder.Entity<WorkflowTransition>(b =>
+ {
+     b.HasIndex(x => new { x.OwnerType, x.FromState });
+     b.Property(x => x.RequiredAccessFunction).HasMaxLength(120);
+ });
+ modelBuilder.Entity<WorkflowStateLog>(b =>
+ {
+     b.HasIndex(x => new { x.OwnerType, x.OwnerId, x.TransitionedAt });
+ });
```

**Why:** transition lookup is per-(OwnerType, FromState); state log is queried in time order per record.

### `src/backend/Libraries/Domain/Models/PurchaseOrder.cs`

```diff
+ public EWorkflowState WorkflowState { get; set; } = EWorkflowState.Draft;
```

**Why:** sample integration so `WorkflowActionBar` has something to drive in the procurement reference UI. Derived repos that have already removed procurement (task 0003) will skip this edit.

### `src/backend/Libraries/Domain/Security/AccessFunctionCatalog.cs`

```diff
+ public const string WorkflowTransition = "workflow:transition";
+ public const string WorkflowAdmin      = "workflow:admin";
```

## 3. Database migration + seed

```bash
dotnet ef migrations add AddWorkflowEngine \
  --project src/backend/Libraries/Data \
  --startup-project src/backend/API
```

Edit `Extensions/DatabaseSeeder.cs` (or your seed location) to seed the default transitions for any owner type that uses the engine. Example for procurement:

```csharp
db.WorkflowTransitions.AddRange(
    new WorkflowTransition { OwnerType = "PurchaseOrder", FromState = EWorkflowState.Draft,        ToState = EWorkflowState.Submitted,    DisplayLabel = "Submit",       RequiredAccessFunction = "po:submit" },
    new WorkflowTransition { OwnerType = "PurchaseOrder", FromState = EWorkflowState.Submitted,    ToState = EWorkflowState.UnderReview, DisplayLabel = "Start Review", RequiredAccessFunction = "po:review" },
    new WorkflowTransition { OwnerType = "PurchaseOrder", FromState = EWorkflowState.UnderReview, ToState = EWorkflowState.Approved,    DisplayLabel = "Approve",      RequiredAccessFunction = "po:approve" },
    new WorkflowTransition { OwnerType = "PurchaseOrder", FromState = EWorkflowState.UnderReview, ToState = EWorkflowState.Rejected,    DisplayLabel = "Reject",       RequiredAccessFunction = "po:approve" },
    new WorkflowTransition { OwnerType = "PurchaseOrder", FromState = EWorkflowState.Approved,    ToState = EWorkflowState.Completed,    DisplayLabel = "Complete",     RequiredAccessFunction = "po:complete" }
);
```

## 4. Verification

```bash
dotnet build src/backend/NieTemplate.sln
pnpm --filter main type-check
grep -n "IWorkflowService"   src/backend/API/Program.cs
grep -n "EWorkflowState"     src/frontend/main/src/types/workflow.ts
grep -n "WorkflowState"      src/backend/Libraries/Domain/Models/PurchaseOrder.cs
```

Live smoke (services running):

```bash
curl -s http://localhost:5002/api/Workflow/transitions?ownerType=PurchaseOrder \
  | jq '.[] | {from:.fromState,to:.toState,label:.displayLabel}'
```

## 5. Rollback

```bash
dotnet ef migrations remove --project src/backend/Libraries/Data --startup-project src/backend/API
git restore --staged --worktree \
  src/backend/Libraries/Domain/Enum/EWorkflowState.cs \
  src/backend/Libraries/Domain/Models/WorkflowTransition.cs \
  src/backend/Libraries/Domain/Models/WorkflowStateLog.cs \
  src/backend/Libraries/Services/Services/Workflow/ \
  src/backend/API/Controllers/WorkflowController.cs \
  src/frontend/main/src/types/workflow.ts \
  src/frontend/main/src/services/workflowService.ts \
  src/frontend/main/src/components/workflow/ \
  src/backend/API/Program.cs \
  src/backend/Libraries/Data/Data/NieTemplateDbContext.cs \
  src/backend/Libraries/Domain/Models/PurchaseOrder.cs \
  src/backend/Libraries/Domain/Security/AccessFunctionCatalog.cs
```

## Maintainer review checklist before promoting to a release

- [ ] State enum reviewed against derived-repo terminology — keep generic, add per-feature wrappers if needed
- [ ] Audit log entries emitted on every transition (use existing audit-logging feature)
- [ ] WorkflowController endpoints all carry `[RequireAccessFunction(...)]`
- [ ] `IOwnedEntity` ownership check applied to transitions where applicable (BOLA, task 0007)
- [ ] WorkflowStateLog index strategy validated against expected query volume
