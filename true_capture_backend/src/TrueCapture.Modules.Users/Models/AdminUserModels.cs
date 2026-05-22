namespace TrueCapture.Modules.Users.Models;

/// <summary>Row shape returned by `GET /api/admin/users`.</summary>
public sealed record AdminUserListItem(
    long       Id,
    string     Email,
    string     Username,
    string?    DisplayName,
    string?    AvatarUrl,
    bool       EmailVerified,
    bool       IsActive,
    bool       IsAdmin,
    bool       IsVerified,
    bool       HasGoogle,
    DateTime?  LastLoginAtUtc,
    DateTime   CreatedAtUtc);

/// <summary>Cursor-paginated payload. Cursor is an opaque base64 string.</summary>
public sealed record AdminUserListResult(
    IReadOnlyList<AdminUserListItem> Items,
    string?                          NextCursor,
    int                              Total);

/// <summary>Full user record for the admin user-detail page (`GET /api/admin/users/{id}`).</summary>
public sealed record AdminUserDetail(
    long       Id,
    string     Email,
    string     Username,
    string?    DisplayName,
    string?    AvatarUrl,
    string?    Bio,
    bool       EmailVerified,
    bool       IsActive,
    bool       IsAdmin,
    bool       IsVerified,
    bool       HasGoogle,
    DateTime?  LastLoginAtUtc,
    DateTime   CreatedAtUtc,
    string     AccountType,      // "public" | "private"
    int        FollowersCount,
    int        FollowingCount,
    int        PostsCount);

/// <summary>Body of `PUT /api/admin/users/{id}` — admin edit of a user's profile fields.</summary>
public sealed record AdminUpdateUserRequest(
    string? DisplayName,
    string? Bio);

/// <summary>Body of `POST /api/admin/users/{id}/status` — activate/suspend a user.</summary>
public sealed record SetUserStatusRequest(bool IsActive);

/// <summary>Bind-from-query filter for the admin users list.</summary>
public sealed class AdminUserListQuery
{
    public string? Search       { get; set; }   // matches email OR username (case-insensitive substring)
    public bool?   IsActive     { get; set; }   // null = either; true/false filter
    public bool?   IsAdmin      { get; set; }
    public bool?   IsVerified   { get; set; }
    public bool?   HasGoogle    { get; set; }
    public string? Cursor       { get; set; }
    public int     Limit        { get; set; } = 20;

    public int NormalizedLimit() => Math.Clamp(Limit <= 0 ? 20 : Limit, 1, 100);
}
