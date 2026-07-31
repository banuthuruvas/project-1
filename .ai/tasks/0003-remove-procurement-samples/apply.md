# Task 0003 — Remove Procurement Reference Samples

> **Run this task ONLY after your project has its own real entities** that replace what procurement was teaching you. Procurement is the active reference sample in the template — it stays in the template repo. Derived projects remove it via this task.

## Pre-checks

```bash
test -f src/backend/Libraries/Domain/Models/PurchaseOrder.cs || { echo "Already removed; skipping."; exit 0; }
```

If you still need procurement in production (you actually run a procurement workflow), do NOT run this task — keep the code as your real implementation and rename it.

## 1. Files to delete

### Backend (entities + DTOs + enum)

```text
src/backend/Libraries/Domain/Models/CatalogItem.cs
src/backend/Libraries/Domain/Models/Vendor.cs
src/backend/Libraries/Domain/Models/PurchaseOrder.cs
src/backend/Libraries/Domain/Models/PurchaseOrderLine.cs
src/backend/Libraries/Domain/Models/PurchaseOrderApproval.cs
src/backend/Libraries/Domain/Models/PurchaseOrderDocument.cs
src/backend/Libraries/Domain/Dto/CatalogItemDto.cs
src/backend/Libraries/Domain/Dto/VendorDto.cs
src/backend/Libraries/Domain/Dto/PurchaseOrderDto.cs
src/backend/Libraries/Domain/Enum/EPurchaseOrderStatus.cs
```

### Backend (services and controllers)

```text
src/backend/Libraries/Services/Services/CatalogItem/
src/backend/Libraries/Services/Services/Vendor/
src/backend/Libraries/Services/Services/PurchaseOrder/
src/backend/Libraries/Services/Services/PurchaseOrderDocument/
src/backend/API/Controllers/CatalogItemController.cs
src/backend/API/Controllers/VendorController.cs
src/backend/API/Controllers/PurchaseOrderController.cs
```

### Frontend

```text
src/frontend/main/src/staff/pages/staff/ProcurementDashboard.vue
src/frontend/main/src/staff/pages/staff/NewPurchaseRequest.vue
src/frontend/main/src/staff/pages/staff/PurchaseOrderDetail.vue
src/frontend/main/src/staff/pages/staff/OrderHistory.vue
src/frontend/main/src/staff/pages/staff/ApprovalQueue.vue
src/frontend/main/src/staff/pages/staff/CatalogItems.vue
src/frontend/main/src/staff/pages/staff/VendorManagement.vue
src/frontend/main/src/services/purchaseOrderService.ts
src/frontend/main/src/services/catalogItemService.ts
src/frontend/main/src/services/vendorService.ts
```

## 2. Files to edit (line by line)

### `src/backend/Libraries/Data/Data/NieTemplateDbContext.cs`

**Edit 1 — drop the procurement DbSets block.**

```diff
-    // Procurement entities
-    public DbSet<Vendor> Vendors { get; set; } = default!;
-    public DbSet<CatalogItem> CatalogItems { get; set; } = default!;
-    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = default!;
-    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = default!;
-    public DbSet<PurchaseOrderApproval> PurchaseOrderApprovals { get; set; } = default!;
-    public DbSet<PurchaseOrderDocument> PurchaseOrderDocuments { get; set; } = default!;
-
     // Code tables
     public DbSet<Code> Codes { get; set; } = default!;
```

**Edit 2 — drop the procurement relationship block in `OnModelCreating` (the `Vendor` / `PurchaseOrder` / `PurchaseOrderLine` / `CatalogItem` configurations).**

Locate the `// Procurement relationships` comment and delete from there through the last `modelBuilder.Entity<CatalogItem>().Property(c => c.UnitPrice)...` line.

**Edit 3 — drop procurement Code seed rows from the `HasData` block** (rows with `Type = ECodeType.VENDOR_CATEGORY.ToString()`, `CATALOG_CATEGORY`, `UNIT_OF_MEASURE`, `DELIVERY_LOCATION`, `CURRENCY` — Ids 5–24 in the current template). If your project does need any of these Code types, keep them; otherwise delete.

### `src/backend/API/Program.cs`

**Edit 4 — drop the procurement using statements.**

```diff
-using Domain.Services.Vendor;
-using Domain.Services.CatalogItem;
-using Domain.Services.PurchaseOrder;
-using Domain.Services.PurchaseOrderDocument;
 using Services.Services;
```

**Edit 5 — drop the procurement DI registrations.**

```diff
-        // Add procurement services
-        builder.Services.AddScoped<IVendorService, VendorService>();
-        builder.Services.AddScoped<ICatalogItemService, CatalogItemService>();
-        builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
-        builder.Services.AddScoped<IPurchaseOrderDocumentService, PurchaseOrderDocumentService>();
-
         // Add audit and role management services
```

### `src/backend/API/Mapping/MappingProfile.cs`

**Edit 6 — drop every TypeAdapterConfig referencing Vendor, CatalogItem, PurchaseOrder, PurchaseOrderLine, PurchaseOrderApproval, PurchaseOrderDocument.**

### `src/backend/API/Extensions/DatabaseSeeder.cs`

**Edit 7 — drop calls in `SeedAsync`:**

