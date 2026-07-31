<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import RoleManagementPanel from "@/components/admin/RoleManagementPanel.vue";
import { useToast } from "@/composables/useToast";
import { NieDataTable, NieSelect } from "@nietemplate/ui";
import roleService from "@/services/roleService";
import staffUserService from "@/services/staffUserService";
import { buildFilterOptions } from "@/utils/listFilterOptions";
import type {
  AccessFunction,
  Role,
  StaffDetails,
  StaffUser,
  UserRoleAssignment,
} from "@/types";

const toast = useToast();
const loading = ref(true);
const saving = ref(false);
const route = useRoute();
const router = useRouter();

const users = ref<StaffUser[]>([]);
const roles = ref<Role[]>([]);
const assignments = ref<UserRoleAssignment[]>([]);
const accessFunctions = ref<AccessFunction[]>([]);

const showAssignModal = ref(false);
const selectedUser = ref<StaffUser | null>(null);
const selectedRoleId = ref<number | null>(null);

const showAddUserModal = ref(false);
const newEmail = ref("");
const lookupResult = ref<StaffDetails | null>(null);
const lookupError = ref("");
const lookupLoading = ref(false);

const userSearch = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);

const activeUsers = computed(() =>
  users.value.filter((user) => user.accountStatus !== "Inactive"),
);

const userRows = computed(() =>
  activeUsers.value.map((user) => ({
    ...user,
    statusLabel: user.isApproved ? "Active" : "Pending Approval",
    assignedRoleNames: getUserAssignments(user.id).map((assignment) =>
      getRoleName(assignment.roleId),
    ),
  })),
);

const userColumns = [
  { key: "fullName", label: "User" },
  { key: "assignedRoleNames", label: "Assigned Roles" },
  { key: "statusLabel", label: "Status" },
  { key: "department", label: "Department" },
];

const userFilterGroups = computed(() => [
  {
    key: "statusLabel",
    label: "Status",
    options: buildFilterOptions(userRows.value, (user) => user.statusLabel),
  },
  {
    key: "department",
    label: "Department",
    options: buildFilterOptions(userRows.value, (user) => user.department),
  },
  {
    key: "assignedRoleNames",
    label: "Roles",
    options: buildFilterOptions(
      userRows.value,
      (user) => user.assignedRoleNames,
    ),
  },
]);

const availableRoles = computed(() => {
  if (!selectedUser.value) {
    return roles.value.filter((role) => role.isActive);
  }

  const assignedRoleIds = getUserAssignments(selectedUser.value.id).map(
    (assignment) => assignment.roleId,
  );

  return roles.value.filter(
    (role) => role.isActive && !assignedRoleIds.includes(role.id),
  );
});

const activeTab = computed<"users" | "roles">(() =>
  route.name === "role-management" ? "roles" : "users",
);

onMounted(async () => {
  await loadPageData();
});

function getApiErrorMessage(error: unknown, fallback: string): string {
  const axiosError = error as {
    response?: { data?: { message?: string; error?: string } };
  };

  return (
    axiosError.response?.data?.message ||
    axiosError.response?.data?.error ||
    fallback
  );
}

async function loadUsers(): Promise<void> {
  users.value = await staffUserService.getAll();
}

async function loadRbac(): Promise<void> {
  const [allRoles, allAssignments, allAccessFunctions] = await Promise.all([
    roleService.getAllRolesWithAccessFunctions(),
    roleService.getAllAssignments(),
    roleService.getAllAccessFunctions(),
  ]);

  roles.value = allRoles;
  assignments.value = allAssignments;
  accessFunctions.value = allAccessFunctions;
}

async function loadPageData(): Promise<void> {
  loading.value = true;

  try {
    await Promise.all([loadUsers(), loadRbac()]);
  } catch {
    toast.error("Failed to load users and roles");
  } finally {
    loading.value = false;
  }
}

async function handleRoleRefresh(): Promise<void> {
  try {
    await Promise.all([loadUsers(), loadRbac()]);
  } catch {
    toast.error("Failed to refresh users and roles");
  }
}

