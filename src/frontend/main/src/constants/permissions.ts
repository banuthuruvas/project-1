type NestedValues<T> = T extends string
  ? T
  : { [K in keyof T]: NestedValues<T[K]> }[keyof T];

export const AccessFunctionCode = {
  Screen: {
    DashboardView: "screen.dashboard.view",
    OperationsView: "screen.operations.view",
    ReportsView: "screen.reports.view",
    AuditView: "screen.audit.view",
    AccessControlView: "screen.access-control.view",
  },
  Api: {
    CodeRead: "api.code.read",
    DocumentDownload: "api.document.download",
    DocumentManage: "api.document.manage",
    AuditRead: "api.audit-log.read",
    AccessControlRead: "api.access-control.read",
    AccessControlRolesManage: "api.access-control.roles.manage",
    AccessControlAssignmentsManage: "api.access-control.assignments.manage",
    // Procurement sample feature (kept as reference; remove via task 0002 in derived repos)
    ProcurementVendorRead: "api.procurement.vendor.read",
    ProcurementVendorManage: "api.procurement.vendor.manage",
    ProcurementCatalogRead: "api.procurement.catalog.read",
    ProcurementCatalogManage: "api.procurement.catalog.manage",
    ProcurementOrderRead: "api.procurement.order.read",
    ProcurementOrderManage: "api.procurement.order.manage",
    ProcurementOrderApprove: "api.procurement.order.approve",
  },
} as const;

export type AccessFunctionCodeValue = NestedValues<typeof AccessFunctionCode>;

export const UiPermission = {
  SettingsManage: "SETTINGS_MANAGE",
  VendorManage: "VENDOR_MANAGE",
  CatalogManage: "CATALOG_MANAGE",
} as const;

export const ACCESS_CONTROL_PERMISSIONS = [
  AccessFunctionCode.Screen.AccessControlView,
  AccessFunctionCode.Api.AccessControlRead,
  AccessFunctionCode.Api.AccessControlRolesManage,
  AccessFunctionCode.Api.AccessControlAssignmentsManage,
] as const;

export const AUDIT_PERMISSIONS = [
  AccessFunctionCode.Screen.AuditView,
  AccessFunctionCode.Api.AuditRead,
] as const;

export const LEGACY_ROLE_PERMISSIONS: Record<string, string[]> = {
  Requester: ["CreatePurchaseOrder", "ViewOwnOrders", "UploadDocuments"],
  Manager: [
    "CreatePurchaseOrder",
    "ViewOwnOrders",
    "UploadDocuments",
    "ApproveOrders",
    "ViewAllOrders",
  ],
  FinanceApprover: ["ViewAllOrders", "ApproveOrders", "ViewReports"],
  ProcurementManager: [
    "CreatePurchaseOrder",
    "ViewAllOrders",
    "ApproveOrders",
    "ManageVendors",
    "ManageCatalog",
    "UploadDocuments",
    "ViewReports",
  ],
  SystemAdmin: [
    "CreatePurchaseOrder",
    "ViewAllOrders",
    "ApproveOrders",
    "ManageVendors",
    "ManageCatalog",
    "UploadDocuments",
    "ViewReports",
    "ManageUsers",
    "ViewAuditLog",
    "ManageConfig",
  ],
};

export const ACCESS_FUNCTION_PERMISSION_MAP: Record<string, string[]> = {
  [UiPermission.SettingsManage]: [UiPermission.SettingsManage],
  [UiPermission.VendorManage]: [UiPermission.VendorManage],
  [UiPermission.CatalogManage]: [UiPermission.CatalogManage],
  [AccessFunctionCode.Screen.AccessControlView]: [UiPermission.SettingsManage],
  [AccessFunctionCode.Api.AccessControlRead]: [UiPermission.SettingsManage],
  [AccessFunctionCode.Api.AccessControlRolesManage]: [
    UiPermission.SettingsManage,
  ],
  [AccessFunctionCode.Api.AccessControlAssignmentsManage]: [
    UiPermission.SettingsManage,
  ],
  [AccessFunctionCode.Screen.OperationsView]: [UiPermission.SettingsManage],
  [AccessFunctionCode.Screen.ReportsView]: [UiPermission.SettingsManage],
};

export interface PermissionUser {
  permissions?: string[];
  roles?: string[];
}

export function resolvePermissions(user?: PermissionUser | null): string[] {
  if (!user) return [];

  const directPermissions = user.permissions?.filter(Boolean) ?? [];
  if (directPermissions.length > 0) {
    const permissions = new Set<string>();

    for (const permission of directPermissions) {
      permissions.add(permission);
      ACCESS_FUNCTION_PERMISSION_MAP[permission]?.forEach((mappedPermission) =>
        permissions.add(mappedPermission),
      );
    }

    return Array.from(permissions);
  }

  const permissions = new Set<string>();
  for (const role of user.roles ?? []) {
    const rolePermissions = LEGACY_ROLE_PERMISSIONS[role];
    rolePermissions?.forEach((permission) => permissions.add(permission));
  }

  return Array.from(permissions);
}

