<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import Cookie from "js-cookie";
import { useTheme } from "@nietemplate/ui";

const username = ref("");
const password = ref("");
const showPassword = ref(false);
const pageLoaded = ref(false);
const isLoading = ref(false);
const isSsoLoading = ref(false);
const errorMessage = ref("");
const ssoStatusMessage = ref("");
const { brandLabel } = useTheme();

const cookieSettings = { domain: import.meta.env.VITE_COOKIE_DOMAIN };
const isPortalSsoEnabled = import.meta.env.VITE_PORTAL_SSO_ENABLED === "true";
const isBusy = computed(() => isLoading.value || isSsoLoading.value);
const heroDescription = computed(
  () => `Secure access for ${brandLabel.value} teams and workflows.`,
);
const heroHighlights = [
  { label: "Secure access", icon: "verified_user" },
  { label: "Workflow ready", icon: "hub" },
  { label: "Operations hub", icon: "insights" },
];

const clearAuthCookies = () => {
  Cookie.remove(import.meta.env.VITE_COOKIE_SESSION_KEY, cookieSettings);
  Cookie.remove(import.meta.env.VITE_COOKIE_USER_KEY, cookieSettings);
};

const completeLogin = (data: {
  sessionToken?: string;
  userId?: string;
  userName?: string;
  fullName?: string;
  email?: string;
  department?: string;
  role?: unknown;
  roles?: Array<{ RoleName?: string | null } | string>;
  permissions?: string[];
}) => {
  const roleNames =
    data.roles
      ?.map((role) =>
        typeof role === "string" ? role : (role.RoleName ?? null),
      )
      .filter(Boolean) ?? [];

  if (data.sessionToken) {
    Cookie.set(
      import.meta.env.VITE_COOKIE_SESSION_KEY,
      data.sessionToken,
      cookieSettings,
    );
    Cookie.set(
      import.meta.env.VITE_COOKIE_USER_KEY,
      JSON.stringify({
        userId: data.userId,
        fullName: data.fullName || data.userName || "",
        email: data.email || "",
        department: data.department || "",
        role: data.role,
        roles: roleNames,
        roleNames,
        permissions: data.permissions?.filter(Boolean) || [],
      }),
      cookieSettings,
    );
  }

  window.location.href = import.meta.env.VITE_DASHBOARD_URL;
};

const getSsoReturnUrl = () => {
  const url = new URL(window.location.href);
  url.searchParams.delete("state");
  url.searchParams.set("sso", "1");
  return url.toString();
};

const sleep = (ms: number) =>
  new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });

const startSsoFlow = async () => {
  if (!isPortalSsoEnabled || isBusy.value) {
    return;
  }

  isSsoLoading.value = true;
  errorMessage.value = "";
  ssoStatusMessage.value = "Connecting to portal...";

  try {
    const params = new URLSearchParams({
      returnUrl: getSsoReturnUrl(),
    });

    const response = await fetch(
      `${import.meta.env.VITE_AUTH_API_URL}/api/Auth/SsoStart?${params.toString()}`,
      {
        method: "GET",
        credentials: "include",
      },
    );

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || "Unable to start portal sign-in.");
    }

    const data = (await response.json()) as {
      launchUrl: string;
    };

    if (!data.launchUrl) {
      throw new Error("Portal sign-in did not provide a launch URL.");
    }

    window.location.href = data.launchUrl;
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : "Portal sign-in failed.";
    ssoStatusMessage.value = "";
    isSsoLoading.value = false;
  }
};

