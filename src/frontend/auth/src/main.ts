import { createApp } from "vue";
import * as Sentry from "@sentry/vue";
import { initTheme } from "@nietemplate/ui";
import { i18n } from "@nietemplate/shared";
import "./style.css";
import App from "./App.vue";
import { authThemeConfig } from "./theme/appTheme";

initTheme(authThemeConfig);

const app = createApp(App);
app.use(i18n);

// Initialize Sentry (no-op if DSN is empty)
const sentryDsn = import.meta.env.VITE_SENTRY_DSN;
if (sentryDsn) {
  Sentry.init({
    app,
    dsn: sentryDsn,
    environment: import.meta.env.VITE_SENTRY_ENVIRONMENT || "development",
    tracesSampleRate: 0.2,
    sendDefaultPii: false,
  });
}

app.mount("#app");