function getUserAssignments(userId: number): UserRoleAssignment[] {
  return assignments.value.filter(
    (assignment) => assignment.staffUserId === userId && assignment.isActive,
  );
}

function getRoleName(roleId: number): string {
  return roles.value.find((role) => role.id === roleId)?.name ?? "Unknown";
}

function getRoleCodeById(roleId: number): string {
  return roles.value.find((role) => role.id === roleId)?.code ?? "";
}

function openAssignModal(user: StaffUser): void {
  selectedUser.value = user;
  selectedRoleId.value = null;
  showAssignModal.value = true;
}

async function saveAssignment(): Promise<void> {
  if (!selectedUser.value || !selectedRoleId.value) return;

  saving.value = true;

  try {
    await roleService.saveAssignment({
      staffUserId: selectedUser.value.id,
      roleId: selectedRoleId.value,
      department: selectedUser.value.department || null,
      isActive: true,
    });

    await loadRbac();
    toast.success("Role assigned successfully");
    showAssignModal.value = false;
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to assign role"));
  } finally {
    saving.value = false;
  }
}

async function removeAssignment(id: number): Promise<void> {
  try {
    const result = await roleService.deleteAssignment(id);

    await Promise.all([loadUsers(), loadRbac()]);

    if (result.userDeactivated) {
      toast.success(
        "Role removed. User deactivated because no active roles remain.",
      );
      return;
    }

    toast.success("Role assignment removed");
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to remove role assignment"));
  }
}

function openAddUserModal(): void {
  newEmail.value = "";
  lookupResult.value = null;
  lookupError.value = "";
  lookupLoading.value = false;
  showAddUserModal.value = true;
}

async function lookupStaff(): Promise<void> {
  if (!newEmail.value.trim()) return;

  lookupLoading.value = true;
  lookupError.value = "";
  lookupResult.value = null;

  try {
    lookupResult.value = await staffUserService.lookupByEmail(
      newEmail.value.trim(),
    );
  } catch (error: unknown) {
    lookupError.value = getApiErrorMessage(error, "Staff member not found.");
  } finally {
    lookupLoading.value = false;
  }
}

async function addUser(): Promise<void> {
  if (!lookupResult.value) return;

  saving.value = true;

  try {
    await staffUserService.register({
      username: lookupResult.value.userId,
      email: lookupResult.value.email,
      fullName: lookupResult.value.name,
      department: lookupResult.value.departmentDescription,
    });

    await loadUsers();
    toast.success("User added successfully");
    showAddUserModal.value = false;
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to add user"));
  } finally {
    saving.value = false;
  }
}

async function approveUser(user: StaffUser): Promise<void> {
  try {
    await staffUserService.approve(user.id);
    await loadUsers();
    toast.success(`${user.fullName || user.username} approved successfully`);
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to approve user"));
  }
}

async function deactivateUser(user: StaffUser): Promise<void> {
  try {
    await staffUserService.deactivate(user.id);
    await loadUsers();
    toast.success(`${user.fullName || user.username} deactivated`);
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to deactivate user"));
  }
}

function getInitials(name: string | null | undefined): string {
  if (!name) return "?";

  return name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);
}

const roleColorMap: Record<string, string> = {
  SystemAdmin: "bg-purple-100 text-purple-700",
  ProgrammeAdmin: "bg-blue-100 text-blue-700",
  Approver: "bg-emerald-100 text-emerald-700",
  Assessor: "bg-amber-100 text-amber-700",
  AdmissionOfficer: "bg-cyan-100 text-cyan-700",
};

function getRoleColor(roleCode: string): string {
  return roleColorMap[roleCode] || "bg-slate-100 text-slate-600";
}

function userSearchAccessor(user: StaffUser & { assignedRoleNames: string[] }) {
  return [
    user.username,
    user.fullName,
    user.email,
    user.department,
    user.statusLabel,
    ...user.assignedRoleNames,
  ];
}