const finalizeSsoFlow = async (state: string) => {
  if (!state) {
    return;
  }

  isSsoLoading.value = true;
  errorMessage.value = "";
  ssoStatusMessage.value = "Completing portal sign-in...";

  try {
    for (let attempt = 0; attempt < 40; attempt += 1) {
      const response = await fetch(
        `${import.meta.env.VITE_AUTH_API_URL}/api/Auth/SsoFinalize?state=${encodeURIComponent(state)}`,
        {
          method: "GET",
          credentials: "include",
        },
      );

      if (response.status === 202) {
        const pending = (await response.json().catch(() => null)) as
          | {
              pollIntervalMs?: number;
            }
          | null;
        await sleep(pending?.pollIntervalMs ?? 1500);
        continue;
      }

      if (response.ok) {
        const data = await response.json();
        completeLogin(data);
        return;
      }

      const errorData = await response.json().catch(() => null);
      throw new Error(
        errorData?.message || "Portal sign-in could not be completed.",
      );
    }

    throw new Error("Portal sign-in timed out. Please try again.");
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : "Portal sign-in failed.";
  } finally {
    ssoStatusMessage.value = "";
    isSsoLoading.value = false;
  }
};

const handleLogin = async () => {
  if (isBusy.value) {
    return;
  }

  isLoading.value = true;
  errorMessage.value = "";

  try {
    const response = await fetch(
      `${import.meta.env.VITE_AUTH_API_URL}/api/Auth/Login`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userid: username.value,
          pd: password.value,
        }),
        credentials: "include",
      },
    );

    if (response.ok) {
      const data = await response.json();
      completeLogin(data);
      return;
    }

    const errorData = await response.json();
    errorMessage.value =
      errorData.message || "Login failed. Please check your credentials.";
  } catch {
    errorMessage.value = "An error occurred during login. Please try again.";
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  window.setTimeout(() => {
    pageLoaded.value = true;
  }, 200);

  clearAuthCookies();

  const params = new URLSearchParams(window.location.search);
  const state = params.get("state");
  const shouldStartSso = params.get("sso");

  if (isPortalSsoEnabled && state) {
    void finalizeSsoFlow(state);
    return;
  }

  if (isPortalSsoEnabled && (shouldStartSso === "1" || shouldStartSso === "true")) {
    void startSsoFlow();
  }
});
</script>

