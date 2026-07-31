<script setup lang="ts">
import { onMounted, onUnmounted, ref } from "vue";
import myInfoService, {
  type MyInfoPersonData,
  type TestProfile,
} from "@/services/myInfoService";
import { useToast } from "@/composables/useToast";
import myInfoLoginBtn from "@/assets/myinfo-login-red.svg";

const toast = useToast();
const activeTab = ref<"myinfo" | "test-accounts">("myinfo");
const loading = ref(true);
const configured = ref(false);
const authenticating = ref(false);
const personData = ref<MyInfoPersonData | null>(null);

// Test profiles
const testProfiles = ref<TestProfile[]>([]);
const testProfilesLoading = ref(false);
const testSearch = ref("");

// Check if MyInfo is configured
onMounted(async () => {
  try {
    configured.value = await myInfoService.isConfigured();
  } catch {
    configured.value = false;
  } finally {
    loading.value = false;
  }
});

// Listen for SingPass callback postMessage
function handleMessage(event: MessageEvent) {
  if (event.origin !== window.location.origin) return;
  if (event.data?.type !== "nietemplate-auth-callback") return;

  if (event.data.error) {
    toast.error(`SingPass error: ${event.data.error}`);
    authenticating.value = false;
    return;
  }

  if (event.data.code && event.data.state) {
    exchangeCode(event.data.code, event.data.state);
  }
}

onMounted(() => window.addEventListener("message", handleMessage));
onUnmounted(() => window.removeEventListener("message", handleMessage));

async function loginWithSingPass() {
  if (!configured.value) {
    toast.error(
      "MyInfo/SingPass is not configured. Please contact your administrator to set up the MyInfo integration.",
    );
    return;
  }
  authenticating.value = true;
  try {
    const url = await myInfoService.getAuthorizeUrl();
    window.open(url, "singpass-login", "width=600,height=700");
  } catch (err: unknown) {
    const message =
      err instanceof Error && err.message
        ? err.message
        : "Failed to initiate SingPass login. MyInfo may not be configured on the server.";
    toast.error(message);
    authenticating.value = false;
  }
}

async function exchangeCode(code: string, state: string) {
  try {
    personData.value = await myInfoService.callback(code, state);
    toast.success("MyInfo data retrieved successfully");
  } catch {
    toast.error("Failed to retrieve MyInfo data");
  } finally {
    authenticating.value = false;
  }
}

function clearData() {
  personData.value = null;
}

// Load test profiles when tab switches to test-accounts
async function loadTestProfiles() {
  if (testProfiles.value.length > 0) return;
  testProfilesLoading.value = true;
  try {
    testProfiles.value = await myInfoService.getTestProfiles();
  } catch {
    toast.error("Failed to load test profiles");
  } finally {
    testProfilesLoading.value = false;
  }
}

function switchTab(tab: "myinfo" | "test-accounts") {
  activeTab.value = tab;
  if (tab === "test-accounts") loadTestProfiles();
}

// Filter test profiles by search
function filteredProfiles() {
  if (!testSearch.value) return testProfiles.value;
  const q = testSearch.value.toLowerCase();
  return testProfiles.value.filter(
    (p) =>
      p.uinfin.toLowerCase().includes(q) ||
      p.name.toLowerCase().includes(q) ||
      p.email.toLowerCase().includes(q),
  );
}

// Person data display cards
const infoCards = [
  {
    title: "Personal Information",
    icon: "person",
    fields: [
      { label: "Full Name", key: "name" },
      { label: "NRIC/FIN", key: "nricFin" },
      { label: "Sex", key: "sex" },
      { label: "Race", key: "race" },
      { label: "Date of Birth", key: "dateOfBirth" },
      { label: "Nationality", key: "nationality" },
      { label: "Birth Country", key: "birthCountry" },
      { label: "Residential Status", key: "residentialStatus" },
      { label: "Marital Status", key: "maritalStatus" },
    ],
  },
  {
    title: "Contact Details",
    icon: "contact_mail",
    fields: [
      { label: "Email", key: "email" },
      { label: "Mobile", key: "mobileNumber" },
    ],
  },
  {
    title: "Address",
    icon: "home",
    fields: [
      { label: "Block", key: "blockNumber" },
      { label: "Street", key: "streetName" },
      { label: "Floor", key: "floorNumber" },
      { label: "Unit", key: "unitNumber" },
      { label: "Postal Code", key: "postalCode" },
      { label: "Full Address", key: "registeredAddress" },
    ],
  },
  {
    title: "Education & Employment",
    icon: "school",
    fields: [
      { label: "Highest Qualification", key: "highestQualification" },
      { label: "Occupation", key: "occupation" },
      { label: "Employer", key: "employerName" },
      { label: "Subject", key: "subject" },
    ],
  },
];
</script>