```diff
         await SeedAccessFunctionsAsync(context);
-        await SeedVendorsAsync(context);
-        await SeedCatalogItemsAsync(context);
-        await SeedPurchaseOrdersAsync(context);
         await SeedUserRolesAsync(context);
```

**Edit 8 — delete the `SeedVendorsAsync`, `SeedCatalogItemsAsync`, and `SeedPurchaseOrdersAsync` method bodies entirely.**

**Edit 9 — drop procurement Code rows from `SeedCodesAsync`** (the `VENDOR_CATEGORY`, `CATALOG_CATEGORY`, `UNIT_OF_MEASURE`, `DELIVERY_LOCATION`, `CURRENCY` AddRange entries).

### `src/backend/Libraries/Domain/Security/AccessFunctionCatalog.cs`

**Edit 10 — drop the Procurement section from `AccessFunctionCodes.Api`** (the 7 `Procurement*` const strings).

**Edit 11 — drop the seven Procurement `AccessFunctionSeedDefinition` entries from the `AccessFunctions` list.**

**Edit 12 — drop Procurement entries from every Role's access function bundle:**

- Manager: drop the 7 Procurement codes.
- User: drop the 4 Procurement codes.
- Viewer: drop the 3 Procurement codes.

### `src/backend/Libraries/Domain/Enum/ECodeType.cs`

**Edit 13 — drop unused enum values** (`VENDOR_CATEGORY`, `CATALOG_CATEGORY`, `UNIT_OF_MEASURE`, `DELIVERY_LOCATION`, `CURRENCY`). If your project uses any, KEEP that one.

### `src/backend/Libraries/Domain/Enum/ECodeName.cs`

**Edit 14 — drop procurement Names** (`IT_SERVICES`, `OFFICE_SUPPLIES`, …, `BRANCH_OFFICE`, `SGD`, `USD`). Keep TITLE/USER_TYPE values.

### `src/frontend/main/src/router/index.ts`

**Edit 15 — replace the procurement routes with your project's dashboard + your real routes.**

Specifically remove these route entries: `vendors`, `catalog`, `new-purchase-request`, `approvals`, `orders`, `purchase-order/:id`. Update the `path: ""` (dashboard) `component:` to your project's `Dashboard.vue` instead of `ProcurementDashboard.vue`.

### `src/frontend/main/src/staff/layouts/StaffLayout.vue`

**Edit 16 — replace the procurement-namespaced feedback function id with a project namespace.**

```diff
-const feedbackFunctionId = computed(
-  () => `procurement.${String(route.name ?? "page")}`,
-);
+const feedbackFunctionId = computed(
+  () => `<your-project>.${String(route.name ?? "page")}`,
+);
```

### `src/frontend/main/src/composables/usePermissions.ts`

**Edit 17 — replace `PRIMARY_NAV_ITEMS` with your project's nav items (drop `Vendors`, `Catalog`, `New Purchase Request`, `Approvals`, `Order History`).**

**Edit 18 — drop procurement role labels in `userRoleLabel` computed:**

```diff
     const labels: Record<string, string> = {
       SystemAdmin: "System Administrator",
-      ProcurementManager: "Procurement Manager",
-      FinanceApprover: "Finance Approver",
       Manager: "Manager",
-      Requester: "Requester",
       Admin: "Administrator",
     };
```

### `src/frontend/main/src/constants/permissions.ts`

**Edit 19 — drop the seven `Procurement*` constants in `AccessFunctionCode.Api`.**

**Edit 20 — drop `VendorManage` and `CatalogManage` from `UiPermission`.**

**Edit 21 — drop the procurement entries from `LEGACY_ROLE_PERMISSIONS`** (Requester, ProcurementManager, FinanceApprover entries entirely; Manager and SystemAdmin lose `CreatePurchaseOrder`, `ApproveOrders`, `ViewAllOrders`, `ManageVendors`, `ManageCatalog`, `ViewReports`, `ViewOwnOrders`).

**Edit 22 — drop entries in `ACCESS_FUNCTION_PERMISSION_MAP` keyed by `UiPermission.VendorManage` and `UiPermission.CatalogManage`.**

### `tests/specs/fixtures/test-config.ts`

**Edit 23 — drop the `vendors`, `catalog`, `orderHistory`, `approvals` entries in `Routes`. Drop the `vendor` and `purchaseOrder` blocks in `ApiEndpoints`.**

## 3. Files to create

```bash
dotnet ef migrations add RemoveProcurementSamples \
  --project src/backend/Libraries/Data \
  --startup-project src/backend/API
```

The migration must drop: `PurchaseOrderDocuments`, `PurchaseOrderApprovals`, `PurchaseOrderLines`, `PurchaseOrders`, `CatalogItems`, `Vendors`. Inspect before applying.

## 4. Verification

```bash
dotnet build src/backend/NieTemplate.sln
grep -rn "Procurement\|PurchaseOrder\|CatalogItem\|Vendor" src/backend/ src/frontend/main/src/   # 0 matches
pnpm --filter main type-check
dotnet ef database update --project src/backend/Libraries/Data --startup-project src/backend/API
pnpm --filter tests test
```

## 5. Rollback

```bash
git restore --staged --worktree src/ tests/
dotnet ef migrations remove --project src/backend/Libraries/Data --startup-project src/backend/API
```