<template>
  <div
    class="login-page min-h-screen flex flex-col lg:flex-row"
    :class="{ 'is-mounted': pageLoaded }"
  >
    <!-- ── Login Box (mobile: full screen, desktop: right side) ── -->
    <div
      class="flex flex-1 items-center justify-center p-4 sm:p-6 md:p-12 order-1 lg:order-2 min-h-screen lg:min-h-0"
      style="
        background: linear-gradient(
          160deg,
          #f8fafc 0%,
          #eef2ff 50%,
          #e0e7ff 100%
        );
      "
    >
      <div class="fade-in-up w-full max-w-md">
        <div class="login-card">
          <!-- Logo inside the box -->
          <div class="mb-6 flex justify-center">
            <img
              src="/nie-logo.svg"
              alt="NIE Logo"
              class="h-20 sm:h-24 md:h-28 drop-shadow-lg"
              data-testid="tic-login-logo"
            />
          </div>

          <h1
            class="mb-2 text-2xl sm:text-3xl font-extrabold tracking-tight text-slate-800 text-center"
          >
            Sign In
          </h1>
          <p class="mb-8 text-slate-500 text-center text-sm sm:text-base">
            Enter your credentials to continue.
          </p>

          <transition name="fade">
            <div
              v-if="errorMessage"
              class="shake mb-6 flex items-center gap-2 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700"
            >
              <span class="material-symbols-outlined text-[18px]">error</span>
              {{ errorMessage }}
            </div>
          </transition>

          <transition name="fade">
            <div
              v-if="ssoStatusMessage"
              class="mb-6 flex items-center gap-2 rounded-xl border border-indigo-200 bg-indigo-50 px-4 py-3 text-sm font-medium text-indigo-700"
            >
              <span class="material-symbols-outlined text-[18px]">sync</span>
              {{ ssoStatusMessage }}
            </div>
          </transition>

          <form class="flex flex-col gap-5" @submit.prevent="handleLogin">
            <div>
              <label
                for="username"
                class="mb-2 block text-sm font-semibold text-slate-700"
              >
                Username
              </label>
              <div class="input-shell">
                <span
                  class="material-symbols-outlined text-[18px] text-slate-400"
                  >person</span
                >
                <input
                  id="username"
                  v-model="username"
                  type="text"
                  class="auth-input"
                  placeholder="Enter your username"
                  autocomplete="username"
                  required
                />
              </div>
            </div>

            <div>
              <label
                for="password"
                class="mb-2 block text-sm font-semibold text-slate-700"
              >
                Password
              </label>
              <div class="input-shell">
                <span
                  class="material-symbols-outlined text-[18px] text-slate-400"
                  >lock</span
                >
                <input
                  id="password"
                  v-model="password"
                  :type="showPassword ? 'text' : 'password'"
                  class="auth-input"
                  placeholder="Enter your password"
                  autocomplete="current-password"
                  required
                />
                <button
                  type="button"
                  class="flex items-center justify-center text-slate-400 hover:text-slate-600 transition-colors"
                  tabindex="-1"
                  @click="showPassword = !showPassword"
                >
                  <span class="material-symbols-outlined text-[18px]">{{
                    showPassword ? "visibility_off" : "visibility"
                  }}</span>
                </button>
              </div>
            </div>

            <button
              type="submit"
              class="login-button flex h-[52px] items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-indigo-600 to-indigo-500 text-base font-bold text-white shadow-lg shadow-indigo-500/25"
              :disabled="isBusy"
            >
              <span v-if="!isLoading">Login</span>
              <span v-else class="loading-spinner"></span>
              <span
                v-if="!isLoading"
                class="material-symbols-outlined text-[20px]"
                >arrow_forward</span
              >
            </button>

            <div
              v-if="isPortalSsoEnabled"
              class="mt-1 flex items-center gap-3 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400"
            >
              <span class="h-px flex-1 bg-slate-200"></span>
              <span>Portal</span>
              <span class="h-px flex-1 bg-slate-200"></span>
            </div>

            <button
              v-if="isPortalSsoEnabled"
              type="button"
              class="portal-button flex h-[52px] items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white text-base font-bold text-slate-700 shadow-sm"
              :disabled="isBusy"
              @click="startSsoFlow"
            >
              <span v-if="!isSsoLoading">Continue with Portal</span>
              <span v-else class="loading-spinner border-slate-400 border-t-slate-700"></span>
              <span
                v-if="!isSsoLoading"
                class="material-symbols-outlined text-[20px]"
                >open_in_new</span
              >
            </button>
          </form>
        </div>
      </div>
    </div>

    <!-- ── Branding Panel (hidden on mobile, left side on desktop) ── -->
    <div
      class="relative hidden overflow-hidden lg:flex lg:w-[50%] lg:flex-col lg:justify-between order-2 lg:order-1"
      style="
        background: linear-gradient(
          135deg,
          #0f172a 0%,
          #1e1b4b 40%,
          #312e81 70%,
          #1e293b 100%
        );
      "
    >
      <div class="absolute inset-0 perspective-[1200px]">
        <div class="orb orb-1"></div>
        <div class="orb orb-2"></div>
        <div class="orb orb-3"></div>
        <div class="grid-floor"></div>
        <div class="center-pulse"></div>
      </div>

      <div class="relative z-10 flex h-full flex-col justify-between p-12">
        <div class="fade-in-down">
          <span
            class="text-sm font-semibold tracking-widest text-indigo-400 uppercase"
            >{{ brandLabel }}</span
          >
        </div>

        <div class="fade-in-up max-w-lg" style="animation-delay: 0.2s">
          <h2
            class="mb-5 text-4xl xl:text-5xl font-extrabold leading-tight tracking-tight text-white"
          >
            <span class="gradient-text">{{ brandLabel }}</span>
          </h2>
          <div class="space-y-4 text-lg leading-relaxed text-slate-300/90">
            <p>{{ heroDescription }}</p>
          </div>

          <div class="mt-10 grid grid-cols-1 gap-4 xl:grid-cols-3">
            <div
              v-for="(highlight, index) in heroHighlights"
              :key="highlight.label"
              class="feature-card"
              :style="{ animationDelay: `${index * 0.1}s` }"
            >
              <span
                class="material-symbols-outlined mb-2 block text-[28px] text-indigo-400"
                >{{ highlight.icon }}</span
              >
              <h3 class="mb-1 text-sm font-bold text-white">
                {{ highlight.label }}
              </h3>
            </div>
          </div>
        </div>

        <div
          class="fade-in-up flex items-center gap-8 text-sm"
          style="animation-delay: 0.45s"
        >
          <div class="stat-item">
            <p class="text-2xl font-extrabold text-white">Secure</p>
            <p class="text-slate-400">Session-based auth</p>
          </div>
          <div class="h-8 w-px bg-white/10"></div>
          <div class="stat-item">
            <p class="text-2xl font-extrabold text-white">Fast</p>
            <p class="text-slate-400">Role-based access</p>
          </div>
          <div class="h-8 w-px bg-white/10"></div>
          <div class="stat-item">
            <p class="text-2xl font-extrabold text-white">Ready</p>
            <p class="text-slate-400">Production-grade</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.fade-in-down {
  animation: fadeInDown 0.8s ease-out both;
}

