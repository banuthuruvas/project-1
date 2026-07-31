import { ref, computed } from "vue";
import Cookie from "js-cookie";
import { removeOneSignalExternalUserId } from "@/services/oneSignalService";
import { getAuthLoginUrl } from "@/services/authService";

interface UserInfo {
  userId: number | string;
  fullName: string;
  email: string;
  department?: string;
  lastLoginAt?: string | null;
  roles: string[];
  roleNames?: string[];
  permissions?: string[];
}

const ADMIN_ROLES = ["SystemAdmin", "ProgrammeAdmin", "Admin"];

const currentUser = ref<UserInfo | null>(null);
const SESSION_KEY = import.meta.env.VITE_COOKIE_SESSION_KEY;
const USER_KEY = import.meta.env.VITE_COOKIE_USER_KEY;

export function useAuth() {
  const isAuthenticated = computed(() => !!Cookie.get(SESSION_KEY));

  const isAdmin = computed(
    () =>
      currentUser.value?.roles?.some((r) => ADMIN_ROLES.includes(r)) ?? false,
  );

  function loadUser() {
    const userJson = Cookie.get(USER_KEY);
    if (userJson) {
      try {
        currentUser.value = JSON.parse(userJson);
      } catch {
        currentUser.value = null;
      }
    }
  }

  function ensureAuthenticated(): boolean {
    if (!Cookie.get(SESSION_KEY) || !Cookie.get(USER_KEY)) {
      return false;
    }
    if (!currentUser.value) {
      loadUser();
    }
    return true;
  }

  function logout() {
    removeOneSignalExternalUserId();
    Cookie.remove(SESSION_KEY);
    Cookie.remove(USER_KEY);
    currentUser.value = null;
    window.location.href = getAuthLoginUrl();
  }

  function hasRole(role: string): boolean {
    return currentUser.value?.roles?.includes(role) ?? false;
  }

  // Load on first use
  if (!currentUser.value && Cookie.get(USER_KEY)) {
    loadUser();
  }

  return {
    currentUser,
    isAuthenticated,
    isAdmin,
    loadUser,
    logout,
    hasRole,
    ensureAuthenticated,
  };
}

