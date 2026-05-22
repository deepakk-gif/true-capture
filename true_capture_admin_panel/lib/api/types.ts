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

// Full user record for the detail page — wire-compatible with the backend
// `AdminUserDetail` DTO (`GET /api/admin/users/{id}`).
export type AdminUserDetail = {
  id: number;
  email: string;
  username: string;
  displayName: string | null;
  avatarUrl: string | null;
  bio: string | null;
  emailVerified: boolean;
  isActive: boolean;
  isAdmin: boolean;
  isVerified: boolean;
  hasGoogle: boolean;
  lastLoginAtUtc: string | null;
  createdAtUtc: string;
  accountType: string; // "public" | "private"
  followersCount: number;
  followingCount: number;
  postsCount: number;
};

/// A post in the admin moderation grid — wire-compatible with the backend `PostItem`.
export type AdminPost = {
  id: number;
  authorId: number;
  type: string; // "normal" | "fakeVsReal"
  kind: string; // "photo" | "carousel" | "video"
  coverUrl: string;
  caption: string | null;
  createdAtUtc: string;
};

export type AdminPostList = {
  items: AdminPost[];
  nextCursor: string | null;
};

/// A row in the post-reports moderation queue (`GET /api/admin/post-reports`).
export type PostReport = {
  id: number;
  postId: number;
  postCoverUrl: string;
  reporterId: number;
  reporterUsername: string;
  reason: string;
  otherText: string | null;
  status: string; // "open" | "resolved"
  resolution: string | null;
  createdAtUtc: string;
};

export type PostReportList = {
  items: PostReport[];
  nextCursor: string | null;
};

/// A Fake-vs-Real upload-access candidate (`GET /api/admin/fvr-candidates`).
export type FvrCandidate = {
  id: number;
  username: string;
  displayName: string | null;
  avatarUrl: string | null;
  followersCount: number;
  creatorScore: number;
  canPostFakeVsReal: boolean;
  joinedAtUtc: string;
};

export type FvrCandidateList = {
  items: FvrCandidate[];
  nextCursor: string | null;
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
