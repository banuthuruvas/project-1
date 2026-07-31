import api from "./api";
import type { RegisterStaffRequest, StaffDetails, StaffUser } from "@/types";

interface AccessControlOverview {
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
    accessFunctionCodes: string[];
  }[];
}

const staffUserService = {
  async getAll(): Promise<StaffUser[]> {
    const overview = (
      await api.get<AccessControlOverview>("/api/AccessControl/GetOverview")
    ).data;

    return overview.users.map((user, index) => ({
      id: index + 1,
      username: user.userId,
      email: null,
      fullName: user.userId,
      department: null,
      accountStatus: user.assignments.some((a) => a.isActive)
        ? "Active"
        : "Inactive",
      isApproved: true,
      lastLoginAt: null,
    }));
  },

  async lookupByEmail(email: string): Promise<StaffDetails> {
    return (
      await api.get<StaffDetails>(
        `/api/AccessControl/LookupStaff?email=${encodeURIComponent(email)}`,
      )
    ).data;
  },

  async register(request: RegisterStaffRequest): Promise<void> {
    await api.post("/api/AccessControl/RegisterUser", request);
  },

  async approve(id: number): Promise<void> {
    await api.post(`/api/AccessControl/ApproveUser?id=${id}`);
  },

  async deactivate(id: number): Promise<void> {
    await api.post(`/api/AccessControl/DeactivateUser?id=${id}`);
  },
};

export default staffUserService;
