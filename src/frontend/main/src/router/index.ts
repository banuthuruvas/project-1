import { createRouter, createWebHashHistory } from "vue-router";
import type { RouteRecordRaw } from "vue-router";
import Cookie from "js-cookie";
import {
  ACCESS_CONTROL_PERMISSIONS,
  AUDIT_PERMISSIONS,
  UiPermission,
  resolvePermissions,
} from "@/constants/permissions";
import { getAuthLoginUrl } from "@/services/authService";

const optionalPages = import.meta.glob("../**/*.vue");

function optionalChildRoute(
  path: string,
  name: string,
  pagePath: string,
  title: string,
): RouteRecordRaw[] {
  const component = optionalPages[pagePath];
  return component
    ? [
        {
          path,
          name,
          component,
          meta: { title },
        },
      ]
    : [];
}

const routes: RouteRecordRaw[] = [
  {
    path: "/",
    component: () => import("@/staff/layouts/StaffLayout.vue"),
    children: [
      {
        path: "",
        name: "dashboard",
        component: () => import("@/staff/pages/staff/ProcurementDashboard.vue"),
        meta: {
          title: "Dashboard",
        },
      },
      {
        path: "vendors",
        name: "vendors",
        component: () => import("@/staff/pages/staff/VendorManagement.vue"),
        meta: {
          title: "Vendors",
        },
      },
      {
        path: "catalog",
        name: "catalog",
        component: () => import("@/staff/pages/staff/CatalogItems.vue"),
        meta: { title: "Catalog Items" },
      },
      {
        path: "new-purchase-request",
        name: "new-purchase-request",
        component: () => import("@/staff/pages/staff/NewPurchaseRequest.vue"),
        meta: { title: "New Purchase Request" },
      },
      {
        path: "approvals",
        name: "approvals",
        component: () => import("@/staff/pages/staff/ApprovalQueue.vue"),
        meta: { title: "Approvals" },
      },
      {
        path: "orders",
        name: "order-history",
        component: () => import("@/staff/pages/staff/OrderHistory.vue"),
        meta: { title: "Order History" },
      },
      {
        path: "purchase-order/:id",
        name: "purchase-order-detail",
        component: () => import("@/staff/pages/staff/PurchaseOrderDetail.vue"),
        meta: { title: "Purchase Order" },
      },
      {
        path: "users",
        name: "users",
        component: () => import("@/staff/pages/admin/Users.vue"),
        meta: {
          permissions: [...ACCESS_CONTROL_PERMISSIONS],
          title: "Users & Roles",
        },
      },
      {
        path: "role-management",
        name: "role-management",
        component: () => import("@/staff/pages/admin/Users.vue"),
        meta: {
          permissions: [...ACCESS_CONTROL_PERMISSIONS],
          title: "Users & Roles",
        },
      },
      {
        path: "access-functions",
        name: "access-functions",
        component: () => import("@/staff/pages/admin/AccessFunctionsPage.vue"),
        meta: {
          permissions: [...ACCESS_CONTROL_PERMISSIONS],
          title: "Access Functions",
        },
      },
      {
        path: "audit-log",
        name: "audit-log",
        component: () => import("@/staff/pages/admin/AuditLog.vue"),
        meta: {
          permissions: [...AUDIT_PERMISSIONS],
          title: "Audit Logs",
        },
      },
      {
        path: "global-settings",
        name: "global-settings",
        component: () => import("@/staff/pages/admin/GlobalSettingsPage.vue"),
        meta: {
          permission: UiPermission.SettingsManage,
          title: "Global Settings",
        },
      },
      {
        path: "push-notifications",
        redirect: { name: "global-settings" },
      },
      {
        path: "monitoring",
        name: "monitoring",
        component: () => import("@/staff/pages/admin/MonitoringPage.vue"),
        meta: {
          permission: UiPermission.SettingsManage,
          title: "Monitoring",
        },
      },
      ...optionalChildRoute("myinfo", "myinfo", "../staff/pages/staff/MyInfoPage.vue", "MyInfo"),
      ...optionalChildRoute("reports", "reports", "../pages/reports/ReportsIndex.vue", "Reports"),
      ...optionalChildRoute("reports/:type", "report-detail", "../pages/reports/ReportDetail.vue", "Report"),
      ...optionalChildRoute("chat", "chat", "../pages/chat/ChatView.vue", "AI Chat"),
      ...optionalChildRoute("chat/:source", "chat-source", "../pages/chat/ChatView.vue", "AI Chat"),
    ],
  },
  { path: "/:pathMatch(.*)*", redirect: "/" },
];

const router = createRouter({
  history: createWebHashHistory(),
  routes,
});

function getUserPermissions(): string[] {
  const userJson = Cookie.get(import.meta.env.VITE_COOKIE_USER_KEY);
  if (!userJson) return [];
  try {
    const user = JSON.parse(userJson) as {
      permissions?: string[];
      roles?: string[];
    };
    return resolvePermissions(user);
  } catch {
    return [];
  }
}

router.beforeEach((to) => {
  const hasSession = !!Cookie.get(import.meta.env.VITE_COOKIE_SESSION_KEY);

  if (!hasSession) {
    window.location.href = getAuthLoginUrl();
    return;
  }

  const requiredPermissions = to.meta?.permissions as string[] | undefined;
  if (requiredPermissions && requiredPermissions.length > 0) {
    const perms = getUserPermissions();
    if (!requiredPermissions.some((permission) => perms.includes(permission))) {
      return { name: "dashboard" };
    }
  }

  const requiredPermission = to.meta?.permission as string | undefined;
  if (requiredPermission) {
    const perms = getUserPermissions();
    if (!perms.includes(requiredPermission)) {
      return { name: "dashboard" };
    }
  }

  const pageTitle = to.meta?.title as string | undefined;
  document.title = pageTitle ? `${pageTitle} | NIE Template` : "NIE Template";
});

export default router;

