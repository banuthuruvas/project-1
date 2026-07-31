<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { NieButton, NieModal, NieSwitch, useToast } from "@nietemplate/ui";
import { useAuth } from "@/composables/useAuth";
import {
  USER_NOTIFICATION_CATEGORIES,
  USER_NOTIFICATION_DEFINITIONS,
  getDefaultNotificationPreferences,
  getSubscribedNotificationDefinitions,
  isNotificationPreferencesEnabled,
  loadNotificationPreferences,
  normalizeNotificationPreferences,
  saveNotificationPreferences,
  setAllNotificationSubscriptions,
  type UserNotificationPreferences,
} from "@/services/notificationPreferencesService";

interface Props {
  modelValue: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
  saved: [preferences: UserNotificationPreferences];
}>();

const toast = useToast();
const { currentUser } = useAuth();

const localPreferences = ref<UserNotificationPreferences>(
  getDefaultNotificationPreferences(),
);
const browserPermission = ref<NotificationPermission | "unsupported">(
  "default",
);

const permissionLabel = computed(() => {
  if (browserPermission.value === "unsupported") {
    return "Unsupported";
  }

  if (browserPermission.value === "granted") {
    return "Granted";
  }

  if (browserPermission.value === "denied") {
    return "Blocked";
  }

  return "Not requested";
});

const overallStatusLabel = computed(() =>
  isNotificationPreferencesEnabled(
    localPreferences.value,
    browserPermission.value,
  )
    ? "Enabled"
    : "Disabled",
);

const subscribedDefinitions = computed(() =>
  getSubscribedNotificationDefinitions(localPreferences.value),
);

const groupedPreferences = computed(() =>
  USER_NOTIFICATION_CATEGORIES.map((category) => {
    const items = USER_NOTIFICATION_DEFINITIONS.filter(
      (definition) => definition.categoryId === category.id,
    );

    return {
      ...category,
      items,
      enabledCount: items.filter(
        (definition) => localPreferences.value.subscriptions[definition.key],
      ).length,
    };
  }).filter((category) => category.items.length > 0),
);

function syncPermission() {
  if (typeof Notification === "undefined") {
    browserPermission.value = "unsupported";
    return;
  }

  browserPermission.value = Notification.permission;
}

function loadPreferences() {
  localPreferences.value = normalizeNotificationPreferences(
    loadNotificationPreferences(currentUser.value?.userId),
  );
}

function openModalState() {
  syncPermission();
  loadPreferences();
}

watch(
  () => props.modelValue,
  (isOpen) => {
    if (isOpen) {
      openModalState();
    }
  },
  { immediate: true },
);

function closeModal() {
  emit("update:modelValue", false);
}

async function requestPermission() {
  if (typeof Notification === "undefined") {
    toast.error("Browser notifications are not supported in this environment");
    return;
  }

  const result = await Notification.requestPermission();
  browserPermission.value = result;

  if (result === "granted") {
    toast.success("Browser notifications enabled");
    return;
  }

  toast.info("Notification permission was not granted");
}

function updateDesktopAlerts(enabled: boolean) {
  localPreferences.value = {
    ...localPreferences.value,
    desktopAlerts: enabled,
  };
}

function updateSubscription(key: string, enabled: boolean) {
  localPreferences.value = {
    ...localPreferences.value,
    subscriptions: {
      ...localPreferences.value.subscriptions,
      [key]: enabled,
    },
  };
}

function selectAll(categoryId?: string) {
  localPreferences.value = setAllNotificationSubscriptions(
    localPreferences.value,
    true,
    categoryId,
  );
}

function deselectAll(categoryId?: string) {
  localPreferences.value = setAllNotificationSubscriptions(
    localPreferences.value,
    false,
    categoryId,
  );
}

function savePreferences() {
  const saved = saveNotificationPreferences(
    currentUser.value?.userId,
    localPreferences.value,
  );

  toast.success("Notification preferences saved");
  emit("saved", saved);
  emit("update:modelValue", false);
}
</script>

