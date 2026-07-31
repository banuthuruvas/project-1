// Staff User
export interface StaffUser {
  id: number;
  username: string;
  email?: string | null;
  fullName?: string | null;
  department?: string | null;
  accountStatus: string;
  isApproved: boolean;
  lastLoginAt?: string | null;
}

// Register Staff Request
export interface RegisterStaffRequest {
  username: string;
  email?: string | null;
  fullName?: string | null;
  department?: string | null;
}

// Staff Details from NTU Staff Store API
export interface StaffDetails {
  workerId: string;
  name: string;
  department: string;
  departmentDescription: string;
  userId: string;
  email: string;
  designation: string;
  joiningDate?: string | null;
  title: string;
}

// Access Function (RBAC)
export interface AccessFunction {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  module: string;
  type?: number | string;
  resourceName?: string;
  route?: string | null;
  httpMethod?: string | null;
  isActive: boolean;
  isSystemFunction?: boolean;
  displayOrder: number;
}

// Role (RBAC)
export interface Role {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  displayOrder: number;
  accessFunctions?: AccessFunction[];
}

export interface SaveRoleRequest {
  id?: number;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  displayOrder: number;
  accessFunctionIds: number[];
}

// User Role Assignment (RBAC)
export interface UserRoleAssignment {
  id: number;
  staffUserId: number;
  roleId: number;
  department?: string | null;
  isActive: boolean;
  role?: Role;
  staffUser?: StaffUser;
}

export interface SaveAccessFunctionRequest {
  id?: number;
  code: string;
  name: string;
  description?: string | null;
  module: string;
  isActive: boolean;
  displayOrder: number;
}

// Account Status constants (matches backend AccountStatus class)
export const AccountStatus = {
  Unverified: "Unverified",
  Verified: "Verified",
  Locked: "Locked",
  Suspended: "Suspended",
  Active: "Active",
  Inactive: "Inactive",
  PendingApproval: "PendingApproval",
} as const;

// Notification
export interface NotificationItem {
  id: number;
  recipientType: string;
  recipientUserId?: number | null;
  recipientEmail?: string | null;
  recipientName?: string | null;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  readAt?: string | null;
  link?: string | null;
  sourceEntityType?: string | null;
  sourceEntityId?: number | null;
  createdOn: string;
}

// Global Settings
export interface GlobalSettings {
  id: number;
  key: string;
  value: string;
  description?: string | null;
  dataType: string;
}

// Generic API response
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

// Error response
export interface ApiError {
  message: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

// Paged result
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
