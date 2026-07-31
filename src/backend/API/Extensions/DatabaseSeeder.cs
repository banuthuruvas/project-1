using Data.Data;
using Domain.Enum;
using Domain.Models;
using Domain.Security;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;

namespace API.Extensions;

/// <summary>
/// Database seeder that populates demo data for development and testing.
/// Usage: dotnet run -- seed
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(MainDbContext context)
    {
        // Ensure database is migrated
        await context.Database.MigrateAsync();

        await SeedRolesAsync(context);
        await SeedCodesAsync(context);
        await SeedAccessFunctionsAsync(context);
        await SeedVendorsAsync(context);
        await SeedCatalogItemsAsync(context);
        await SeedPurchaseOrdersAsync(context);
        await SeedUserRolesAsync(context);
    }

    private static async Task SeedRolesAsync(MainDbContext context)
    {
        if (await context.Roles.AnyAsync()) return;

        var now = DateTimeHelper.Now;

        context.Roles.AddRange(
            AccessFunctionCatalog.Roles.Select(role => new Role
            {
                Id = role.Id,
                Code = role.Code,
                Name = role.Name,
                Description = role.Description,
                IsActive = true,
                IsSystemRole = true,
                DisplayOrder = role.DisplayOrder,
                CreatedBy = "seeder",
                CreatedOn = now
            }));

        await context.SaveChangesAsync();
    }

    private static async Task SeedCodesAsync(MainDbContext context)
    {
        if (await context.Codes.AnyAsync()) return;

        context.Codes.AddRange(
            // Statuses
            new Code { Type = "Status", Name = "Active", DisplayName = "Active", Description = "Active status", DisplayOrder = 1 },
            new Code { Type = "Status", Name = "Inactive", DisplayName = "Inactive", Description = "Inactive status", DisplayOrder = 2 },
            // Priorities
            new Code { Type = "Priority", Name = "High", DisplayName = "High", Description = "High priority", DisplayOrder = 1 },
            new Code { Type = "Priority", Name = "Medium", DisplayName = "Medium", Description = "Medium priority", DisplayOrder = 2 },
            new Code { Type = "Priority", Name = "Low", DisplayName = "Low", Description = "Low priority", DisplayOrder = 3 },
            // Vendor categories
            new Code { Type = "VENDOR_CATEGORY", Name = "IT Equipment", DisplayName = "IT Equipment", Description = "IT hardware and software vendors", DisplayOrder = 1 },
            new Code { Type = "VENDOR_CATEGORY", Name = "Office Supplies", DisplayName = "Office Supplies", Description = "Stationery and office supplies", DisplayOrder = 2 },
            new Code { Type = "VENDOR_CATEGORY", Name = "Furniture", DisplayName = "Furniture", Description = "Office furniture vendors", DisplayOrder = 3 },
            new Code { Type = "VENDOR_CATEGORY", Name = "Services", DisplayName = "Services", Description = "Professional services vendors", DisplayOrder = 4 },
            // Catalog categories
            new Code { Type = "CATALOG_CATEGORY", Name = "Hardware", DisplayName = "Hardware", Description = "Computer hardware and peripherals", DisplayOrder = 1 },
            new Code { Type = "CATALOG_CATEGORY", Name = "Software", DisplayName = "Software", Description = "Software licenses and subscriptions", DisplayOrder = 2 },
            new Code { Type = "CATALOG_CATEGORY", Name = "Furniture", DisplayName = "Furniture", Description = "Office furniture items", DisplayOrder = 3 },
            new Code { Type = "CATALOG_CATEGORY", Name = "Stationery", DisplayName = "Stationery", Description = "Stationery and paper products", DisplayOrder = 4 },
            new Code { Type = "CATALOG_CATEGORY", Name = "Cleaning", DisplayName = "Cleaning", Description = "Cleaning supplies and materials", DisplayOrder = 5 },
            // Units of measure
            new Code { Type = "UNIT_OF_MEASURE", Name = "Each", DisplayName = "Each", Description = "Individual unit", DisplayOrder = 1 },
            new Code { Type = "UNIT_OF_MEASURE", Name = "Box", DisplayName = "Box", Description = "Box of items", DisplayOrder = 2 },
            new Code { Type = "UNIT_OF_MEASURE", Name = "Pack", DisplayName = "Pack", Description = "Pack of items", DisplayOrder = 3 },
            new Code { Type = "UNIT_OF_MEASURE", Name = "Ream", DisplayName = "Ream", Description = "Ream of paper (500 sheets)", DisplayOrder = 4 },
            new Code { Type = "UNIT_OF_MEASURE", Name = "Set", DisplayName = "Set", Description = "Set of items", DisplayOrder = 5 },
            // Delivery locations
            new Code { Type = "DELIVERY_LOCATION", Name = "NIE Block 1", DisplayName = "NIE Block 1", Description = "NIE Block 1 Loading Bay", DisplayOrder = 1 },
            new Code { Type = "DELIVERY_LOCATION", Name = "NIE Block 2", DisplayName = "NIE Block 2", Description = "NIE Block 2 Reception", DisplayOrder = 2 },
            new Code { Type = "DELIVERY_LOCATION", Name = "NIE Block 7", DisplayName = "NIE Block 7", Description = "NIE Block 7 Store Room", DisplayOrder = 3 },
            new Code { Type = "DELIVERY_LOCATION", Name = "NIE Library", DisplayName = "NIE Library", Description = "NIE Library Counter", DisplayOrder = 4 },
            // Currency
            new Code { Type = "CURRENCY", Name = "SGD", DisplayName = "SGD", Description = "Singapore Dollar", DisplayOrder = 1 },
            new Code { Type = "CURRENCY", Name = "USD", DisplayName = "USD", Description = "US Dollar", DisplayOrder = 2 }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedAccessFunctionsAsync(MainDbContext context)
    {
        if (await context.AccessFunctions.AnyAsync()) return;

        var now = DateTimeHelper.Now;
        var functions = AccessFunctionCatalog.AccessFunctions
            .Select(definition => new AccessFunction
            {
                Code = definition.Code,
                Name = definition.Name,
                Description = definition.Description,
                Module = definition.Module,
                Type = definition.Type,
                ResourceName = definition.ResourceName,
                Route = definition.Route,
                HttpMethod = definition.HttpMethod,
                IsActive = true,
                IsSystemFunction = true,
                DisplayOrder = definition.DisplayOrder,
                CreatedBy = "seeder",
                CreatedOn = now
            })
            .ToList();

        context.AccessFunctions.AddRange(functions);
        await context.SaveChangesAsync();

        var allFunctions = await context.AccessFunctions.ToListAsync();

        foreach (var roleDefinition in AccessFunctionCatalog.Roles)
        {
            var role = await context.Roles.FirstAsync(existingRole => existingRole.Code == roleDefinition.Code);

            foreach (var function in allFunctions.Where(item => roleDefinition.AccessFunctionCodes.Contains(item.Code)))
            {
                context.RoleAccessFunctions.Add(new RoleAccessFunction
                {
                    RoleId = role.Id,
                    AccessFunctionId = function.Id,
                    CreatedBy = "seeder",
                    CreatedOn = now
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedVendorsAsync(MainDbContext context)
    {
        if (await context.Vendors.AnyAsync()) return;

        context.Vendors.AddRange(
            new Vendor { Name = "Tech Solutions Pte Ltd", Code = "TECH-001", ContactPerson = "John Lim", Email = "sales@techsolutions.sg", Phone = "+65 6789 0123", Address = "1 Science Park Drive, Singapore 118221", Category = "IT Equipment", IsActive = true, CreatedBy = "seeder", CreatedOn = DateTimeHelper.Now },
            new Vendor { Name = "Office Essentials Singapore", Code = "OES-001", ContactPerson = "Sarah Tan", Email = "orders@officeessentials.sg", Phone = "+65 6234 5678", Address = "88 Thomson Road, Singapore 307684", Category = "Office Supplies", IsActive = true, CreatedBy = "seeder", CreatedOn = DateTimeHelper.Now },
            new Vendor { Name = "FurniCraft Industries", Code = "FCI-001", ContactPerson = "David Wong", Email = "info@furnicraft.sg", Phone = "+65 6890 1234", Address = "25 International Business Park, Singapore 609916", Category = "Furniture", IsActive = true, CreatedBy = "seeder", CreatedOn = DateTimeHelper.Now }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedCatalogItemsAsync(MainDbContext context)
    {
        if (await context.CatalogItems.AnyAsync()) return;

        var vendors = await context.Vendors.ToListAsync();
        var techVendor = vendors.First(v => v.Code == "TECH-001");
        var officeVendor = vendors.First(v => v.Code == "OES-001");
        var furniVendor = vendors.First(v => v.Code == "FCI-001");
        var now = DateTimeHelper.Now;

        context.CatalogItems.AddRange(
            // Tech vendor items
            new CatalogItem { Name = "Dell Latitude 5540 Laptop", Sku = "DELL-LAT5540", Description = "14\" business laptop, Intel i7, 16GB RAM, 512GB SSD", Category = "Hardware", UnitOfMeasure = "Each", UnitPrice = 1899.00m, VendorId = techVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Dell 27\" Monitor U2723QE", Sku = "DELL-U2723QE", Description = "27\" 4K UltraSharp USB-C Hub Monitor", Category = "Hardware", UnitOfMeasure = "Each", UnitPrice = 749.00m, VendorId = techVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Microsoft 365 Business License", Sku = "MS365-BIZ", Description = "Annual Microsoft 365 Business Standard license per user", Category = "Software", UnitOfMeasure = "Each", UnitPrice = 264.00m, VendorId = techVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Logitech MX Keys Keyboard", Sku = "LOG-MXKEYS", Description = "Advanced wireless illuminated keyboard", Category = "Hardware", UnitOfMeasure = "Each", UnitPrice = 159.00m, VendorId = techVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            // Office vendor items
            new CatalogItem { Name = "A4 Copy Paper (80gsm)", Sku = "OES-A4PAPER", Description = "Premium A4 paper 80gsm, 500 sheets per ream", Category = "Stationery", UnitOfMeasure = "Ream", UnitPrice = 5.90m, VendorId = officeVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Pilot G-2 Gel Pen (Black)", Sku = "OES-PILOTG2", Description = "Pilot G-2 0.7mm gel pen, box of 12", Category = "Stationery", UnitOfMeasure = "Box", UnitPrice = 18.50m, VendorId = officeVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "3M Post-It Notes (3x3)", Sku = "OES-POSTIT3", Description = "Post-It sticky notes 76x76mm, pack of 12 pads", Category = "Stationery", UnitOfMeasure = "Pack", UnitPrice = 12.80m, VendorId = officeVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Hand Sanitizer (500ml)", Sku = "OES-HSANI500", Description = "Antibacterial hand sanitizer 500ml pump bottle", Category = "Cleaning", UnitOfMeasure = "Each", UnitPrice = 8.90m, VendorId = officeVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            // Furniture vendor items
            new CatalogItem { Name = "Ergonomic Office Chair", Sku = "FCI-ERGOCHAIR", Description = "Adjustable ergonomic mesh office chair with lumbar support", Category = "Furniture", UnitOfMeasure = "Each", UnitPrice = 489.00m, VendorId = furniVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Standing Desk (120x60cm)", Sku = "FCI-STDESK120", Description = "Electric height-adjustable standing desk, white top", Category = "Furniture", UnitOfMeasure = "Each", UnitPrice = 699.00m, VendorId = furniVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedPurchaseOrdersAsync(MainDbContext context)
    {
        if (await context.PurchaseOrders.AnyAsync()) return;

        var vendors = await context.Vendors.ToListAsync();
        var catalogItems = await context.CatalogItems.ToListAsync();
        var techVendor = vendors.First(v => v.Code == "TECH-001");
        var officeVendor = vendors.First(v => v.Code == "OES-001");
        var furniVendor = vendors.First(v => v.Code == "FCI-001");
        var now = DateTimeHelper.Now;

        // PO 1: Draft - IT equipment
        var po1 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0001",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-2),
            DeliveryAddress = "NIE Block 7",
            ExpectedDeliveryDate = now.AddDays(14),
            Status = EPurchaseOrderStatus.Draft,
            Notes = "New laptops for incoming research assistants",
            VendorId = techVendor.Id,
            TotalAmount = 0, // Will calculate below
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-2)
        };

        // PO 2: Submitted - Office supplies
        var po2 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0002",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-5),
            DeliveryAddress = "NIE Block 1",
            ExpectedDeliveryDate = now.AddDays(7),
            Status = EPurchaseOrderStatus.Submitted,
            Notes = "Monthly stationery replenishment for academic group",
            VendorId = officeVendor.Id,
            TotalAmount = 0,
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-5)
        };

        // PO 3: Approved - Furniture
        var po3 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0003",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-10),
            DeliveryAddress = "NIE Block 2",
            ExpectedDeliveryDate = now.AddDays(21),
            Status = EPurchaseOrderStatus.Approved,
            Notes = "Ergonomic furniture upgrade for faculty offices",
            VendorId = furniVendor.Id,
            TotalAmount = 0,
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-10)
        };

        // PO 4: Rejected - Software
        var po4 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0004",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-8),
            DeliveryAddress = "NIE Block 1",
            Status = EPurchaseOrderStatus.Rejected,
            Notes = "Additional software licenses",
            RejectionReason = "Budget exceeded for this quarter. Please resubmit in Q3.",
            VendorId = techVendor.Id,
            TotalAmount = 0,
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-8)
        };

        // PO 5: Pending Manager Approval
        var po5 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0005",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-1),
            DeliveryAddress = "NIE Library",
            ExpectedDeliveryDate = now.AddDays(10),
            Status = EPurchaseOrderStatus.PendingManagerApproval,
            Notes = "Monitors for new hot-desking area",
            VendorId = techVendor.Id,
            TotalAmount = 0,
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-1)
        };

        context.PurchaseOrders.AddRange(po1, po2, po3, po4, po5);
        await context.SaveChangesAsync();

        // Add lines for each PO
        var laptop = catalogItems.First(c => c.Sku == "DELL-LAT5540");
        var monitor = catalogItems.First(c => c.Sku == "DELL-U2723QE");
        var keyboard = catalogItems.First(c => c.Sku == "LOG-MXKEYS");
        var paper = catalogItems.First(c => c.Sku == "OES-A4PAPER");
        var pens = catalogItems.First(c => c.Sku == "OES-PILOTG2");
        var postIt = catalogItems.First(c => c.Sku == "OES-POSTIT3");
        var ms365 = catalogItems.First(c => c.Sku == "MS365-BIZ");
        var chair = catalogItems.First(c => c.Sku == "FCI-ERGOCHAIR");
        var desk = catalogItems.First(c => c.Sku == "FCI-STDESK120");

        // PO1 lines: 3 laptops + 3 keyboards
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po1.Id, LineNumber = 1, ItemName = laptop.Name, Description = laptop.Description, UnitOfMeasure = "Each", Quantity = 3, UnitPrice = laptop.UnitPrice, LineTotal = 3 * laptop.UnitPrice, CatalogItemId = laptop.Id, CreatedBy = "seeder", CreatedOn = now },
            new PurchaseOrderLine { PurchaseOrderId = po1.Id, LineNumber = 2, ItemName = keyboard.Name, Description = keyboard.Description, UnitOfMeasure = "Each", Quantity = 3, UnitPrice = keyboard.UnitPrice, LineTotal = 3 * keyboard.UnitPrice, CatalogItemId = keyboard.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po1.TotalAmount = (3 * laptop.UnitPrice) + (3 * keyboard.UnitPrice);

        // PO2 lines: paper, pens, post-its
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po2.Id, LineNumber = 1, ItemName = paper.Name, Description = paper.Description, UnitOfMeasure = "Ream", Quantity = 20, UnitPrice = paper.UnitPrice, LineTotal = 20 * paper.UnitPrice, CatalogItemId = paper.Id, CreatedBy = "seeder", CreatedOn = now },
            new PurchaseOrderLine { PurchaseOrderId = po2.Id, LineNumber = 2, ItemName = pens.Name, Description = pens.Description, UnitOfMeasure = "Box", Quantity = 5, UnitPrice = pens.UnitPrice, LineTotal = 5 * pens.UnitPrice, CatalogItemId = pens.Id, CreatedBy = "seeder", CreatedOn = now },
            new PurchaseOrderLine { PurchaseOrderId = po2.Id, LineNumber = 3, ItemName = postIt.Name, Description = postIt.Description, UnitOfMeasure = "Pack", Quantity = 3, UnitPrice = postIt.UnitPrice, LineTotal = 3 * postIt.UnitPrice, CatalogItemId = postIt.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po2.TotalAmount = (20 * paper.UnitPrice) + (5 * pens.UnitPrice) + (3 * postIt.UnitPrice);

        // PO3 lines: 4 chairs + 4 desks
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po3.Id, LineNumber = 1, ItemName = chair.Name, Description = chair.Description, UnitOfMeasure = "Each", Quantity = 4, UnitPrice = chair.UnitPrice, LineTotal = 4 * chair.UnitPrice, CatalogItemId = chair.Id, CreatedBy = "seeder", CreatedOn = now },
            new PurchaseOrderLine { PurchaseOrderId = po3.Id, LineNumber = 2, ItemName = desk.Name, Description = desk.Description, UnitOfMeasure = "Each", Quantity = 4, UnitPrice = desk.UnitPrice, LineTotal = 4 * desk.UnitPrice, CatalogItemId = desk.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po3.TotalAmount = (4 * chair.UnitPrice) + (4 * desk.UnitPrice);

        // PO4 lines: 10 M365 licenses
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po4.Id, LineNumber = 1, ItemName = ms365.Name, Description = ms365.Description, UnitOfMeasure = "Each", Quantity = 10, UnitPrice = ms365.UnitPrice, LineTotal = 10 * ms365.UnitPrice, CatalogItemId = ms365.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po4.TotalAmount = 10 * ms365.UnitPrice;

        // PO5 lines: 6 monitors
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po5.Id, LineNumber = 1, ItemName = monitor.Name, Description = monitor.Description, UnitOfMeasure = "Each", Quantity = 6, UnitPrice = monitor.UnitPrice, LineTotal = 6 * monitor.UnitPrice, CatalogItemId = monitor.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po5.TotalAmount = 6 * monitor.UnitPrice;

        await context.SaveChangesAsync();
    }

    private static async Task SeedUserRolesAsync(MainDbContext context)
    {
        if (await context.UserRoles.AnyAsync()) return;

        var adminRole = await context.Roles.FirstAsync(r => r.Id == (int)ERole.Administrator);
        var now = DateTimeHelper.Now;

        // Assign the test user (devia) as admin
        context.UserRoles.Add(new UserRole
        {
            UserId = "devia",
            RoleId = adminRole.Id,
            AssignedOn = now,
            AssignedBy = "seeder",
            IsActive = true,
            CreatedBy = "seeder",
            CreatedOn = now
        });
        await context.SaveChangesAsync();
    }
}
