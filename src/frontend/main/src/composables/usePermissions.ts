import { computed } from "vue";
import { useAuth } from "./useAuth";
import {
  ACCESS_CONTROL_PERMISSIONS,
  AUDIT_PERMISSIONS,
  AccessFunctionCode,
  UiPermission,
  resolvePermissions,
} from "@/constants/permissions";

export interface NavItem {
  name: string;
  icon: string;
  route: string;
  activeRoutes?: string[];
  permission?: string;
  permissions?: string[];
}

const PRIMARY_NAV_ITEMS: NavItem[] = [
  { name: "Dashboard", icon: "dashboard", route: "dashboard" },
  { name: "Vendors", icon: "storefront", route: "vendors" },
  { name: "Catalog", icon: "inventory_2", route: "catalog" },
  {
    name: "New Purchase Request",
    icon: "add_shopping_cart",
    route: "new-purchase-request",
  },
  { name: "Approvals", icon: "approval", route: "approvals" },
  {
    name: "Order History",
    icon: "history",
    route: "order-history",
    activeRoutes: ["order-history", "purchase-order-detail"],
  },
  { name: "MyInfo", icon: "fingerprint", route: "myinfo" },
];

const ADMIN_NAV_ITEMS: NavItem[] = [
  {
    name: "Users & Roles",
    icon: "manage_accounts",
    route: "users",
    activeRoutes: ["users", "role-management"],
    permissions: [...ACCESS_CONTROL_PERMISSIONS],
  },
  {
    name: "Access Functions",
    icon: "key",
    route: "access-functions",
    permissions: [...ACCESS_CONTROL_PERMISSIONS],
  },
  {
    name: "Audit Logs",
    icon: "history",
    route: "audit-log",
    permissions: [...AUDIT_PERMISSIONS],
  },
  {
    name: "Global Settings",
    icon: "tune",
    route: "global-settings",
    permission: UiPermission.SettingsManage,
  },
  {
    name: "Monitoring",
    icon: "monitoring",
    route: "monitoring",
    permission: UiPermission.SettingsManage,
  },
];

export function usePermissions() {
  const { currentUser, hasRole } = useAuth();

  const userPermissions = computed<string[]>(() => {
    return resolvePermissions(currentUser.value);
  });

  function hasPermission(permission: string): boolean {
    return userPermissions.value.includes(permission);
  }

  function filterNavItems(items: NavItem[]): NavItem[] {
    return items.filter((item) => {
      if (item.permissions && item.permissions.length > 0) {
        return item.permissions.some((permission) => hasPermission(permission));
      }

      if (!item.permission) return true;
      return hasPermission(item.permission);
    });
  }

  const navItems = computed<NavItem[]>(() => {
    return filterNavItems(PRIMARY_NAV_ITEMS);
  });

  const adminNavItems = computed<NavItem[]>(() =>
    filterNavItems(ADMIN_NAV_ITEMS),
  );

  const userRoleLabel = computed(() => {
    const roleNames = currentUser.value?.roleNames?.filter(Boolean) ?? [];
    if (roleNames.length > 1) {
      return `${roleNames[0]} +${roleNames.length - 1}`;
    }
    if (roleNames.length === 1) {
      return roleNames[0];
    }

    const labels: Record<string, string> = {
      SystemAdmin: "System Administrator",
      ProcurementManager: "Procurement Manager",
      FinanceApprover: "Finance Approver",
      Manager: "Manager",
      Requester: "Requester",
      Admin: "Administrator",
    };
    const role = currentUser.value?.roles?.[0];

    if (
      ACCESS_CONTROL_PERMISSIONS.some((permission) =>
        userPermissions.value.includes(permission),
      )
    ) {
      return "Administrator";
    }

    if (userPermissions.value.includes(AccessFunctionCode.Screen.AuditView)) {
      return "Auditor";
    }

    return role ? labels[role] || role : "Staff";
  });

  return {
    userPermissions,
    hasPermission,
    hasRole,
    navItems,
    adminNavItems,
    userRoleLabel,
  };
}

