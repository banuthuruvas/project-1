import api from "./api";
import type {
  AccessFunction,
  Role,
  SaveAccessFunctionRequest,
  SaveRoleRequest,
  UserRoleAssignment,
} from "@/types";

interface AccessControlOverview {
  users: unknown[];
  roles: Role[];
  accessFunctions: AccessFunction[];
}

const roleService = {
  async getAllAccessFunctions(): Promise<AccessFunction[]> {
    const overview = (
      await api.get<AccessControlOverview>("/api/AccessControl/GetOverview")
    ).data;
    return overview.accessFunctions;
  },

  async getAccessFunctionsByModule(module: string): Promise<AccessFunction[]> {
    const all = await this.getAllAccessFunctions();
    return all.filter((af) => af.module === module);
  },

  async getAllRoles(): Promise<Role[]> {
    const overview = (
      await api.get<AccessControlOverview>("/api/AccessControl/GetOverview")
    ).data;
    return overview.roles;
  },

  async getAllRolesWithAccessFunctions(): Promise<Role[]> {
    return this.getAllRoles();
  },

  async getRoleById(id: number): Promise<Role> {
    const roles = await this.getAllRoles();
    const role = roles.find((r) => r.id === id);
    if (!role) throw new Error(`Role ${id} not found`);
    return role;
  },

  async saveRole(request: SaveRoleRequest): Promise<Role> {
    if (request.id) {
      return (
        await api.post<Role>("/api/AccessControl/UpdateRole", {
          id: request.id,
          code: request.code,
          name: request.name,
          description: request.description,
          isActive: request.isActive,
          accessFunctionIds: request.accessFunctionIds,
        })
      ).data;
    }
    return (
      await api.post<Role>("/api/AccessControl/CreateRole", {
        code: request.code,
        name: request.name,
        description: request.description,
        isActive: request.isActive,
        accessFunctionIds: request.accessFunctionIds,
      })
    ).data;
  },

  async saveAccessFunction(
    request: SaveAccessFunctionRequest,
  ): Promise<AccessFunction> {
    return (
      await api.post<AccessFunction>(
        "/api/AccessControl/SaveAccessFunction",
        request,
      )
    ).data;
  },

  async getAllAssignments(): Promise<UserRoleAssignment[]> {
    const overview = (
      await api.get<{
        users: {
          userId: string;
          assignments: {
            id: number;
            userId: string;
            roleId: number;
            roleCode: string;
            roleName: string;
            isActive: boolean;
          }[];
        }[];
      }>("/api/AccessControl/GetOverview")
    ).data;

    return overview.users.flatMap((user, userIndex) =>
      user.assignments.map((a) => ({
        id: a.id,
        staffUserId: userIndex + 1,
        roleId: a.roleId,
        department: null,
        isActive: a.isActive,
        role: undefined,
        staffUser: undefined,
      })),
    );
  },

  async getAssignmentsByStaffId(
    staffId: number,
  ): Promise<UserRoleAssignment[]> {
    const all = await this.getAllAssignments();
    return all.filter((a) => a.staffUserId === staffId);
  },

  async getStaffAccessFunctions(staffId: number): Promise<string[]> {
    return (
      await api.get<string[]>(`/api/AccessControl/GetCurrentAccessProfile`)
    ).data;
  },

  async saveAssignment(
    assignment: Partial<UserRoleAssignment>,
  ): Promise<UserRoleAssignment> {
    const result = (
      await api.post("/api/AccessControl/AssignRole", {
        userId: String(assignment.staffUserId),
        roleId: assignment.roleId,
      })
    ).data;
    return result as UserRoleAssignment;
  },

  async deleteAssignment(
    id: number,
  ): Promise<{ userDeactivated: boolean; staffUserId?: number }> {
    await api.delete(`/api/AccessControl/RemoveAssignment/${id}`);
    return { userDeactivated: false };
  },
};

export default roleService;

