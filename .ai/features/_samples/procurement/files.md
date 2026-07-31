# Procurement — File Map

## Owned files (delete-with-feature)

### Backend entities and DTOs

| Path | Purpose |
| --- | --- |
| `src/backend/Libraries/Domain/Models/Vendor.cs` | Vendor entity (master data) |
| `src/backend/Libraries/Domain/Models/CatalogItem.cs` | Catalog item per vendor |
| `src/backend/Libraries/Domain/Models/PurchaseOrder.cs` | PO header with status workflow |
| `src/backend/Libraries/Domain/Models/PurchaseOrderLine.cs` | PO line items (qty + unit price) |
| `src/backend/Libraries/Domain/Models/PurchaseOrderApproval.cs` | Approval chain rows |
| `src/backend/Libraries/Domain/Models/PurchaseOrderDocument.cs` | Document attachment with hard FK to PO |
| `src/backend/Libraries/Domain/Dto/VendorDto.cs` | Vendor DTO |
| `src/backend/Libraries/Domain/Dto/CatalogItemDto.cs` | Catalog item DTO |
| `src/backend/Libraries/Domain/Dto/PurchaseOrderDto.cs` | PO DTO with nested Lines/Approvals/Documents |
| `src/backend/Libraries/Domain/Enum/EPurchaseOrderStatus.cs` | Status enum (Draft, Submitted, PendingManagerApproval, PendingFinanceApproval, PendingProcurementApproval, Approved, Rejected, Cancelled) |

### Backend services and controllers

| Path | Purpose |
| --- | --- |
| `src/backend/Libraries/Services/Services/Vendor/IVendorService.cs` | service interface |
| `src/backend/Libraries/Services/Services/Vendor/VendorService.cs` | service impl |
| `src/backend/Libraries/Services/Services/CatalogItem/ICatalogItemService.cs` | service interface |
| `src/backend/Libraries/Services/Services/CatalogItem/CatalogItemService.cs` | service impl |
| `src/backend/Libraries/Services/Services/PurchaseOrder/IPurchaseOrderService.cs` | service interface |
| `src/backend/Libraries/Services/Services/PurchaseOrder/PurchaseOrderService.cs` | service impl |
| `src/backend/Libraries/Services/Services/PurchaseOrderDocument/IPurchaseOrderDocumentService.cs` | document service interface |
| `src/backend/Libraries/Services/Services/PurchaseOrderDocument/PurchaseOrderDocumentService.cs` | document service impl |
| `src/backend/API/Controllers/VendorController.cs` | CRUD endpoints |
| `src/backend/API/Controllers/CatalogItemController.cs` | CRUD endpoints |
| `src/backend/API/Controllers/PurchaseOrderController.cs` | CRUD + Submit + ProcessApproval + UploadDocument + DeleteDocument |

### Frontend

| Path | Purpose |
| --- | --- |
| `src/frontend/main/src/staff/pages/staff/ProcurementDashboard.vue` | Main dashboard with KPIs |
| `src/frontend/main/src/staff/pages/staff/VendorManagement.vue` | Vendor list/CRUD |
| `src/frontend/main/src/staff/pages/staff/CatalogItems.vue` | Catalog list/CRUD |
| `src/frontend/main/src/staff/pages/staff/NewPurchaseRequest.vue` | Create PO |
| `src/frontend/main/src/staff/pages/staff/OrderHistory.vue` | PO list |
| `src/frontend/main/src/staff/pages/staff/PurchaseOrderDetail.vue` | PO detail + approval actions |
| `src/frontend/main/src/staff/pages/staff/ApprovalQueue.vue` | Pending approvals |
| `src/frontend/main/src/services/vendorService.ts` | Vendor API client |
| `src/frontend/main/src/services/catalogItemService.ts` | Catalog API client |
| `src/frontend/main/src/services/purchaseOrderService.ts` | PO API client |

## Touched files (line-level edits required when removing the feature)

| Path | What it contains | Why must be touched |
| --- | --- | --- |
| `src/backend/Libraries/Data/Data/NieTemplateDbContext.cs` | DbSets + relationship configs + procurement Code seed rows | Remove DbSets, OnModelCreating procurement block, HasData procurement Code rows |
| `src/backend/API/Program.cs` | usings + 4 service DI registrations | Remove |
| `src/backend/API/Mapping/MappingProfile.cs` | 6 Mapster configs | Remove |
| `src/backend/API/Extensions/DatabaseSeeder.cs` | SeedVendorsAsync / SeedCatalogItemsAsync / SeedPurchaseOrdersAsync + procurement Code rows in SeedCodesAsync | Remove |
| `src/backend/Libraries/Domain/Security/AccessFunctionCatalog.cs` | 7 ProcurementXxx const strings + 7 SeedDefinition entries + role bundles | Remove |
| `src/backend/Libraries/Domain/Enum/ECodeType.cs` | VENDOR_CATEGORY, CATALOG_CATEGORY, UNIT_OF_MEASURE, DELIVERY_LOCATION, CURRENCY | Remove unused values |
| `src/backend/Libraries/Domain/Enum/ECodeName.cs` | procurement Name values | Remove |
| `src/frontend/main/src/router/index.ts` | 7 procurement routes | Remove |
| `src/frontend/main/src/staff/layouts/StaffLayout.vue` | `procurement.${routeName}` feedback function id | Replace with project namespace |
| `src/frontend/main/src/composables/usePermissions.ts` | PRIMARY_NAV_ITEMS procurement entries + procurement role labels | Remove |
| `src/frontend/main/src/constants/permissions.ts` | Procurement* constants + UiPermission Vendor/Catalog + LEGACY_ROLE_PERMISSIONS procurement entries + ACCESS_FUNCTION_PERMISSION_MAP procurement keys | Remove |
| `tests/specs/fixtures/test-config.ts` | vendor + purchaseOrder ApiEndpoints + procurement Routes | Remove |

## Migrations

| Migration | What it does |
| --- | --- |
| `20260407105535_AddProcurementEntities.cs` | Initial procurement schema |
| (created on removal) `<timestamp>_RemoveProcurementSamples.cs` | Drops all procurement tables and seeded rows |

## External dependencies

None — procurement uses only what already ships in the template (EF Core, Mapster, Mapster + the FileStorage service).
