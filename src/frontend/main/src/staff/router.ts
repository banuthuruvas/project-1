import { createRouter, createWebHashHistory } from "vue-router";
import Cookie from "js-cookie";
import { resolvePermissions } from "@/constants/permissions";
import { getAuthLoginUrl } from "@/services/authService";

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    {
      path: "/",
      component: () => import("@/layouts/StaffLayout.vue"),
      children: [
        {
          path: "",
          name: "dashboard",
          component: () => import("@/pages/staff/StaffDashboard.vue"),
          meta: { title: "Staff Dashboard" },
        },
        {
          path: "queue",
          name: "application-queue",
          component: () => import("@/pages/staff/ApplicationQueue.vue"),
          meta: {
            permission: "SearchApplications",
            title: "Application Queue",
          },
        },
        {
          path: "application/:id",
          name: "application-detail",
          component: () => import("@/pages/staff/ApplicationDetail.vue"),
          meta: { permission: "ViewApplication" },
        },
        {
          path: "interviews",
          name: "interviews",
          component: () => import("@/pages/staff/Interviews.vue"),
          meta: { title: "Interviews" },
        },
        {
          path: "document-review",
          name: "document-review",
          component: () => import("@/pages/staff/DocumentReviewQueue.vue"),
          meta: { permission: "SearchApplications", title: "Document Review" },
        },
        {
          path: "support-tickets",
          name: "support-tickets",
          component: () => import("@/pages/staff/SupportTickets.vue"),
          meta: { title: "Support Tickets" },
        },
        {
          path: "faq",
          name: "faq-management",
          component: () => import("@/pages/admin/FaqManagement.vue"),
          meta: { title: "FAQ / Knowledge Base" },
        },
        // Merged: Programmes + Categories
        {
          path: "programmes",
          name: "programmes",
          component: () => import("@/pages/admin/ProgrammesHub.vue"),
          meta: { permission: "ManageProgrammes", title: "Programmes" },
        },
        {
          path: "programmes/:id",
          name: "programme-detail",
          component: () => import("@/pages/admin/ProgrammeDetail.vue"),
          meta: { permission: "ManageProgrammes" },
        },
        {
          path: "programmes/category/:categoryCode",
          name: "programme-category-detail",
          component: () => import("@/pages/admin/ProgrammeCategoryDetail.vue"),
          meta: { permission: "ManageProgrammes" },
        },
        // Merged: Configuration + Intake Semesters
        {
          path: "config",
          name: "configuration",
          component: () => import("@/pages/admin/ConfigurationHub.vue"),
          meta: { permission: "ManageConfig", title: "Configuration" },
        },
        {
          path: "users",
          name: "users",
          component: () => import("@/pages/admin/Users.vue"),
          meta: { permission: "ManageUsers", title: "Users & Roles" },
        },
        {
          path: "audit-log",
          name: "audit-log",
          component: () => import("@/pages/admin/AuditLog.vue"),
          meta: { permission: "ViewAuditLog", title: "Audit Logs" },
        },
        {
          path: "global-settings",
          name: "global-settings",
          component: () => import("@/pages/admin/GlobalSettingsPage.vue"),
          meta: { permission: "ManageConfig", title: "Global Settings" },
        },
        {
          path: "reports",
          name: "reports",
          component: () => import("@/pages/admin/Reports.vue"),
          meta: { permission: "ViewReports", title: "Reports" },
        },
        {
          path: "verification-dashboard",
          name: "verification-dashboard",
          component: () => import("@/pages/admin/VerificationDashboard.vue"),
          meta: { permission: "ViewReports", title: "Verification Dashboard" },
        },
        {
          path: "analytics",
          name: "analytics",
          component: () => import("@/pages/admin/AnalyticsDashboard.vue"),
          meta: { permission: "ViewReports", title: "Analytics" },
        },
        {
          path: "search",
          name: "advanced-search",
          component: () => import("@/pages/admin/AdvancedSearch.vue"),
          meta: { permission: "SearchApplications", title: "Advanced Search" },
        },
        // Merged: All Enrolment pages
        {
          path: "enrolment",
          name: "enrolment",
          component: () => import("@/pages/admin/EnrolmentHub.vue"),
          meta: {
            permission: "ManageEnrolmentConfiguration",
            title: "Enrolment",
          },
        },
        // Redirects for old routes
        { path: "programme-categories", redirect: { name: "programmes" } },
        { path: "intake-management", redirect: { name: "configuration" } },
        { path: "administration", redirect: { name: "users" } },
        { path: "ereg/profiles", redirect: { name: "enrolment" } },
        { path: "ereg/programmes", redirect: { name: "enrolment" } },
        { path: "ereg/fees", redirect: { name: "enrolment" } },
        { path: "ereg/windows", redirect: { name: "enrolment" } },
        { path: "ereg/periods", redirect: { name: "enrolment" } },
        { path: "ereg/config", redirect: { name: "enrolment" } },
        { path: "ereg/audit-log", redirect: { name: "enrolment" } },
        { path: "ereg/access", redirect: { name: "users" } },
      ],
    },
  ],
});

function getUserPermissions(): string[] {
  const userKey = import.meta.env.VITE_COOKIE_USER_KEY;
  const userJson = Cookie.get(userKey);
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

router.beforeEach((to, _from, next) => {
  const sessionKey = import.meta.env.VITE_COOKIE_SESSION_KEY;
  const session = Cookie.get(sessionKey);

  if (!session) {
    window.location.href = getAuthLoginUrl();
    return;
  }

  const requiredPermission = to.meta.permission as string | undefined;
  if (requiredPermission) {
    const perms = getUserPermissions();
    if (!perms.includes(requiredPermission)) {
      next({ name: "dashboard" });
      return;
    }
  }

  next();
});

export default router;

