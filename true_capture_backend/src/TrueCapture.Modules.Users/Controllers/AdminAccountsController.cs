using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Users.Models;
using TrueCapture.Modules.Users.Services;
using TrueCapture.Shared.Authorization;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Users.Controllers;

/// <summary>
/// Super-admin-only account management. The mobile sign-up flow remains the
/// only path for regular users; admin accounts are minted exclusively here.
/// </summary>
[AdminOnly]
public sealed class AdminAccountsController(IAdminAccountsService svc) : BaseController
{
    /// <summary>Lists every permission for the super-admin permission-picker UI.</summary>
    [HttpGet("api/admin/permissions")]
    [RequirePermission("Users.CreateAdmin")]
    public async Task<IActionResult> ListPermissions(CancellationToken ct = default)
        => Ok(await svc.ListPermissionsAsync(ct));

    /// <summary>Creates a new admin with the specified per-user permission codes.</summary>
    [HttpPost("api/admin/users")]
    [RequirePermission("Users.CreateAdmin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest req, CancellationToken ct = default)
        => Ok(await svc.CreateAdminAsync(req, ct));
}
