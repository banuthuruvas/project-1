/**
 * OneSignal Web Push Notification Service
 *
 * Initializes OneSignal for web push notifications.
 * Set VITE_ONESIGNAL_APP_ID in your .env to enable.
 * Users are linked via setExternalUserId after login.
 */

declare global {
  interface Window {
    OneSignalDeferred?: Array<(oneSignal: OneSignalInstance) => void>;
  }
}

interface OneSignalInstance {
  init(config: { appId: string; allowLocalhostAsSecureOrigin?: boolean }): void;
  login(externalId: string): Promise<void>;
  logout(): Promise<void>;
}

let initialized = false;

export function initOneSignal(): void {
  const appId = import.meta.env.VITE_ONESIGNAL_APP_ID;
  if (!appId || initialized) return;

  initialized = true;

  window.OneSignalDeferred = window.OneSignalDeferred || [];
  window.OneSignalDeferred.push((oneSignal) => {
    oneSignal.init({
      appId,
      allowLocalhostAsSecureOrigin:
        import.meta.env.VITE_SENTRY_ENVIRONMENT === "development",
    });
  });

  // Load OneSignal SDK script
  const script = document.createElement("script");
  script.src = "https://cdn.onesignal.com/sdks/web/v16/OneSignalSDK.page.js";
  script.defer = true;
  document.head.appendChild(script);
}

/**
 * Link the current authenticated user to OneSignal for targeted push notifications.
 */
export function setOneSignalExternalUserId(userId: string): void {
  if (!import.meta.env.VITE_ONESIGNAL_APP_ID) return;
  window.OneSignalDeferred = window.OneSignalDeferred || [];
  window.OneSignalDeferred.push((oneSignal) => {
    oneSignal.login(userId);
  });
}

/**
 * Unlink the user from OneSignal (call on logout).
 */
export function removeOneSignalExternalUserId(): void {
  if (!import.meta.env.VITE_ONESIGNAL_APP_ID) return;
  window.OneSignalDeferred = window.OneSignalDeferred || [];
  window.OneSignalDeferred.push((oneSignal) => {
    oneSignal.logout();
  });
}
