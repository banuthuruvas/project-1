import axios from "axios";
import Cookie from "js-cookie";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 30000,
});

api.interceptors.request.use((config) => {
  const sessionKey = import.meta.env.VITE_COOKIE_SESSION_KEY;
  const sessionId = Cookie.get(sessionKey);
  if (sessionId) {
    config.headers["X-Session-Id"] = sessionId;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      Cookie.remove(import.meta.env.VITE_COOKIE_SESSION_KEY);
      Cookie.remove(import.meta.env.VITE_COOKIE_USER_KEY);
      window.location.href = import.meta.env.VITE_AUTH_SERVICE_URL || "/";
    }
    return Promise.reject(error);
  },
);

export default api;

