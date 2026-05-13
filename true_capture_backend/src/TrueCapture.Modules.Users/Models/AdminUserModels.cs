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
