using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSampleModelAndPolymorphicDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_SampleModels_SampleModelId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "SampleChildModels");

            migrationBuilder.DropTable(
                name: "SampleModels");

            migrationBuilder.DropIndex(
                name: "IX_Documents_SampleModelId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SampleModelId",
                table: "Documents");

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Documents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerType",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.DropIndex(
                name: "IX_AccessFunctions_Code",
                table: "AccessFunctions");

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Code", "Description", "DisplayOrder", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.document.download", "Download stored documents.", 130, "Documents", "Download documents", "DocumentController.DownloadFile", "/api/Document/DownloadFile" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Code", "Description", "DisplayOrder", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.document.manage", "Upload or delete stored documents.", 140, "Documents", "Manage documents", "DocumentController.UploadFile/DeleteFile", "/api/Document" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Code", "Description", "DisplayOrder", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.audit-log.read", "Read audit history, summaries, and entity change details.", 150, "Audit", "Read audit logs", "AuditLogController.Get*", "/api/AuditLog" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.access-control.read", "Read roles, assignments, and access function configuration.", 160, "GET", "Administration", "Read access control", "AccessControlController.Get*", "/api/AccessControl" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.access-control.roles.manage", "Create, update, or delete roles and their access functions.", 170, "POST/DELETE", "Administration", "Manage roles", "AccessControlController.CreateRole/UpdateRole/DeleteRole", "/api/AccessControl" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Name", "ResourceName" },
                values: new object[] { "api.access-control.assignments.manage", "Assign or remove roles for users.", 180, "POST/DELETE", "Manage role assignments", "AccessControlController.AssignRole/RemoveAssignment" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.vendor.read", "Read vendor records.", 200, "GET", "Procurement", "Read vendors", "VendorController.Get*", "/api/Vendor" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.vendor.manage", "Create, update, or delete vendor records.", 210, "POST", "Procurement", "Manage vendors", "VendorController.Save/Edit/Delete*", "/api/Vendor" });

            migrationBuilder.InsertData(
                table: "AccessFunctions",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedOn", "Description", "DisplayOrder", "HttpMethod", "IsActive", "IsSystemFunction", "Module", "Name", "ResourceName", "Route", "Type", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 15, "api.procurement.catalog.read", null, null, "Read catalog item records.", 220, "GET", true, true, "Procurement", "Read catalog items", "CatalogItemController.Get*", "/api/CatalogItem", 2, null, null },
                    { 16, "api.procurement.catalog.manage", null, null, "Create, update, or delete catalog items.", 230, "POST", true, true, "Procurement", "Manage catalog items", "CatalogItemController.Save/Edit/Delete*", "/api/CatalogItem", 2, null, null },
                    { 17, "api.procurement.order.read", null, null, "Read purchase order records.", 240, "GET", true, true, "Procurement", "Read purchase orders", "PurchaseOrderController.Get*", "/api/PurchaseOrder", 2, null, null },
                    { 18, "api.procurement.order.manage", null, null, "Create, update, or delete purchase orders.", 250, "POST", true, true, "Procurement", "Manage purchase orders", "PurchaseOrderController.Save/Edit/Delete*", "/api/PurchaseOrder", 2, null, null },
                    { 19, "api.procurement.order.approve", null, null, "Approve or reject submitted purchase orders.", 260, "POST", true, true, "Procurement", "Approve purchase orders", "PurchaseOrderController.Approve/Reject", "/api/PurchaseOrder", 2, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessFunctions_Code",
                table: "AccessFunctions",
                column: "Code",
                unique: true);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 15, 1 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 16, 1 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 17, 1 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 18, 1 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 19, 1 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 20,
                column: "AccessFunctionId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 21,
                column: "AccessFunctionId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 22,
                column: "AccessFunctionId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 23,
                column: "AccessFunctionId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 24,
                column: "AccessFunctionId",
                value: 6);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 7, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 8, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 9, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 13, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 14, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 15, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 16, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 17, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 18, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 19, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 1, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 2, 2 });

            migrationBuilder.InsertData(
                table: "RoleAccessFunctions",
                columns: new[] { "Id", "AccessFunctionId", "CreatedBy", "CreatedOn", "RoleId", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 37, 6, null, null, 2, null, null },
                    { 38, 7, null, null, 2, null, null },
                    { 39, 8, null, null, 2, null, null },
                    { 40, 13, null, null, 2, null, null },
                    { 44, 1, null, null, 4, null, null },
                    { 45, 3, null, null, 4, null, null },
                    { 46, 6, null, null, 4, null, null },
                    { 47, 7, null, null, 4, null, null },
                    { 48, 13, null, null, 4, null, null },
                    { 41, 15, null, null, 2, null, null },
                    { 42, 17, null, null, 2, null, null },
                    { 43, 18, null, null, 2, null, null },
                    { 49, 15, null, null, 4, null, null },
                    { 50, 17, null, null, 4, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OwnerType_OwnerId",
                table: "Documents",
                columns: new[] { "OwnerType", "OwnerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_OwnerType_OwnerId",
                table: "Documents");

            migrationBuilder.DeleteData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "Documents");

            migrationBuilder.AddColumn<int>(
                name: "SampleModelId",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SampleModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MandatoryField = table.Column<string>(type: "text", nullable: false),
                    NonMandatoryField = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    SampleEnum = table.Column<int>(type: "integer", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SampleChildModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SampleModelId = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MandatoryField = table.Column<string>(type: "text", nullable: false),
                    NonMandatoryField = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleChildModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleChildModels_SampleModels_SampleModelId",
                        column: x => x.SampleModelId,
                        principalTable: "SampleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Code", "Description", "DisplayOrder", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.sample-model.read", "Read sample-model data and related records.", 110, "Sample Model", "Read sample entities", "SampleModelController.Get*", "/api/SampleModel" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Code", "Description", "DisplayOrder", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.sample-model.manage", "Create, update, or delete sample-model records.", 120, "Sample Model", "Manage sample entities", "SampleModelController.Save/Edit/Delete*", "/api/SampleModel" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Code", "Description", "DisplayOrder", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.document.download", "Download stored documents.", 130, "Documents", "Download documents", "DocumentController.DownloadFile", "/api/Document/DownloadFile" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.document.manage", "Upload or delete stored documents.", 140, "POST", "Documents", "Manage documents", "DocumentController.UploadFile/DeleteFile and SampleModel document actions", "/api/Document" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.audit-log.read", "Read audit history, summaries, and entity change details.", 150, "GET", "Audit", "Read audit logs", "AuditLogController.Get*", "/api/AuditLog" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Name", "ResourceName" },
                values: new object[] { "api.access-control.read", "Read roles, assignments, and access function configuration.", 160, "GET", "Read access control", "AccessControlController.Get*" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.access-control.roles.manage", "Create, update, or delete roles and their access functions.", 170, "POST/DELETE", "Administration", "Manage roles", "AccessControlController.CreateRole/UpdateRole/DeleteRole", "/api/AccessControl" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.access-control.assignments.manage", "Assign or remove roles for users.", 180, "POST/DELETE", "Administration", "Manage role assignments", "AccessControlController.AssignRole/RemoveAssignment", "/api/AccessControl" });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 1, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 2, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 3, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 4, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 6, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 20,
                column: "AccessFunctionId",
                value: 7);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 21,
                column: "AccessFunctionId",
                value: 8);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 22,
                column: "AccessFunctionId",
                value: 9);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 23,
                column: "AccessFunctionId",
                value: 10);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 24,
                column: "AccessFunctionId",
                value: 11);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 1, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 2, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 6, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 7, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 8, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 9, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 10, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 1, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 3, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 6, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 7, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 9, 4 });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SampleModelId",
                table: "Documents",
                column: "SampleModelId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleChildModels_SampleModelId",
                table: "SampleChildModels",
                column: "SampleModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_SampleModels_SampleModelId",
                table: "Documents",
                column: "SampleModelId",
                principalTable: "SampleModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
