# Task 0002 — Remove SampleModel Scaffolding

> **Why:** SampleModel was the original demo entity. The Procurement reference sample replaced it. This task removes SampleModel from a derived repo. Procurement stays — it is the active reference sample. Remove procurement separately via task 0003 once your real entities exist.

## Pre-checks

```bash
test -f src/backend/Libraries/Domain/Models/SampleModel.cs || { echo "Already removed; skipping."; exit 0; }
```

## 1. Files to delete

```text
src/backend/API/Controllers/SampleModelController.cs
src/backend/Libraries/Domain/Models/SampleModel.cs
src/backend/Libraries/Domain/Models/SampleChildModel.cs
src/backend/Libraries/Domain/Dto/SampleModelDto.cs
src/backend/Libraries/Domain/Dto/SampleChildDto.cs
src/backend/Libraries/Domain/Enum/ESampleEnum.cs
src/backend/Libraries/Services/Services/SampleModel/    # entire folder
tests/specs/api/sample-model.api.spec.ts
```

## 2. Files to edit (line by line)

### `src/backend/Libraries/Domain/Models/Document.cs`

**Edit 1 — replace SampleModel coupling with polymorphic owner.**

```diff
 namespace Domain.Models;

 public class Document : TimestampedEntity
 {
     public string FilePath { get; set; } = default!;
     public long FileSize { get; set; } = default!;
     public string UserFileName { get; set; } = default!;

-    public int SampleModelId { get; set; }
-    public virtual SampleModel SampleModel { get; set; } = default!;
+    /// <summary>
+    /// Optional polymorphic owner type (e.g. "PurchaseOrder", "Profile"). Apps wire owner-specific
+    /// linking entities (like PurchaseOrderDocument) when they need a hard FK; this pair is for
+    /// loose attachments that do not require a relational FK.
+    /// </summary>
+    public string? OwnerType { get; set; }
+
+    /// <summary>
+    /// Optional polymorphic owner id paired with OwnerType. Null when the document is owner-less.
+    /// </summary>
+    public int? OwnerId { get; set; }
 }
```

**Why:** removing SampleModel breaks the FK. The polymorphic pair gives Document a generic linking story without forcing every entity to define its own document linker entity. Procurement keeps its own `PurchaseOrderDocument` because it needs a strict FK; that pattern stays valid.

---

### `src/backend/Libraries/Data/Data/NieTemplateDbContext.cs`

**Edit 2 — drop the Sample DbSets.**

```diff
-    public DbSet<SampleModel> SampleModels { get; set; } = default!;
-    public DbSet<SampleChildModel> SampleChildModels { get; set; } = default!;
     public DbSet<Document> Documents { get; set; } = default!;
```

**Edit 3 — drop the AISolution relationship block; add Document polymorphic index.**

```diff
-        // AISolution
-        modelBuilder.Entity<SampleModel>()
-            .HasMany(p => p.ChildModels)
-            .WithOne(t => t.SampleModel)
-            .HasForeignKey(t => t.SampleModelId)
-            .OnDelete(DeleteBehavior.Cascade);
-
-        // Procurement relationships
+        // Document configuration (polymorphic owner; no hard FK from this layer)
+        modelBuilder.Entity<Document>()
+            .HasIndex(e => new { e.OwnerType, e.OwnerId });
+
+        // Procurement relationships
```

**Edit 4 — drop the commented duplicate sample collection block.**

```diff
-        //Repeat this for all collections
-        //modelBuilder.Entity<SampleModel>()
-        //    .HasMany(p => p.ChildModels1)
-        //    .WithOne(u => u.SampleModel)
-        //    .HasForeignKey(u => u.SampleModelId)
-        //    .OnDelete(DeleteBehavior.Cascade);
-
         // Code table configuration
```

**Why:** the commented block is dead noise from the original sample.

---

### `src/backend/API/Program.cs`

**Edit 5 — drop the AISolution using, add the renamed Code namespace.**

```diff
 using Domain.Services;
-using Domain.Services.AISolution;
+using Domain.Services.Code;
 using Domain.Services.Document;
```

**Edit 6 — drop the SampleModel DI registration.**

```diff
         // Add services to the container
-        builder.Services.AddScoped<ISampleModelService, SampleModelService>();
         builder.Services.AddScoped<ICodeService, CodeService>();
```

**Why:** `Domain.Services.AISolution` was the sample-model namespace. `ICodeService` lives there for legacy reasons; the namespace gets renamed to `Domain.Services.Code` in this task.

---

### `src/backend/API/Mapping/MappingProfile.cs`

**Edit 7 — drop SampleModel and SampleChildModel mappings.**

