import Cookie from "js-cookie";

const cookieSettings = { domain: import.meta.env.VITE_COOKIE_DOMAIN };

/** Full URL of the auth frontend login page */
export function getAuthLoginUrl(): string {
  return import.meta.env.VITE_AUTH_SERVICE_URL || "/";
}

const authService = {
  /**
   * Redirect to auth service login if sessionId is missing
   */
  ensureAuthenticated(): void {
    const sessionId = Cookie.get(import.meta.env.VITE_COOKIE_SESSION_KEY);
    if (!sessionId) {
      Cookie.remove(import.meta.env.VITE_COOKIE_SESSION_KEY, cookieSettings);
      Cookie.remove(import.meta.env.VITE_COOKIE_USER_KEY, cookieSettings);
      window.location.href = getAuthLoginUrl();
    }
  },

  /**
   * Redirect to auth service login (for 403 or manual use)
   */
  redirectToLogin(): void {
    Cookie.remove(import.meta.env.VITE_COOKIE_SESSION_KEY, cookieSettings);
    Cookie.remove(import.meta.env.VITE_COOKIE_USER_KEY, cookieSettings);
    window.location.href = getAuthLoginUrl();
  },
};

export { authService };
export default authService;
