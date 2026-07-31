import { ref, computed, readonly } from "vue";
import Cookies from "js-cookie";

interface User {
  id: string;
  email: string;
  name: string;
  roles: string[];
}

const user = ref<User | null>(null);
const isAuthenticated = computed(() => !!user.value);
const isLoading = ref(false);

export function useAuth() {
  const SESSION_COOKIE_NAME = "NieTemplate-SessionToken";

  function getSessionToken(): string | undefined {
    return Cookies.get(SESSION_COOKIE_NAME);
  }

  function setSessionToken(token: string): void {
    Cookies.set(SESSION_COOKIE_NAME, token, { expires: 1 }); // 1 day
  }

  function clearSession(): void {
    Cookies.remove(SESSION_COOKIE_NAME);
    user.value = null;
  }

  function setUser(userData: User): void {
    user.value = userData;
  }

  function hasRole(role: string): boolean {
    return user.value?.roles.includes(role) ?? false;
  }

  function hasAnyRole(roles: string[]): boolean {
    return roles.some((role) => hasRole(role));
  }

  async function logout(): Promise<void> {
    clearSession();
    // Redirect to auth app
    window.location.href = "/auth/";
  }

  return {
    user: readonly(user),
    isAuthenticated,
    isLoading: readonly(isLoading),
    getSessionToken,
    setSessionToken,
    clearSession,
    setUser,
    hasRole,
    hasAnyRole,
    logout,
  };
}
