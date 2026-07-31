# Task 0008 — Introduce `EApprovalStage` Enum

> **Status:** scaffolded.
> **Why:** rule N-19 in `.ai/common/07-best-practices-dotnet.md` and rule 11 in `.ai/common/04-do-and-dont.md`. The procurement approval flow uses raw strings ("Manager" / "Finance" / "Procurement") for stage. This violates the template's enum-first contract. The strings appear in `PurchaseOrderController.Submit`, `PurchaseOrderController.ProcessApproval`, the database, the DTO, and the frontend approval queue — five surfaces, each independently breakable.

## Pre-checks

```bash
test ! -f src/backend/Libraries/Domain/Enum/EApprovalStage.cs || { echo "Already applied."; exit 0; }
```

## 1. Files to create

### `src/backend/Libraries/Domain/Enum/EApprovalStage.cs`

```csharp
namespace Domain.Enum;

public enum EApprovalStage
{
    Manager = 1,
    Finance = 2,
    Procurement = 3
}
```

Order matters — the ordinal drives the existing `StageOrder` column. Keep them stable; never renumber.

### `src/frontend/main/src/types/procurement.ts`

```typescript
// Mirror of Domain.Enum.EApprovalStage — string values match enum.ToString()
export enum EApprovalStage {
  Manager = 'Manager',
  Finance = 'Finance',
  Procurement = 'Procurement',
}
```

### Migration

```bash
dotnet ef migrations add PurchaseOrderApprovalStageEnum \
  --project src/backend/Libraries/Data \
  --startup-project src/backend/API
```

The migration converts the column type from `text` to a string-backed enum. EF Core stores enums as integer by default; if the column was previously `text`, configure the property with `.HasConversion<string>()` so existing data is preserved. Edit `MainDbContext.OnModelCreating`:

```csharp
modelBuilder.Entity<PurchaseOrderApproval>()
    .Property(a => a.ApprovalStage)
    .HasConversion<string>();
```

## 2. Files to edit

### `src/backend/Libraries/Domain/Models/PurchaseOrderApproval.cs`

```diff
-public string ApprovalStage { get; set; } = default!;
+public EApprovalStage ApprovalStage { get; set; }
```

### `src/backend/Libraries/Domain/Dto/PurchaseOrderDto.cs`

`PurchaseOrderApprovalDto.ApprovalStage` typed as `EApprovalStage`.

### `src/backend/API/Controllers/PurchaseOrderController.cs`

In `Submit`:

```diff
-po.Approvals.Add(new PurchaseOrderApproval { ApprovalStage = "Manager", StageOrder = 1, PurchaseOrderId = po.Id });
-po.Approvals.Add(new PurchaseOrderApproval { ApprovalStage = "Finance", StageOrder = 2, PurchaseOrderId = po.Id });
-po.Approvals.Add(new PurchaseOrderApproval { ApprovalStage = "Procurement", StageOrder = 3, PurchaseOrderId = po.Id });
+po.Approvals.Add(new PurchaseOrderApproval { ApprovalStage = EApprovalStage.Manager, StageOrder = (int)EApprovalStage.Manager, PurchaseOrderId = po.Id });
+po.Approvals.Add(new PurchaseOrderApproval { ApprovalStage = EApprovalStage.Finance, StageOrder = (int)EApprovalStage.Finance, PurchaseOrderId = po.Id });
+po.Approvals.Add(new PurchaseOrderApproval { ApprovalStage = EApprovalStage.Procurement, StageOrder = (int)EApprovalStage.Procurement, PurchaseOrderId = po.Id });
```

In `ProcessApproval` switch:

```diff
-po.Status = nextApproval.ApprovalStage switch
-{
-    "Finance" => EPurchaseOrderStatus.PendingFinanceApproval,
-    "Procurement" => EPurchaseOrderStatus.PendingProcurementApproval,
-    _ => EPurchaseOrderStatus.PendingManagerApproval
-};
+po.Status = nextApproval.ApprovalStage switch
+{
+    EApprovalStage.Finance => EPurchaseOrderStatus.PendingFinanceApproval,
+    EApprovalStage.Procurement => EPurchaseOrderStatus.PendingProcurementApproval,
+    EApprovalStage.Manager => EPurchaseOrderStatus.PendingManagerApproval,
+    _ => throw new InvalidOperationException($"Unknown approval stage: {nextApproval.ApprovalStage}")
+};
```

### `src/frontend/main/src/staff/pages/staff/ApprovalQueue.vue`

Replace any `if (stage === 'Manager')` style checks with `if (stage === EApprovalStage.Manager)`. Add a label formatter:

```typescript
function approvalStageLabel(stage: EApprovalStage): string {
  switch (stage) {
    case EApprovalStage.Manager: return 'Manager Approval';
    case EApprovalStage.Finance: return 'Finance Approval';
    case EApprovalStage.Procurement: return 'Procurement Approval';
  }
}
```

### `src/frontend/main/src/services/purchaseOrderService.ts`

DTO field `approvalStage: EApprovalStage`.

## 3. Verification

```bash
dotnet build src/backend/NieTemplate.sln
grep -rn '"Manager"\|"Finance"\|"Procurement"' \
   src/backend/API/Controllers/PurchaseOrderController.cs \
   src/backend/Libraries/Services/Services/PurchaseOrder/ \
   src/frontend/main/src/staff/pages/staff/ApprovalQueue.vue   # 0 matches
pnpm --filter main type-check
dotnet ef database update --project src/backend/Libraries/Data --startup-project src/backend/API
```

## 4. Rollback

```bash
git restore --staged --worktree src/ && \
dotnet ef migrations remove --project src/backend/Libraries/Data --startup-project src/backend/API
```

## Maintainer review

- [ ] Confirm production data only contains the 3 known stages before applying the migration (otherwise the conversion will fail)
- [ ] Add `EApprovalStage` to procurement feature dossier `customize.md`
