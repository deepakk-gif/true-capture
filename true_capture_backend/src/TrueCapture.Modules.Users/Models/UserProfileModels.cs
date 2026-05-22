namespace TrueCapture.Modules.Users.Models;

/// <summary>
/// Full self-profile shape returned by <c>GET/PUT /api/users/me</c> and the avatar
/// endpoints (and by the admin avatar endpoints, which reuse the same profile service).
/// </summary>
public sealed record UserProfileResponse(
    long      Id,
    string    Email,
    string    Username,
    string?   DisplayName,
    string?   AvatarUrl,
    string?   Bio,
    DateTime  JoinedAtUtc,
    int       FollowersCount,
    int       FollowingCount,
    int       PostsCount,
    bool      IsSuspended,
    bool      IsBlueTick,
    string    AccountType,   // "public" | "private"
    string?   Gender,        // "male" | "female" | "other" | null
    bool      EmailVerified,
    bool      IsAdmin);

/// <summary>
/// Body of <c>PUT /api/users/me</c>. All fields are applied as a full replace.
/// <c>Gender</c>: null/blank clears it; <c>AccountType</c>: null/blank leaves it unchanged.
/// </summary>
public sealed record UpdateProfileRequest(
    string? DisplayName,
    string? Bio,
    string? Gender,
    string? AccountType);