```diff
         // Document mappings
         TypeAdapterConfig<Document, DocumentDto>.NewConfig();
         TypeAdapterConfig<DocumentDto, Document>.NewConfig();

-        // SampleModel mappings
-        TypeAdapterConfig<SampleModel, SampleModelDto>.NewConfig()
-            .Map(dest => dest.ChildModels, src => src.ChildModels.Adapt<List<SampleChildDto>>());
-
-        TypeAdapterConfig<SampleModelDto, SampleModel>.NewConfig();
-
-        // SampleChildModel mappings
-        TypeAdapterConfig<SampleChildModel, SampleChildDto>.NewConfig();
-        TypeAdapterConfig<SampleChildDto, SampleChildModel>.NewConfig();
-
         // Vendor mappings
```

---

### `src/backend/API/Extensions/DatabaseSeeder.cs`

**Edit 8 — drop SeedSampleModelsAsync from SeedAsync.**

```diff
         await SeedRolesAsync(context);
         await SeedCodesAsync(context);
         await SeedAccessFunctionsAsync(context);
-        await SeedSampleModelsAsync(context);
         await SeedVendorsAsync(context);
```

**Edit 9 — delete the entire SeedSampleModelsAsync method block.**

```diff
-    private static async Task SeedSampleModelsAsync(MainDbContext context)
-    {
-        if (await context.SampleModels.AnyAsync()) return;
-
-        context.SampleModels.AddRange(
-            new SampleModel { MandatoryField = "Demo Item 1", UserId = "seeder", SampleEnum = ESampleEnum.SampleValue1, IsActive = true, CreatedBy = "seeder", CreatedOn = DateTimeHelper.Now },
-            new SampleModel { MandatoryField = "Demo Item 2", UserId = "seeder", SampleEnum = ESampleEnum.SampleValue2, IsActive = true, CreatedBy = "seeder", CreatedOn = DateTimeHelper.Now }
-        );
-        await context.SaveChangesAsync();
-    }
-
     private static async Task SeedVendorsAsync(MainDbContext context)
```

**Edit 10 — replace fragile string lookup with enum lookup in SeedUserRolesAsync.**

```diff
-        var adminRole = await context.Roles.FirstAsync(r => r.Code == "admin");
+        var adminRole = await context.Roles.FirstAsync(r => r.Id == (int)ERole.Administrator);
```

**Why:** `r.Code == "admin"` does not match the seeded role code (`SYSTEM_ADMIN`). The fix uses the `ERole` enum.

---

### `src/backend/Libraries/Domain/Security/AccessFunctionCatalog.cs`

**Edit 11 — remove `SampleRead` and `SampleManage` constants; add Procurement constants.**

```diff
     public static class Api
     {
         public const string CodeRead = "api.code.read";
-        public const string SampleRead = "api.sample-model.read";
-        public const string SampleManage = "api.sample-model.manage";
         public const string DocumentDownload = "api.document.download";
         public const string DocumentManage = "api.document.manage";
         public const string AuditRead = "api.audit-log.read";
         public const string AccessControlRead = "api.access-control.read";
         public const string AccessControlRolesManage = "api.access-control.roles.manage";
         public const string AccessControlAssignmentsManage = "api.access-control.assignments.manage";
+
+        // Procurement sample-feature access functions (kept as reference; remove via task 0003 in derived repos)
+        public const string ProcurementVendorRead = "api.procurement.vendor.read";
+        public const string ProcurementVendorManage = "api.procurement.vendor.manage";
+        public const string ProcurementCatalogRead = "api.procurement.catalog.read";
+        public const string ProcurementCatalogManage = "api.procurement.catalog.manage";
+        public const string ProcurementOrderRead = "api.procurement.order.read";
+        public const string ProcurementOrderManage = "api.procurement.order.manage";
+        public const string ProcurementOrderApprove = "api.procurement.order.approve";
     }
 }
```