<template>
  <div class="space-y-6">
    <!-- Tabs -->
    <div class="overflow-x-auto">
      <div class="portal-tabbar" role="tablist" aria-label="MyInfo tabs">
        <button
          role="tab"
          :aria-selected="activeTab === 'myinfo'"
          :class="[
            'portal-tab flex items-center gap-2 whitespace-nowrap',
            activeTab === 'myinfo'
              ? 'bg-accent text-white shadow-soft'
              : 'text-slate-500 hover:bg-accent-light hover:text-accent',
          ]"
          @click="switchTab('myinfo')"
        >
          <span class="material-symbols-outlined text-[18px] align-middle mr-1"
            >fingerprint</span
          >
          MyInfo Login
        </button>
        <button
          role="tab"
          :aria-selected="activeTab === 'test-accounts'"
          :class="[
            'portal-tab flex items-center gap-2 whitespace-nowrap',
            activeTab === 'test-accounts'
              ? 'bg-accent text-white shadow-soft'
              : 'text-slate-500 hover:bg-accent-light hover:text-accent',
          ]"
          @click="switchTab('test-accounts')"
        >
          <span class="material-symbols-outlined text-[18px] align-middle mr-1"
            >groups</span
          >
          Test Accounts
        </button>
      </div>
    </div>

    <!-- Tab 1: MyInfo Login -->
    <div v-if="activeTab === 'myinfo'">
      <!-- Loading state -->
      <div
        v-if="loading"
        class="flex items-center justify-center py-20 text-slate-400"
      >
        <span class="material-symbols-outlined animate-spin mr-2">sync</span>
        Checking configuration...
      </div>

      <!-- Not configured -->
      <div
        v-else-if="!configured"
        class="rounded-xl border border-amber-200 bg-amber-50 p-6 text-center"
      >
        <span class="material-symbols-outlined text-amber-500 text-4xl mb-2"
          >warning</span
        >
        <p class="text-amber-800 font-medium">MyInfo is not configured</p>
        <p class="text-amber-600 text-sm mt-1">
          Please configure MyInfo settings in appsettings.json to enable
          SingPass integration.
        </p>
      </div>

      <!-- Login prompt (no data yet) -->
      <div
        v-else-if="!personData"
        class="max-w-lg mx-auto text-center space-y-6 py-8"
      >
        <div
          class="mx-auto size-16 rounded-full bg-accent/10 text-accent flex items-center justify-center"
        >
          <span class="material-symbols-outlined text-[32px]"
            >fingerprint</span
          >
        </div>
        <div>
          <h2 class="text-xl font-bold text-slate-900">
            Retrieve MyInfo Data
          </h2>
          <p class="mt-2 text-slate-500 text-sm">
            Login with SingPass to retrieve your MyInfo personal data. This uses
            the MyInfo FAPI v5 integration with SingPass staging environment.
          </p>
        </div>
        <button
          class="inline-flex items-center gap-2 disabled:opacity-50"
          :disabled="authenticating"
          @click="loginWithSingPass"
        >
          <img
            :src="myInfoLoginBtn"
            alt="Login with MyInfo"
            class="h-12 cursor-pointer hover:opacity-90 transition-opacity"
          />
        </button>
        <p v-if="authenticating" class="text-sm text-slate-400 animate-pulse">
          Waiting for SingPass authentication...
        </p>
      </div>

      <!-- Person data display -->
      <div v-else class="space-y-6">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div
              class="size-10 rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center"
            >
              <span class="material-symbols-outlined">verified</span>
            </div>
            <div>
              <h2 class="text-lg font-bold text-slate-900">
                {{ personData.name || "MyInfo Data" }}
              </h2>
              <p class="text-sm text-slate-500">
                Verified at
                {{
                  personData.verifiedAtUtc
                    ? new Date(personData.verifiedAtUtc).toLocaleString()
                    : "just now"
                }}
              </p>
            </div>
          </div>
          <button
            class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-slate-600 bg-slate-100 rounded-lg hover:bg-slate-200 transition-colors"
            @click="clearData"
          >
            <span class="material-symbols-outlined text-[16px]">close</span>
            Clear
          </button>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div
            v-for="card in infoCards"
            :key="card.title"
            class="rounded-xl border border-slate-200 bg-white p-5"
          >
            <div class="flex items-center gap-2 mb-4">
              <span
                class="material-symbols-outlined text-accent text-[20px]"
                >{{ card.icon }}</span
              >
              <h3 class="font-semibold text-slate-900 text-sm">
                {{ card.title }}
              </h3>
            </div>
            <dl class="space-y-2.5">
              <div
                v-for="field in card.fields"
                :key="field.key"
                class="flex justify-between text-sm"
              >
                <dt class="text-slate-500">{{ field.label }}</dt>
                <dd class="text-slate-900 font-medium text-right">
                  {{
                    (personData as Record<string, unknown>)[field.key] || "—"
                  }}
                </dd>
              </div>
            </dl>
          </div>
        </div>
      </div>
    </div>

    <!-- Tab 2: Test Accounts -->
    <div v-if="activeTab === 'test-accounts'">
      <div
        v-if="testProfilesLoading"
        class="flex items-center justify-center py-20 text-slate-400"
      >
        <span class="material-symbols-outlined animate-spin mr-2">sync</span>
        Loading test profiles...
      </div>

      <template v-else>
        <!-- Search -->
        <div class="mb-4">
          <div class="relative max-w-sm">
            <span
              class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-[20px]"
              >search</span
            >
            <input
              v-model="testSearch"
              type="text"
              placeholder="Search by NRIC, name, or email..."
              class="w-full pl-10 pr-4 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-accent/20 focus:border-accent"
            />
          </div>
        </div>

        <p class="text-xs text-slate-400 mb-3">
          {{ filteredProfiles().length }} of {{ testProfiles.length }} test
          profiles (SingPass staging environment)
        </p>

        <!-- Table -->
        <div class="overflow-x-auto rounded-xl border border-slate-200">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50">
              <tr>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  NRIC/FIN
                </th>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  Name
                </th>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  Sex
                </th>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  Race
                </th>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  DOB
                </th>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  Nationality
                </th>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  Email
                </th>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  Mobile
                </th>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  Pass Type
                </th>
                <th
                  class="px-4 py-3 text-left font-medium text-slate-600 whitespace-nowrap"
                >
                  Postal Code
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 bg-white">
              <tr
                v-for="profile in filteredProfiles()"
                :key="profile.uinfin"
                class="hover:bg-slate-50"
              >
                <td class="px-4 py-2.5 font-mono text-xs text-slate-700">
                  {{ profile.uinfin }}
                </td>
                <td class="px-4 py-2.5 text-slate-900 whitespace-nowrap">
                  {{ profile.name }}
                </td>
                <td class="px-4 py-2.5 text-slate-600">{{ profile.sex }}</td>
                <td class="px-4 py-2.5 text-slate-600">{{ profile.race }}</td>
                <td class="px-4 py-2.5 text-slate-600 whitespace-nowrap">
                  {{ profile.dob }}
                </td>
                <td class="px-4 py-2.5 text-slate-600">
                  {{ profile.nationality }}
                </td>
                <td class="px-4 py-2.5 text-slate-600">
                  {{ profile.email }}
                </td>
                <td class="px-4 py-2.5 text-slate-600">
                  {{ profile.mobile }}
                </td>
                <td class="px-4 py-2.5 text-slate-600">
                  {{ profile.passType || "—" }}
                </td>
                <td class="px-4 py-2.5 text-slate-600">
                  {{ profile.postalCode }}
                </td>
              </tr>
              <tr v-if="filteredProfiles().length === 0">
                <td colspan="10" class="px-4 py-8 text-center text-slate-400">
                  No matching test profiles found.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </div>
  </div>
</template>
