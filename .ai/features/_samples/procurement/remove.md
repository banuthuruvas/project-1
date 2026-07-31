# Procurement — Remove

The authoritative removal procedure lives in [`.ai/tasks/0003-remove-procurement-samples/apply.md`](../../../tasks/0003-remove-procurement-samples/apply.md) — that is the file the alignment agent consumes. Read it for the line-by-line steps.

This dossier file just summarizes:

1. **When to run it:** after your derived project has its own real entities. Procurement should NOT be in any production deployment that is not actually a procurement system.
2. **Pre-checks:** `test -f src/backend/Libraries/Domain/Models/PurchaseOrder.cs` (skip if already gone).
3. **What it removes:** 22 backend files (entities, DTOs, services, controllers, enum), 10 frontend files (pages + services), 12 file-level config edits, plus a drop migration.
4. **Verification:** `dotnet build`, `grep` returns 0 procurement matches, `pnpm type-check`, `dotnet ef database update`.
5. **Rollback:** `git restore` + `dotnet ef migrations remove`.