**Edit 12 — remove the two `SampleRead` and `SampleManage` `AccessFunctionSeedDefinition` entries from the `AccessFunctions` list, and append the seven Procurement entries** (full content in the template's current `AccessFunctionCatalog.cs`).

**Edit 13 — remove `Api.SampleRead` and `Api.SampleManage` from every `Roles` entry. The exact role bundles after this edit:**

- Manager: keep DashboardView, OperationsView, ReportsView, AuditView, CodeRead, DocumentDownload, DocumentManage, AuditRead — and add all seven Procurement codes.
- User: keep DashboardView, OperationsView, CodeRead, DocumentDownload, DocumentManage — add ProcurementVendorRead, ProcurementCatalogRead, ProcurementOrderRead, ProcurementOrderManage.
- Viewer: keep DashboardView, ReportsView, CodeRead, DocumentDownload — add ProcurementVendorRead, ProcurementCatalogRead, ProcurementOrderRead.

**Edit 14 — fix the `DocumentManage` resource description.**

```diff
-                "DocumentController.UploadFile/DeleteFile and SampleModel document actions",
+                "DocumentController.UploadFile/DeleteFile",
```

---

### `src/backend/Libraries/Services/Services/Code/ICodeService.cs` and `CodeService.cs`

**Edit 15 — rename namespace.**

```diff
-namespace Domain.Services.AISolution;
+namespace Domain.Services.Code;
```

### `src/backend/API/Controllers/CodeController.cs`

**Edit 16 — update the using.**

```diff
-using Domain.Services.AISolution;
+using Domain.Services.Code;
```

---

### `src/backend/API/Controllers/CatalogItemController.cs` and `VendorController.cs` and `PurchaseOrderController.cs`

**Edit 17 — replace every `[RequireAccessFunction(AccessFunctionCodes.Api.SampleRead)]` and `[RequireAccessFunction(AccessFunctionCodes.Api.SampleManage)]` with the matching Procurement code:**

| Controller | SampleRead → | SampleManage → |
| --- | --- | --- |
| `CatalogItemController.cs` | `ProcurementCatalogRead` | `ProcurementCatalogManage` |
| `VendorController.cs` | `ProcurementVendorRead` | `ProcurementVendorManage` |
| `PurchaseOrderController.cs` | `ProcurementOrderRead` | `ProcurementOrderManage` |

**Edit 18 — `PurchaseOrderController.ProcessApproval`** specifically uses `ProcurementOrderApprove` (overrides the table above).

---

### `src/frontend/main/src/constants/permissions.ts`

**Edit 19 — replace Sample mirrors with Procurement mirrors.**

```diff
   Api: {
     CodeRead: "api.code.read",
-    SampleRead: "api.sample-model.read",
-    SampleManage: "api.sample-model.manage",
     DocumentDownload: "api.document.download",
     DocumentManage: "api.document.manage",
     AuditRead: "api.audit-log.read",
     AccessControlRead: "api.access-control.read",
     AccessControlRolesManage: "api.access-control.roles.manage",
     AccessControlAssignmentsManage: "api.access-control.assignments.manage",
+    // Procurement sample feature (kept as reference; remove via task 0003 in derived repos)
+    ProcurementVendorRead: "api.procurement.vendor.read",
+    ProcurementVendorManage: "api.procurement.vendor.manage",
+    ProcurementCatalogRead: "api.procurement.catalog.read",
+    ProcurementCatalogManage: "api.procurement.catalog.manage",
+    ProcurementOrderRead: "api.procurement.order.read",
+    ProcurementOrderManage: "api.procurement.order.manage",
+    ProcurementOrderApprove: "api.procurement.order.approve",
   },
 } as const;
```

---

### `tests/specs/fixtures/test-config.ts`

**Edit 20 — replace `sampleModel` route/endpoint blocks with `vendor` + `purchaseOrder` reference blocks** (use the current template content as reference).

## 3. Files to create

Generate the EF migration:

```bash
dotnet ef migrations add RemoveSampleModelAndPolymorphicDocument \
  --project src/backend/Libraries/Data \
  --startup-project src/backend/API
```

This produces both `<timestamp>_RemoveSampleModelAndPolymorphicDocument.cs` and its Designer file. Inspect the migration: it should drop `FK_Documents_SampleModels_SampleModelId`, drop `SampleChildModels`, drop `SampleModels`, drop `SampleModelId` column on `Documents`, add `OwnerType` and `OwnerId` columns, add an index on `(OwnerType, OwnerId)`, and `UpdateData` rows in the `AccessFunctions` table where Sample functions previously existed.

## 4. Verification

```bash
dotnet build src/backend/NieTemplate.sln                                # exit 0
grep -rn "SampleModel\|SampleChildModel\|ESampleEnum" src/ tests/        # 0 matches
grep -rn "Domain\.Services\.AISolution" src/                             # 0 matches
pnpm --filter main type-check                                            # exit 0
dotnet ef database update --project src/backend/Libraries/Data --startup-project src/backend/API   # applies cleanly
pnpm --filter tests test                                                 # passes (no sample-model spec)
```

## 5. Rollback

```bash
git restore --staged --worktree src/ tests/ .ai/
dotnet ef migrations remove --project src/backend/Libraries/Data --startup-project src/backend/API
```