function openTab(tab: "users" | "roles") {
  const targetRoute = tab === "roles" ? "role-management" : "users";

  if (route.name !== targetRoute) {
    void router.push({ name: targetRoute });
  }
}
</script>

<template>
  <div class="flex flex-col gap-8">
    <!-- Tabs -->
    <div class="portal-tabbar" role="tablist" aria-label="Users and roles tabs">
      <button
        role="tab"
        :aria-selected="activeTab === 'users'"
        class="portal-tab flex items-center gap-2"
        :class="
          activeTab === 'users'
            ? 'bg-accent text-white shadow-soft'
            : 'text-slate-500 hover:bg-accent-light hover:text-accent'
        "
        @click="openTab('users')"
      >
        <span class="material-symbols-outlined text-[18px]">group</span>
        Users
        <span
          class="ml-1 px-2 py-0.5 rounded-full text-[11px] font-bold"
          :class="
            activeTab === 'users'
              ? 'bg-white/15 text-white'
              : 'bg-slate-200 text-slate-500'
          "
          >{{ users.length }}</span
        >
      </button>
      <button
        role="tab"
        :aria-selected="activeTab === 'roles'"
        class="portal-tab flex items-center gap-2"
        :class="
          activeTab === 'roles'
            ? 'bg-accent text-white shadow-soft'
            : 'text-slate-500 hover:bg-accent-light hover:text-accent'
        "
        @click="openTab('roles')"
      >
        <span class="material-symbols-outlined text-[18px]">shield</span>
        Roles
        <span
          class="ml-1 px-2 py-0.5 rounded-full text-[11px] font-bold"
          :class="
            activeTab === 'roles'
              ? 'bg-white/15 text-white'
              : 'bg-slate-200 text-slate-500'
          "
          >{{ roles.length }}</span
        >
      </button>
    </div>

    <div v-if="loading" class="flex justify-center py-16">
      <div
        class="size-10 border-4 border-accent/30 border-t-accent rounded-full animate-spin"
      ></div>
    </div>

    <template v-else>
      <!-- =================== USERS TAB =================== -->
      <template v-if="activeTab === 'users'">
        <NieDataTable
          v-model:search="userSearch"
          v-model:selected-filters="selectedFilters"
          :columns="userColumns"
          :data="userRows"
          row-key="id"
          :filter-groups="userFilterGroups"
          search-placeholder="Search all users"
          create-label="Add User"
          hide-edit
          hide-delete
          :search-accessor="userSearchAccessor"
          @create="openAddUserModal"
        >
          <template #cell-fullName="{ row }">
            <div class="flex items-center gap-3">
              <div
                class="flex size-10 items-center justify-center rounded-full bg-accent/10 text-sm font-bold text-accent"
              >
                {{ getInitials(row.fullName) }}
              </div>
              <div>
                <p class="text-sm font-bold text-slate-800">
                  {{ row.fullName || row.username }}
                </p>
                <p class="text-xs text-slate-500">
                  {{ row.username }}
                </p>
              </div>
            </div>
          </template>

          <template #cell-assignedRoleNames="{ row }">
            <div class="flex flex-wrap gap-1.5">
              <span
                v-for="assignment in getUserAssignments(row.id)"
                :key="assignment.id"
                class="group inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-bold"
                :class="getRoleColor(getRoleCodeById(assignment.roleId))"
              >
                {{ getRoleName(assignment.roleId) }}
                <button
                  class="opacity-0 transition-opacity group-hover:opacity-100 hover:text-red-500"
                  title="Remove"
                  @click.stop="removeAssignment(assignment.id)"
                >
                  <span class="material-symbols-outlined text-[14px]"
                    >close</span
                  >
                </button>
              </span>
              <span
                v-if="getUserAssignments(row.id).length === 0"
                class="text-xs italic text-slate-400"
              >
                No roles
              </span>
            </div>
          </template>

          <template #cell-statusLabel="{ row }">
            <div class="flex items-center gap-2">
              <span
                v-if="row.isApproved"
                class="flex items-center gap-2 text-sm font-medium text-emerald-600"
              >
                <span class="size-2 rounded-full bg-emerald-500"></span>
                Active
              </span>
              <span
                v-else
                class="flex items-center gap-2 text-sm font-medium text-amber-600"
              >
                <span class="size-2 rounded-full bg-amber-500"></span>
                Pending Approval
              </span>
              <button
                v-if="!row.isApproved"
                class="ml-2 inline-flex items-center gap-1 rounded-lg bg-emerald-500 px-2.5 py-1 text-xs font-bold text-white transition-colors hover:bg-emerald-600"
                @click.stop="approveUser(row)"
              >
                <span class="material-symbols-outlined text-[14px]">check</span>
                Approve
              </button>
            </div>
          </template>

          <template #cell-department="{ value }">
            {{ value || "-" }}
          </template>

          <template #extra-actions="{ row }">
            <button
              class="inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-xs font-bold text-accent transition-colors hover:bg-accent/10"
              @click.stop="openAssignModal(row)"
            >
              <span class="material-symbols-outlined text-[16px]">add</span>
              Assign Role
            </button>
            <button
              class="inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-xs font-bold text-red-500 transition-colors hover:bg-red-50"
              title="Deactivate user"
              @click.stop="deactivateUser(row)"
            >
              <span class="material-symbols-outlined text-[16px]"
                >person_off</span
              >
              Deactivate
            </button>
          </template>
        </NieDataTable>
      </template>

      <!-- =================== ROLES TAB =================== -->
      <template v-if="activeTab === 'roles'">
        <RoleManagementPanel
          :users="users"
          :roles="roles"
          :assignments="assignments"
          :access-functions="accessFunctions"
          @refresh="handleRoleRefresh"
        />
      </template>
    </template>

    <!-- =================== ASSIGN ROLE MODAL (from Users tab) =================== -->
    <Teleport to="body">
      <div
        v-if="showAssignModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/20 backdrop-blur-sm p-4"
      >
        <div
          class="bg-white rounded-3xl w-full max-w-lg shadow-2xl flex flex-col overflow-hidden"
        >
          <div
            class="flex items-center justify-between p-6 border-b border-slate-100"
          >
            <h2 class="text-xl font-bold text-slate-800">Assign Role</h2>
            <button
              class="p-2 hover:bg-slate-100 rounded-full text-slate-400"
              @click="showAssignModal = false"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>
          <div class="p-6 flex flex-col gap-5">
            <div>
              <p class="text-sm font-bold text-slate-500 mb-1">Staff Member</p>
              <p class="text-lg font-bold text-slate-800">
                {{ selectedUser?.fullName || selectedUser?.username }}
              </p>
              <p class="text-sm text-slate-500">{{ selectedUser?.username }}</p>
            </div>
            <div class="flex flex-col gap-2">
              <NieSelect
                v-model="selectedRoleId"
                label="Select Role"
                :options="
                  availableRoles.map((r) => ({ value: r.id, label: r.name }))
                "
                placeholder="Choose a role..."
              />
            </div>
          </div>
          <div
            class="p-6 border-t border-slate-100 bg-slate-50 flex items-center justify-end gap-3"
          >
            <button
              class="px-6 py-2.5 rounded-xl font-bold text-slate-500 hover:bg-slate-200 transition-colors"
              @click="showAssignModal = false"
            >
              Cancel
            </button>
            <button
              class="px-6 py-2.5 rounded-xl bg-accent text-white font-bold shadow-soft hover:bg-accent/90 transition-all disabled:opacity-50"
              :disabled="!selectedRoleId || saving"
              @click="saveAssignment"
            >
              {{ saving ? "Saving..." : "Assign Role" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- =================== ADD USER MODAL =================== -->
    <Teleport to="body">
      <div
        v-if="showAddUserModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/20 backdrop-blur-sm p-4"
      >
        <div
          class="bg-white rounded-3xl w-full max-w-lg shadow-2xl flex flex-col overflow-hidden"
        >
          <div
            class="flex items-center justify-between p-6 border-b border-slate-100"
          >
            <h2 class="text-xl font-bold text-slate-800">Add New User</h2>
            <button
              class="p-2 hover:bg-slate-100 rounded-full text-slate-400"
              @click="showAddUserModal = false"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>
          <div class="p-6 flex flex-col gap-5">
            <p class="text-sm text-slate-500">
              Search for a staff member by their NIE email address. Their
              profile will be retrieved from the staff directory.
            </p>
            <!-- Email Search -->
            <div class="flex flex-col gap-2">
              <label class="text-sm font-bold text-slate-500"
                >Email <span class="text-red-500">*</span></label
              >
              <div class="flex gap-2">
                <input
                  v-model="newEmail"
                  type="email"
                  class="flex-1 h-12 px-4 rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-accent/20 text-slate-800 font-medium"
                  placeholder="e.g. john@nie.edu.sg"
                  @keyup.enter="lookupStaff"
                />
                <button
                  class="px-5 h-12 rounded-xl bg-accent text-white font-bold shadow-soft hover:bg-accent/90 transition-all disabled:opacity-50 flex items-center gap-2"
                  :disabled="!newEmail.trim() || lookupLoading"
                  @click="lookupStaff"
                >
                  <span class="material-symbols-outlined text-[18px]"
                    >search</span
                  >
                  {{ lookupLoading ? "Searching..." : "Search" }}
                </button>
              </div>
            </div>
            <!-- Lookup Error -->
            <div
              v-if="lookupError"
              class="flex items-center gap-3 p-4 rounded-xl bg-red-50 border border-red-100"
            >
              <span
                class="material-symbols-outlined text-red-500 text-[20px] shrink-0"
                >error</span
              >
              <span class="text-sm text-red-700">{{ lookupError }}</span>
            </div>
            <!-- Lookup Result -->
            <div
              v-if="lookupResult"
              class="flex flex-col gap-3 p-4 rounded-xl bg-emerald-50 border border-emerald-100"
            >
              <div class="flex items-center gap-2 mb-1">
                <span
                  class="material-symbols-outlined text-emerald-600 text-[20px]"
                  >check_circle</span
                >
                <span class="text-sm font-bold text-emerald-700"
                  >Staff member found</span
                >
              </div>
              <div class="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <span class="text-slate-400 text-xs font-medium">Name</span>
                  <p class="text-slate-800 font-medium">
                    {{ lookupResult.name }}
                  </p>
                </div>
                <div>
                  <span class="text-slate-400 text-xs font-medium">Email</span>
                  <p class="text-slate-800 font-medium">
                    {{ lookupResult.email }}
                  </p>
                </div>
                <div>
                  <span class="text-slate-400 text-xs font-medium"
                    >Department</span
                  >
                  <p class="text-slate-800 font-medium">
                    {{ lookupResult.departmentDescription }}
                  </p>
                </div>
                <div>
                  <span class="text-slate-400 text-xs font-medium"
                    >Designation</span
                  >
                  <p class="text-slate-800 font-medium">
                    {{ lookupResult.designation }}
                  </p>
                </div>
                <div>
                  <span class="text-slate-400 text-xs font-medium"
                    >User ID</span
                  >
                  <p class="text-slate-800 font-medium">
                    {{ lookupResult.userId }}
                  </p>
                </div>
                <div>
                  <span class="text-slate-400 text-xs font-medium">Title</span>
                  <p class="text-slate-800 font-medium">
                    {{ lookupResult.title }}
                  </p>
                </div>
              </div>
            </div>
          </div>
          <div
            class="p-6 border-t border-slate-100 bg-slate-50 flex items-center justify-end gap-3"
          >
            <button
              class="px-6 py-2.5 rounded-xl font-bold text-slate-500 hover:bg-slate-200 transition-colors"
              @click="showAddUserModal = false"
            >
              Cancel
            </button>
            <button
              class="px-6 py-2.5 rounded-xl bg-accent text-white font-bold shadow-soft hover:bg-accent/90 transition-all disabled:opacity-50"
              :disabled="!lookupResult || saving"
              @click="addUser"
            >
              {{ saving ? "Adding..." : "Add User" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

