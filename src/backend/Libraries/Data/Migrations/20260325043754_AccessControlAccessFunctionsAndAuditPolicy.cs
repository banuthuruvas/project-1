using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AccessControlAccessFunctionsAndAuditPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "UserRoles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "UserRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "UserRoles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Roles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Roles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Roles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccessFunctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ResourceName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemFunction = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessFunctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleAccessFunctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    AccessFunctionId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAccessFunctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAccessFunctions_AccessFunctions_AccessFunctionId",
                        column: x => x.AccessFunctionId,
                        principalTable: "AccessFunctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleAccessFunctions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AccessFunctions",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedOn", "Description", "DisplayOrder", "HttpMethod", "IsActive", "IsSystemFunction", "Module", "Name", "ResourceName", "Route", "Type", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 1, "screen.dashboard.view", null, null, "Open the main staff dashboard shell.", 10, null, true, true, "Dashboard", "View dashboard", "dashboard", "/", 1, null, null },
                    { 2, "screen.operations.view", null, null, "Open the operations list and detail workflow.", 20, null, true, true, "Operations", "View operations list", "operations", "/operations", 1, null, null },
                    { 3, "screen.reports.view", null, null, "Open reporting and dashboard summaries.", 30, null, true, true, "Reporting", "View reports", "reports", "/reports", 1, null, null },
                    { 4, "screen.audit.view", null, null, "Open the audit log management screen.", 40, null, true, true, "Audit", "View audit trail", "audit", "/audit", 1, null, null },
                    { 5, "screen.access-control.view", null, null, "Open the access control administration screen.", 50, null, true, true, "Administration", "View access control", "access-control", "/access-control", 1, null, null },
                    { 6, "api.code.read", null, null, "Read code-table and lookup data.", 100, "GET", true, true, "Reference Data", "Read code tables", "CodeController.GetAll", "/api/Code", 2, null, null },
                    { 7, "api.sample-model.read", null, null, "Read sample-model data and related records.", 110, "GET", true, true, "Sample Model", "Read sample entities", "SampleModelController.Get*", "/api/SampleModel", 2, null, null },
                    { 8, "api.sample-model.manage", null, null, "Create, update, or delete sample-model records.", 120, "POST", true, true, "Sample Model", "Manage sample entities", "SampleModelController.Save/Edit/Delete*", "/api/SampleModel", 2, null, null },
                    { 9, "api.document.download", null, null, "Download stored documents.", 130, "GET", true, true, "Documents", "Download documents", "DocumentController.DownloadFile", "/api/Document/DownloadFile", 2, null, null },
                    { 10, "api.document.manage", null, null, "Upload or delete stored documents.", 140, "POST", true, true, "Documents", "Manage documents", "DocumentController.UploadFile/DeleteFile and SampleModel document actions", "/api/Document", 2, null, null },
                    { 11, "api.audit-log.read", null, null, "Read audit history, summaries, and entity change details.", 150, "GET", true, true, "Audit", "Read audit logs", "AuditLogController.Get*", "/api/AuditLog", 2, null, null },
                    { 12, "api.access-control.read", null, null, "Read roles, assignments, and access function configuration.", 160, "GET", true, true, "Administration", "Read access control", "AccessControlController.Get*", "/api/AccessControl", 2, null, null },
                    { 13, "api.access-control.roles.manage", null, null, "Create, update, or delete roles and their access functions.", 170, "POST/DELETE", true, true, "Administration", "Manage roles", "AccessControlController.CreateRole/UpdateRole/DeleteRole", "/api/AccessControl", 2, null, null },
                    { 14, "api.access-control.assignments.manage", null, null, "Assign or remove roles for users.", 180, "POST/DELETE", true, true, "Administration", "Manage role assignments", "AccessControlController.AssignRole/RemoveAssignment", "/api/AccessControl", 2, null, null }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Code", "CreatedBy", "CreatedOn", "Description", "DisplayOrder", "Name", "UpdatedBy", "UpdatedOn" },
                values: new object[] { "SYSTEM_ADMIN", null, null, "Full access to screens, APIs, audit logs, and access control administration.", 10, "System Administrator", null, null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "CreatedBy", "CreatedOn", "Description", "DisplayOrder", "Name", "UpdatedBy", "UpdatedOn" },
                values: new object[] { "OPERATIONS_USER", null, null, "Work on operational records and document actions in the staff workspace.", 30, "Operations User", null, null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Code", "CreatedBy", "CreatedOn", "Description", "DisplayOrder", "Name", "UpdatedBy", "UpdatedOn" },
                values: new object[] { "OPERATIONS_MANAGER", null, null, "Manage operational records and review audit activity without changing access control.", 20, "Operations Manager", null, null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Code", "CreatedBy", "CreatedOn", "Description", "DisplayOrder", "Name", "UpdatedBy", "UpdatedOn" },
                values: new object[] { "READ_ONLY_VIEWER", null, null, "Read-only access to dashboards, reporting, and entity details.", 40, "Read Only Viewer", null, null });

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn" },
                values: new object[] { null, null, null, null });

            migrationBuilder.InsertData(
                table: "RoleAccessFunctions",
                columns: new[] { "Id", "AccessFunctionId", "CreatedBy", "CreatedOn", "RoleId", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 1, 1, null, null, 1, null, null },
                    { 2, 2, null, null, 1, null, null },
                    { 3, 3, null, null, 1, null, null },
                    { 4, 4, null, null, 1, null, null },
                    { 5, 5, null, null, 1, null, null },
                    { 6, 6, null, null, 1, null, null },
                    { 7, 7, null, null, 1, null, null },
                    { 8, 8, null, null, 1, null, null },
                    { 9, 9, null, null, 1, null, null },
                    { 10, 10, null, null, 1, null, null },
                    { 11, 11, null, null, 1, null, null },
                    { 12, 12, null, null, 1, null, null },
                    { 13, 13, null, null, 1, null, null },
                    { 14, 14, null, null, 1, null, null },
                    { 15, 1, null, null, 3, null, null },
                    { 16, 2, null, null, 3, null, null },
                    { 17, 3, null, null, 3, null, null },
                    { 18, 4, null, null, 3, null, null },
                    { 19, 6, null, null, 3, null, null },
                    { 20, 7, null, null, 3, null, null },
                    { 21, 8, null, null, 3, null, null },
                    { 22, 9, null, null, 3, null, null },
                    { 23, 10, null, null, 3, null, null },
                    { 24, 11, null, null, 3, null, null },
                    { 25, 1, null, null, 2, null, null },
                    { 26, 2, null, null, 2, null, null },
                    { 27, 6, null, null, 2, null, null },
                    { 28, 7, null, null, 2, null, null },
                    { 29, 8, null, null, 2, null, null },
                    { 30, 9, null, null, 2, null, null },
                    { 31, 10, null, null, 2, null, null },
                    { 32, 1, null, null, 4, null, null },
                    { 33, 3, null, null, 4, null, null },
                    { 34, 6, null, null, 4, null, null },
                    { 35, 7, null, null, 4, null, null },
                    { 36, 9, null, null, 4, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessFunctions_Code",
                table: "AccessFunctions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessFunctions_Type_Module_DisplayOrder",
                table: "AccessFunctions",
                columns: new[] { "Type", "Module", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAccessFunctions_AccessFunctionId",
                table: "RoleAccessFunctions",
                column: "AccessFunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAccessFunctions_RoleId_AccessFunctionId",
                table: "RoleAccessFunctions",
                columns: new[] { "RoleId", "AccessFunctionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleAccessFunctions");

            migrationBuilder.DropTable(
                name: "AccessFunctions");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Code",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Roles");

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Controller = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsAllowed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Full system access - can manage all resources including users and roles", "Administrator" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Standard user access - can view and edit assigned resources", "User" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Manager access - can manage team resources and approve workflows", "Manager" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Read-only access - can only view resources without modification", "Viewer" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_Controller_Action",
                table: "RolePermissions",
                columns: new[] { "RoleId", "Controller", "Action" },
                unique: true);
        }
    }
}
