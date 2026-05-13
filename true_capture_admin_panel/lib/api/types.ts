// Shared response types — keep wire-compatible with the backend's DTOs.

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  accessExpiresAtUtc: string;
};

export type ResultError = { errors: string[] };

export type AdminUserListItem = {
  id: number;
  email: string;
  username: string;
  displayName: string | null;
  avatarUrl: string | null;
  emailVerified: boolean;
  isActive: boolean;
  isAdmin: boolean;
  isVerified: boolean;
  hasGoogle: boolean;
  lastLoginAtUtc: string | null;
  createdAtUtc: string;
};

export type AdminUserListResult = {
  items: AdminUserListItem[];
  nextCursor: string | null;
  total: number;
};

export type AdminUserListQuery = {
  search?: string;
  isActive?: boolean;
  isAdmin?: boolean;
  isVerified?: boolean;
  hasGoogle?: boolean;
  cursor?: string;
  limit?: number;
};