.fade-in-up {
  animation: fadeInUp 0.8s ease-out both;
}

@keyframes fadeInDown {
  from {
    opacity: 0;
    transform: translateY(-30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.logo-3d {
  transition: transform 0.4s ease;
  filter: brightness(1.1);
}

.gradient-text {
  background: linear-gradient(135deg, #818cf8, #60a5fa, #a78bfa);
  background-size: 200% 200%;
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  animation: gradientShift 4s ease infinite;
}

@keyframes gradientShift {
  0%,
  100% {
    background-position: 0% 50%;
  }
  50% {
    background-position: 100% 50%;
  }
}

.orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
}

.orb-1 {
  top: -10%;
  right: -5%;
  width: 400px;
  height: 400px;
  background: radial-gradient(
    circle,
    rgba(99, 102, 241, 0.25),
    transparent 70%
  );
  animation: float3d1 8s ease-in-out infinite;
}

.orb-2 {
  bottom: 10%;
  left: -5%;
  width: 300px;
  height: 300px;
  background: radial-gradient(circle, rgba(139, 92, 246, 0.2), transparent 70%);
  animation: float3d2 10s ease-in-out infinite;
}

.orb-3 {
  top: 40%;
  right: 20%;
  width: 200px;
  height: 200px;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.2), transparent 70%);
  animation: float3d3 7s ease-in-out infinite;
}

.center-pulse {
  position: absolute;
  top: 50%;
  left: 50%;
  width: 160px;
  height: 160px;
  transform: translate(-50%, -50%);
  border-radius: 999px;
  background: radial-gradient(
    circle,
    rgba(129, 140, 248, 0.18),
    transparent 68%
  );
  animation: pulseHalo 4s ease-in-out infinite;
}

@keyframes pulseHalo {
  0%,
  100% {
    transform: translate(-50%, -50%) scale(0.92);
    opacity: 0.6;
  }
  50% {
    transform: translate(-50%, -50%) scale(1.08);
    opacity: 1;
  }
}

@keyframes float3d1 {
  0%,
  100% {
    transform: translate3d(0, 0, 0) scale(1);
  }
  50% {
    transform: translate3d(-30px, 40px, 50px) scale(1.1);
  }
}

@keyframes float3d2 {
  0%,
  100% {
    transform: translate3d(0, 0, 0) scale(1);
  }
  50% {
    transform: translate3d(40px, -30px, -30px) scale(0.9);
  }
}

@keyframes float3d3 {
  0%,
  100% {
    transform: translate3d(0, 0, 0);
  }
  33% {
    transform: translate3d(20px, -20px, 40px);
  }
  66% {
    transform: translate3d(-20px, 30px, -20px);
  }
}

.grid-floor {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 40%;
  background:
    linear-gradient(to top, rgba(99, 102, 241, 0.06), transparent),
    repeating-linear-gradient(
      90deg,
      rgba(99, 102, 241, 0.04) 0px,
      transparent 1px,
      transparent 60px
    ),
    repeating-linear-gradient(
      0deg,
      rgba(99, 102, 241, 0.04) 0px,
      transparent 1px,
      transparent 60px
    );
  transform: perspective(500px) rotateX(45deg);
  transform-origin: bottom center;
  mask-image: linear-gradient(to top, rgba(0, 0, 0, 0.8), transparent);
  -webkit-mask-image: linear-gradient(to top, rgba(0, 0, 0, 0.8), transparent);
}

.feature-card {
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.04);
  padding: 16px;
  backdrop-filter: blur(12px);
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  animation: fadeInUp 0.8s ease-out both;
}

