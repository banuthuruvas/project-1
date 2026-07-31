using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderWorkflowState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkflowState",
                table: "PurchaseOrders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "WorkflowStateLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PerformedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PerformedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PerformedByRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransitionedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OwnerType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStateLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequiredRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequiresRemarks = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    UiConditions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Code", "Description", "DisplayOrder", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.workflow.read", "Read workflow state, history, and available transitions.", 145, "Workflow", "Read workflow state", "WorkflowController.Get*", "/api/Workflow" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.workflow.transition", "Move workflow-enabled records through configured states.", 146, "POST", "Workflow", "Transition workflow state", "WorkflowController.TransitionState", "/api/Workflow" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.report.read", "Preview and download application reports.", 147, "GET/POST", "Reporting", "Generate reports", "ReportController.*", "/api/Report" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.chat.use", "Use AI chat conversations and streaming responses.", 148, "GET/POST/DELETE", "AI", "Use AI chat", "ChatController.*", "/api/Chat" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Code", "Description", "DisplayOrder", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.audit-log.read", "Read audit history, summaries, and entity change details.", 150, "Audit", "Read audit logs", "AuditLogController.Get*", "/api/AuditLog" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.access-control.read", "Read roles, assignments, and access function configuration.", 160, "GET", "Administration", "Read access control", "AccessControlController.Get*", "/api/AccessControl" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.access-control.roles.manage", "Create, update, or delete roles and their access functions.", 170, "POST/DELETE", "Administration", "Manage roles", "AccessControlController.CreateRole/UpdateRole/DeleteRole", "/api/AccessControl" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.access-control.assignments.manage", "Assign or remove roles for users.", 180, "POST/DELETE", "Administration", "Manage role assignments", "AccessControlController.AssignRole/RemoveAssignment", "/api/AccessControl" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Code", "Description", "DisplayOrder", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.vendor.read", "Read vendor records.", 200, "Read vendors", "VendorController.Get*", "/api/Vendor" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Code", "Description", "DisplayOrder", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.vendor.manage", "Create, update, or delete vendor records.", 210, "Manage vendors", "VendorController.Save/Edit/Delete*", "/api/Vendor" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.catalog.read", "Read catalog item records.", 220, "GET", "Read catalog items", "CatalogItemController.Get*", "/api/CatalogItem" });

            migrationBuilder.InsertData(
                table: "AccessFunctions",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedOn", "Description", "DisplayOrder", "HttpMethod", "IsActive", "IsSystemFunction", "Module", "Name", "ResourceName", "Route", "Type", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 20, "api.procurement.catalog.manage", null, null, "Create, update, or delete catalog items.", 230, "POST", true, true, "Procurement", "Manage catalog items", "CatalogItemController.Save/Edit/Delete*", "/api/CatalogItem", 2, null, null },
                    { 21, "api.procurement.order.read", null, null, "Read purchase order records.", 240, "GET", true, true, "Procurement", "Read purchase orders", "PurchaseOrderController.Get*", "/api/PurchaseOrder", 2, null, null },
                    { 22, "api.procurement.order.manage", null, null, "Create, update, or delete purchase orders.", 250, "POST", true, true, "Procurement", "Manage purchase orders", "PurchaseOrderController.Save/Edit/Delete*", "/api/PurchaseOrder", 2, null, null },
                    { 23, "api.procurement.order.approve", null, null, "Approve or reject submitted purchase orders.", 260, "POST", true, true, "Procurement", "Approve purchase orders", "PurchaseOrderController.Approve/Reject", "/api/PurchaseOrder", 2, null, null }
                });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 20, 1 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 21, 1 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 22, 1 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 23, 1 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 24,
                column: "AccessFunctionId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 25,
                column: "AccessFunctionId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 26,
                column: "AccessFunctionId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 27,
                column: "AccessFunctionId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 28,
                column: "AccessFunctionId",
                value: 6);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 29,
                column: "AccessFunctionId",
                value: 7);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 30,
                column: "AccessFunctionId",
                value: 8);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 31,
                column: "AccessFunctionId",
                value: 9);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 32,
                column: "AccessFunctionId",
                value: 10);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 33,
                column: "AccessFunctionId",
                value: 11);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 34,
                column: "AccessFunctionId",
                value: 12);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 13, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 17, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 18, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 19, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 20, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 21, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 22, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 42,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 23, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 43,
                column: "AccessFunctionId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 44,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 2, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 45,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 6, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 46,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 7, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 47,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 8, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 48,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 9, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 49,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 10, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 50,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 11, 2 });

            migrationBuilder.InsertData(
                table: "RoleAccessFunctions",
                columns: new[] { "Id", "AccessFunctionId", "CreatedBy", "CreatedOn", "RoleId", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 51, 12, null, null, 2, null, null },
                    { 52, 17, null, null, 2, null, null },
                    { 53, 19, null, null, 2, null, null },
                    { 56, 1, null, null, 4, null, null },
                    { 57, 3, null, null, 4, null, null },
                    { 58, 6, null, null, 4, null, null },
                    { 59, 7, null, null, 4, null, null },
                    { 60, 9, null, null, 4, null, null },
                    { 61, 11, null, null, 4, null, null },
                    { 62, 12, null, null, 4, null, null },
                    { 63, 17, null, null, 4, null, null },
                    { 64, 19, null, null, 4, null, null }
                });

            migrationBuilder.InsertData(
                table: "WorkflowTransitions",
                columns: new[] { "Id", "CreatedBy", "CreatedOn", "DisplayLabel", "DisplayOrder", "FromState", "IsActive", "RequiredRole", "RequiresRemarks", "ToState", "UiConditions", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 1, null, null, "Submit for Review", 1, "Draft", true, "Administrator", true, "Submitted", null, null, null },
                    { 2, null, null, "Start Review", 1, "Submitted", true, "Manager", false, "UnderReview", null, null, null },
                    { 3, null, null, "Approve", 1, "UnderReview", true, "Manager", true, "Approved", null, null, null },
                    { 4, null, null, "Reject", 2, "UnderReview", true, "Manager", true, "Rejected", null, null, null },
                    { 5, null, null, "Return for Revision", 3, "UnderReview", true, "Manager", true, "ReturnedForRevision", null, null, null },
                    { 6, null, null, "Resubmit", 1, "ReturnedForRevision", true, "Administrator", true, "Submitted", null, null, null },
                    { 7, null, null, "Mark as Completed", 1, "Approved", true, "Administrator", false, "Completed", null, null, null },
                    { 8, null, null, "Cancel", 2, "Draft", true, "Administrator", true, "Cancelled", null, null, null },
                    { 9, null, null, "Cancel", 2, "Submitted", true, "Administrator", true, "Cancelled", null, null, null },
                    { 10, null, null, "Re-open as Draft", 1, "Rejected", true, "Administrator", true, "Draft", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "RoleAccessFunctions",
                columns: new[] { "Id", "AccessFunctionId", "CreatedBy", "CreatedOn", "RoleId", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 54, 21, null, null, 2, null, null },
                    { 55, 22, null, null, 2, null, null },
                    { 65, 21, null, null, 4, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStateLogs_OwnerType_OwnerId",
                table: "WorkflowStateLogs",
                columns: new[] { "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStateLogs_TransitionedAt",
                table: "WorkflowStateLogs",
                column: "TransitionedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_FromState_ToState_RequiredRole",
                table: "WorkflowTransitions",
                columns: new[] { "FromState", "ToState", "RequiredRole" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowStateLogs");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions");

            migrationBuilder.DeleteData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DropColumn(
                name: "WorkflowState",
                table: "PurchaseOrders");

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
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.access-control.assignments.manage", "Assign or remove roles for users.", 180, "POST/DELETE", "Administration", "Manage role assignments", "AccessControlController.AssignRole/RemoveAssignment", "/api/AccessControl" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Code", "Description", "DisplayOrder", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.vendor.read", "Read vendor records.", 200, "Procurement", "Read vendors", "VendorController.Get*", "/api/Vendor" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.vendor.manage", "Create, update, or delete vendor records.", 210, "POST", "Procurement", "Manage vendors", "VendorController.Save/Edit/Delete*", "/api/Vendor" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.catalog.read", "Read catalog item records.", 220, "GET", "Procurement", "Read catalog items", "CatalogItemController.Get*", "/api/CatalogItem" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Module", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.catalog.manage", "Create, update, or delete catalog items.", 230, "POST", "Procurement", "Manage catalog items", "CatalogItemController.Save/Edit/Delete*", "/api/CatalogItem" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Code", "Description", "DisplayOrder", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.order.read", "Read purchase order records.", 240, "Read purchase orders", "PurchaseOrderController.Get*", "/api/PurchaseOrder" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Code", "Description", "DisplayOrder", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.order.manage", "Create, update, or delete purchase orders.", 250, "Manage purchase orders", "PurchaseOrderController.Save/Edit/Delete*", "/api/PurchaseOrder" });

            migrationBuilder.UpdateData(
                table: "AccessFunctions",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Code", "Description", "DisplayOrder", "HttpMethod", "Name", "ResourceName", "Route" },
                values: new object[] { "api.procurement.order.approve", "Approve or reject submitted purchase orders.", 260, "POST", "Approve purchase orders", "PurchaseOrderController.Approve/Reject", "/api/PurchaseOrder" });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 1, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 2, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 3, 3 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 4, 3 });

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
                column: "AccessFunctionId",
                value: 7);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 26,
                column: "AccessFunctionId",
                value: 8);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 27,
                column: "AccessFunctionId",
                value: 9);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 28,
                column: "AccessFunctionId",
                value: 13);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 29,
                column: "AccessFunctionId",
                value: 14);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 30,
                column: "AccessFunctionId",
                value: 15);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 31,
                column: "AccessFunctionId",
                value: 16);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 32,
                column: "AccessFunctionId",
                value: 17);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 33,
                column: "AccessFunctionId",
                value: 18);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 34,
                column: "AccessFunctionId",
                value: 19);

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

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 6, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 7, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 8, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 13, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 15, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 42,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 17, 2 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 43,
                column: "AccessFunctionId",
                value: 18);

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 44,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 1, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 45,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 3, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 46,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 6, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 47,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 7, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 48,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 13, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 49,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 15, 4 });

            migrationBuilder.UpdateData(
                table: "RoleAccessFunctions",
                keyColumn: "Id",
                keyValue: 50,
                columns: new[] { "AccessFunctionId", "RoleId" },
                values: new object[] { 17, 4 });
        }
    }
}
