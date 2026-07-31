# Procurement — Customize

This file is for the **rare** case where your derived project actually runs procurement as its production workflow. In that case, do not run task 0003 — keep the code and rename it to your project's namespace.

If you are not running procurement in production, the answer is always: **don't customize, run task 0003 to remove it, copy the patterns into your real feature.**

## Adopt procurement as your real workflow

1. Decide whether to keep the name "Procurement" or rename to your project's vocabulary.
2. If renaming: project-wide find/replace following these mappings (apply per-file, review each hit):
   - `Procurement` → `<YourFeature>`
   - `PurchaseOrder` → `<YourEntity>`
   - `Vendor` → `<YourMaster>`
   - `CatalogItem` → `<YourCatalog>`
3. Re-create migrations from scratch in your derived repo's project name; squash the procurement migration history if you need a clean schema.
4. Replace the seeded fake data in `DatabaseSeeder.SeedVendorsAsync / SeedCatalogItemsAsync / SeedPurchaseOrdersAsync` with your project's real seed (or delete the seed entirely if production data comes from somewhere else).
5. Update `EPurchaseOrderStatus` to match your actual workflow states. Mirror to the FE.
6. Replace approval chain stages in `PurchaseOrderController.Submit` (`"Manager"`, `"Finance"`, `"Procurement"`) with an `EApprovalStage` enum (template TODO N-19).
7. Refresh the seeded role bundles in `AccessFunctionCatalog` to match your real org structure.
