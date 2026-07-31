export interface NotificationCategoryDefinition {
  id: string;
  label: string;
  description: string;
}

export interface NotificationPreferenceDefinition {
  key: string;
  label: string;
  description: string;
  categoryId: string;
  defaultEnabled: boolean;
}

export interface UserNotificationPreferences {
  desktopAlerts: boolean;
  subscriptions: Record<string, boolean>;
}

export interface AdminNotificationSettingDefinition {
  key: string;
  label: string;
  description: string;
  categoryId: string;
  defaultValue: boolean;
}

const STORAGE_KEY_PREFIX = "nie-template-notification-preferences";

export const USER_NOTIFICATION_CATEGORIES: NotificationCategoryDefinition[] = [
  {
    id: "orders",
    label: "Orders & Approvals",
    description:
      "Updates tied to purchase requests, approvals, and fulfilment milestones.",
  },
  {
    id: "catalog",
    label: "Catalog & Vendors",
    description:
      "Changes to vendor records, catalogs, and procurement reference data.",
  },
  {
    id: "workspace",
    label: "Workspace",
    description:
      "General procurement announcements and reminders for the signed-in user.",
  },
];

export const USER_NOTIFICATION_DEFINITIONS: NotificationPreferenceDefinition[] =
  [
    {
      key: "orderUpdates",
      label: "Order updates",
      description:
        "Status changes, fulfilment milestones, and request progress for your orders.",
      categoryId: "orders",
      defaultEnabled: true,
    },
    {
      key: "approvalReminders",
      label: "Approval reminders",
      description:
        "Pending approvals, escalations, and reminders when action is needed.",
      categoryId: "orders",
      defaultEnabled: true,
    },
    {
      key: "approvalDecisions",
      label: "Approval decisions",
      description:
        "Approvals, rejections, and comments on submitted procurement requests.",
      categoryId: "orders",
      defaultEnabled: true,
    },
    {
      key: "vendorUpdates",
      label: "Vendor updates",
      description:
        "Vendor onboarding, suspension, and important profile or compliance changes.",
      categoryId: "catalog",
      defaultEnabled: false,
    },
    {
      key: "catalogRefreshes",
      label: "Catalog refreshes",
      description:
        "Catalog additions, pricing refreshes, and availability changes.",
      categoryId: "catalog",
      defaultEnabled: true,
    },
    {
      key: "workspaceAnnouncements",
      label: "Workspace announcements",
      description:
        "General procurement notices, policy reminders, and release updates.",
      categoryId: "workspace",
      defaultEnabled: true,
    },
  ];

export const ADMIN_NOTIFICATION_CATEGORIES: NotificationCategoryDefinition[] = [
  {
    id: "operations",
    label: "Operations",
    description:
      "System-level notifications for approvals, incidents, and service health.",
  },
  {
    id: "security",
    label: "Security & Access",
    description:
      "Changes that affect roles, access functions, and sensitive activity review.",
  },
  {
    id: "configuration",
    label: "Configuration",
    description:
      "Alerts for global settings and other system-wide administration changes.",
  },
];

export const ADMIN_NOTIFICATION_SETTING_DEFINITIONS: AdminNotificationSettingDefinition[] =
  [
    {
      key: "Notifications.Admin.ApprovalBacklog.Enabled",
      label: "Approval backlog alerts",
      description:
        "Notify administrators when approval queues age, spike, or breach SLA thresholds.",
      categoryId: "operations",
      defaultValue: true,
    },
    {
      key: "Notifications.Admin.Monitoring.Enabled",
      label: "Monitoring incidents",
      description:
        "Notify administrators when uptime checks degrade or monitoring issues are detected.",
      categoryId: "operations",
      defaultValue: true,
    },
    {
      key: "Notifications.Admin.AccessControl.Enabled",
      label: "Access control changes",
      description:
        "Notify administrators when roles, assignments, or access functions are changed.",
      categoryId: "security",
      defaultValue: true,
    },
    {
      key: "Notifications.Admin.Audit.Enabled",
      label: "Audit review alerts",
      description:
        "Notify administrators about unusual audit activity that needs follow-up.",
      categoryId: "security",
      defaultValue: false,
    },
    {
      key: "Notifications.Admin.Configuration.Enabled",
      label: "Configuration changes",
      description:
        "Notify administrators when global settings or system-wide notification defaults change.",
      categoryId: "configuration",
      defaultValue: true,
    },
  ];