<template>
  <NieModal
    :model-value="modelValue"
    title="Notification Settings"
    size="xl"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div class="space-y-4">
      <section
        class="rounded-3xl border p-5"
        style="
          border-color: var(--color-border);
          background-color: var(--color-surface);
        "
      >
        <div
          class="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between"
        >
          <div class="space-y-3">
            <div class="flex flex-wrap gap-2">
              <span
                class="rounded-full px-3 py-1 text-xs font-semibold"
                style="
                  background-color: var(--color-sidebar-active);
                  color: var(--color-primary);
                "
              >
                Status: {{ overallStatusLabel }}
              </span>
              <span
                class="rounded-full px-3 py-1 text-xs font-semibold"
                style="
                  background-color: var(--color-surface-muted, #f8fafc);
                  color: var(--color-text-muted);
                "
              >
                Browser: {{ permissionLabel }}
              </span>
              <span
                class="rounded-full px-3 py-1 text-xs font-semibold"
                style="
                  background-color: var(--color-surface-muted, #f8fafc);
                  color: var(--color-text-muted);
                "
              >
                {{ subscribedDefinitions.length }} of
                {{ USER_NOTIFICATION_DEFINITIONS.length }} enabled
              </span>
            </div>

            <p class="text-sm" style="color: var(--color-text-muted)">
              Choose which alerts appear in your inbox and browser.
            </p>
          </div>

          <div
            class="flex items-center justify-between gap-4 rounded-[1rem] border px-4 py-3"
            style="
              border-color: var(--color-border);
              background-color: var(--color-surface-muted, #f8fafc);
            "
          >
            <div>
              <p class="text-sm font-semibold" style="color: var(--color-text)">
                Desktop alerts
              </p>
              <p class="mt-1 text-xs" style="color: var(--color-text-muted)">
                Show browser pop-ups for enabled items.
              </p>
            </div>

            <NieSwitch
              :model-value="localPreferences.desktopAlerts"
              @update:model-value="updateDesktopAlerts(Boolean($event))"
            />
          </div>
        </div>

        <div class="mt-4 flex flex-wrap gap-2">
          <NieButton
            v-if="browserPermission !== 'granted'"
            variant="outline"
            size="sm"
            @click="requestPermission"
          >
            Request Permission
          </NieButton>
          <NieButton variant="outline" size="sm" @click="selectAll()">
            Enable All
          </NieButton>
          <NieButton variant="outline" size="sm" @click="deselectAll()">
            Disable All
          </NieButton>
        </div>
      </section>

      <section
        v-for="category in groupedPreferences"
        :key="category.id"
        class="rounded-3xl border p-5"
        style="
          border-color: var(--color-border);
          background-color: var(--color-surface);
        "
      >
        <div
          class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between"
        >
          <div>
            <p class="text-sm font-semibold" style="color: var(--color-text)">
              {{ category.label }}
            </p>
            <p class="mt-1 text-xs" style="color: var(--color-text-muted)">
              {{ category.enabledCount }} of {{ category.items.length }} enabled
            </p>
          </div>

          <div class="flex flex-wrap items-center gap-2">
            <NieButton
              variant="outline"
              size="sm"
              @click="selectAll(category.id)"
            >
              All
            </NieButton>
            <NieButton
              variant="outline"
              size="sm"
              @click="deselectAll(category.id)"
            >
              None
            </NieButton>
          </div>
        </div>

        <div class="mt-4 space-y-2">
          <div
            v-for="definition in category.items"
            :key="definition.key"
            class="flex items-center justify-between gap-4 rounded-2xl border px-4 py-3"
            style="
              border-color: var(--color-border);
              background-color: var(--color-surface-muted, #f8fafc);
            "
          >
            <div class="min-w-0">
              <div class="flex flex-wrap items-center gap-2">
                <p class="text-sm font-medium" style="color: var(--color-text)">
                  {{ definition.label }}
                </p>
                <span
                  class="inline-flex items-center rounded-full px-2.5 py-1 text-[11px] font-semibold"
                  :style="{
                    backgroundColor: localPreferences.subscriptions[
                      definition.key
                    ]
                      ? 'var(--color-sidebar-active)'
                      : 'var(--color-surface)',
                    color: localPreferences.subscriptions[definition.key]
                      ? 'var(--color-primary)'
                      : 'var(--color-text-muted)',
                  }"
                >
                  {{
                    localPreferences.subscriptions[definition.key]
                      ? "Enabled"
                      : "Disabled"
                  }}
                </span>
              </div>
            </div>

            <NieSwitch
              :model-value="localPreferences.subscriptions[definition.key]"
              @update:model-value="
                updateSubscription(definition.key, Boolean($event))
              "
            />
          </div>
        </div>
      </section>
    </div>

    <template #footer>
      <div class="flex flex-wrap items-center justify-end gap-3">
        <NieButton variant="ghost" @click="closeModal">Cancel</NieButton>
        <NieButton @click="savePreferences">Save</NieButton>
      </div>
    </template>
  </NieModal>
</template>
