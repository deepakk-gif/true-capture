namespace TrueCapture.Modules.Users.Models;

/// <summary>Body of `POST /api/admin/users` — super-admin creates a new admin.</summary>
public sealed record CreateAdminRequest(
    string             Email,
    string             Username,
    string             Password,
    string?            DisplayName,
    IReadOnlyList<string> PermissionCodes);

/// <summary>Response of `POST /api/admin/users`.</summary>
public sealed record CreatedAdminResponse(
    long                 Id,
    string               Email,
    string               Username,
    string?              DisplayName,
    IReadOnlyList<string> GrantedPermissionCodes);

/// <summary>Row shape of `GET /api/admin/permissions`.</summary>
public sealed record PermissionDescriptor(
    string Code,
    string Module,
    string? Description);