function createDefaultSubscriptions(): Record<string, boolean> {
  return Object.fromEntries(
    USER_NOTIFICATION_DEFINITIONS.map((definition) => [
      definition.key,
      definition.defaultEnabled,
    ]),
  ) as Record<string, boolean>;
}

export function getDefaultNotificationPreferences(): UserNotificationPreferences {
  return {
    desktopAlerts: true,
    subscriptions: createDefaultSubscriptions(),
  };
}

export function normalizeNotificationPreferences(
  preferences?: Partial<UserNotificationPreferences> | null,
): UserNotificationPreferences {
  const defaults = getDefaultNotificationPreferences();
  const subscriptions = { ...defaults.subscriptions };

  Object.entries(preferences?.subscriptions ?? {}).forEach(([key, enabled]) => {
    if (key in subscriptions && typeof enabled === "boolean") {
      subscriptions[key] = enabled;
    }
  });

  return {
    desktopAlerts:
      typeof preferences?.desktopAlerts === "boolean"
        ? preferences.desktopAlerts
        : defaults.desktopAlerts,
    subscriptions,
  };
}

function getStorageKey(userId?: string | number | null): string {
  return `${STORAGE_KEY_PREFIX}:${String(userId ?? "guest")}`;
}

export function loadNotificationPreferences(
  userId?: string | number | null,
): UserNotificationPreferences {
  if (typeof window === "undefined") {
    return getDefaultNotificationPreferences();
  }

  try {
    const raw = window.localStorage.getItem(getStorageKey(userId));
    if (!raw) {
      return getDefaultNotificationPreferences();
    }

    return normalizeNotificationPreferences(
      JSON.parse(raw) as Partial<UserNotificationPreferences>,
    );
  } catch {
    return getDefaultNotificationPreferences();
  }
}

export function saveNotificationPreferences(
  userId: string | number | null | undefined,
  preferences: Partial<UserNotificationPreferences>,
): UserNotificationPreferences {
  const normalized = normalizeNotificationPreferences(preferences);

  if (typeof window === "undefined") {
    return normalized;
  }

  try {
    window.localStorage.setItem(
      getStorageKey(userId),
      JSON.stringify(normalized),
    );
  } catch {
    // Ignore local storage failures and return the normalized value.
  }

  return normalized;
}

export function setAllNotificationSubscriptions(
  preferences: UserNotificationPreferences,
  enabled: boolean,
  categoryId?: string,
): UserNotificationPreferences {
  const normalized = normalizeNotificationPreferences(preferences);
  const subscriptions = { ...normalized.subscriptions };
  const definitions = categoryId
    ? USER_NOTIFICATION_DEFINITIONS.filter(
        (definition) => definition.categoryId === categoryId,
      )
    : USER_NOTIFICATION_DEFINITIONS;

  definitions.forEach((definition) => {
    subscriptions[definition.key] = enabled;
  });

  return {
    ...normalized,
    subscriptions,
  };
}

export function getSubscribedNotificationDefinitions(
  preferences: UserNotificationPreferences,
): NotificationPreferenceDefinition[] {
  const normalized = normalizeNotificationPreferences(preferences);

  return USER_NOTIFICATION_DEFINITIONS.filter(
    (definition) => normalized.subscriptions[definition.key],
  );
}

export function getNotificationPreferenceSummary(
  preferences: UserNotificationPreferences,
): string {
  const count = getSubscribedNotificationDefinitions(preferences).length;
  return count === 1 ? "1 subscribed" : `${count} subscribed`;
}

export function isNotificationPreferencesEnabled(
  preferences: UserNotificationPreferences,
  permission: NotificationPermission | "unsupported",
): boolean {
  const normalized = normalizeNotificationPreferences(preferences);

  return normalized.desktopAlerts && permission === "granted";
}