.feature-card:hover {
  border-color: rgba(129, 140, 248, 0.3);
  background: rgba(255, 255, 255, 0.08);
  transform: perspective(800px) rotateY(-3deg) translateY(-4px) translateZ(10px);
  box-shadow: 0 20px 40px -15px rgba(99, 102, 241, 0.15);
}

.stat-item {
  transition: transform 0.3s ease;
}

.stat-item:hover {
  transform: translateY(-2px);
}

.login-card {
  border-radius: 24px;
  background: white;
  padding: 40px;
  box-shadow:
    0 4px 6px -1px rgba(0, 0, 0, 0.05),
    0 20px 50px -12px rgba(0, 0, 0, 0.08);
  transition:
    transform 0.3s ease,
    box-shadow 0.3s ease;
}

.login-card:hover {
  transform: perspective(1000px) rotateX(1deg) rotateY(-1deg) translateY(-2px);
  box-shadow:
    0 4px 6px -1px rgba(0, 0, 0, 0.05),
    0 25px 60px -12px rgba(0, 0, 0, 0.12);
}

.input-shell {
  display: flex;
  align-items: center;
  gap: 10px;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  background: #f8fafc;
  padding: 0 14px;
  transition:
    border-color 0.25s ease,
    box-shadow 0.25s ease,
    transform 0.25s ease;
}

.input-shell:focus-within {
  border-color: #6366f1;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.12);
  transform: translateY(-1px);
}

.auth-input {
  width: 100%;
  border: 0;
  background: transparent;
  padding: 15px 0;
  font-size: 0.95rem;
  color: #0f172a;
  outline: none;
}

.auth-input::placeholder {
  color: #94a3b8;
}

.login-button {
  position: relative;
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.login-button::before {
  content: "";
  position: absolute;
  inset: 0;
  background: linear-gradient(
    135deg,
    transparent,
    rgba(255, 255, 255, 0.1),
    transparent
  );
  transform: translateX(-100%);
  transition: transform 0.6s ease;
}

.login-button:hover:not(:disabled) {
  transform: translateY(-2px) scale(1.02);
  box-shadow: 0 12px 30px -8px rgba(99, 102, 241, 0.4);
}

.login-button:hover:not(:disabled)::before {
  transform: translateX(100%);
}

.login-button:disabled {
  cursor: wait;
  opacity: 0.8;
}

.loading-spinner {
  display: inline-block;
  width: 20px;
  height: 20px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: #fff;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.shake {
  animation: shake 0.5s ease-in-out;
}

@keyframes shake {
  0%,
  100% {
    transform: translateX(0);
  }
  20% {
    transform: translateX(-8px);
  }
  40% {
    transform: translateX(8px);
  }
  60% {
    transform: translateX(-4px);
  }
  80% {
    transform: translateX(4px);
  }
}

.login-page {
  opacity: 0;
  transition: opacity 0.5s ease;
}

.login-page.is-mounted {
  opacity: 1;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

@media (max-width: 1024px) {
  .login-card {
    padding: 32px;
  }
}

@media (max-width: 640px) {
  .login-card {
    border-radius: 20px;
    padding: 24px;
  }
}
</style>
