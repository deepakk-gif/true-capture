using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Users.Models;
using TrueCapture.Modules.Users.Services;
using TrueCapture.Shared.Authorization;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Users.Controllers;

[Route("api/admin/users")]
[AdminOnly]
public sealed class AdminUsersController(IAdminUsersService svc) : BaseController
{
    /// <summary>
    /// Cursor-paginated admin user list with substring search and boolean filters.
    /// `GET /api/admin/users?search=&isActive=&isAdmin=&isVerified=&hasGoogle=&cursor=&limit=20`
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminUserListQuery query, CancellationToken ct = default)
        => Ok(await svc.ListAsync(query, ct));
}
