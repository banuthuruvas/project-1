import { createApp } from "vue";
import * as Sentry from "@sentry/vue";
import { initTheme } from "@nietemplate/ui";
import { i18n } from "@nietemplate/shared";
import "./style.css";
import App from "./App.vue";
import router from "./router";
import { mainThemeConfig } from "./theme/appTheme";
import { initOneSignal } from "./services/oneSignalService";

initTheme(mainThemeConfig);

const app = createApp(App);
app.use(router);
app.use(i18n);

// Initialize Sentry (no-op if DSN is empty)
const sentryDsn = import.meta.env.VITE_SENTRY_DSN;
if (sentryDsn) {
  Sentry.init({
    app,
    dsn: sentryDsn,
    environment: import.meta.env.VITE_SENTRY_ENVIRONMENT || "development",
    integrations: [Sentry.browserTracingIntegration({ router })],
    tracesSampleRate: 0.2,
    sendDefaultPii: false,
  });
}

// Initialize OneSignal push notifications (no-op if APP_ID is empty)
initOneSignal();

// Register service worker for offline support
if ("serviceWorker" in navigator) {
  navigator.serviceWorker.register(`${import.meta.env.BASE_URL}sw.js`).catch(() => {
    /* SW registration failed – app still works online */
  });
}

app.mount("#app");
