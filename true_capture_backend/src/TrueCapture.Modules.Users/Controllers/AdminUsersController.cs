using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCapture.Modules.Users.Models;
using TrueCapture.Modules.Users.Services;
using TrueCapture.Shared.Authorization;
using TrueCapture.Shared.Constants;
using TrueCapture.Shared.Controllers;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Users.Controllers;

[Route("api/admin/users")]
[AdminOnly]
public sealed class AdminUsersController(
    IAdminUsersService  svc,
    IUserProfileService profiles) : BaseController
{
    /// <summary>
    /// Cursor-paginated admin user list with substring search and boolean filters.
    /// `GET /api/admin/users?search=&isActive=&isAdmin=&isVerified=&hasGoogle=&cursor=&limit=20`
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminUserListQuery query, CancellationToken ct = default)
        => Ok(await svc.ListAsync(query, ct));

    /// <summary>`GET /api/admin/users/{id}` — full user record for the detail page.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct = default)
        => Ok(await svc.GetDetailAsync(id, ct));

    /// <summary>`PUT /api/admin/users/{id}` — edit a user's display name + bio.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> Update(
        long id, [FromBody] AdminUpdateUserRequest req, CancellationToken ct = default)
        => Ok(await svc.UpdateAsync(id, req, ct));

    /// <summary>`POST /api/admin/users/{id}/status` — activate or suspend a user.</summary>
    [HttpPost("{id:long}/status")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> SetStatus(
        long id, [FromBody] SetUserStatusRequest req, CancellationToken ct = default)
    {
        // Guard against an admin locking themselves out mid-session.
        if (id == CurrentUserId)
            return Ok(Result<AdminUserDetail>.Validation(["You cannot change your own account status."]));

        return Ok(await svc.SetStatusAsync(id, req.IsActive, ct));
    }

    /// <summary>`POST /api/admin/users/{id}/avatar` — upload an avatar for a user (multipart, field `file`).</summary>
    [HttpPost("{id:long}/avatar")]
    [RequirePermission("Users.Manage")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<IActionResult> UploadAvatar(
        long id, [FromForm] IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return Ok(Result<UserProfileResponse>.Validation(["No file was uploaded."]));

        await using var stream = file.OpenReadStream();
        return Ok(await profiles.SetAvatarAsync(
            id, new AvatarUpload(stream, file.FileName, file.ContentType, file.Length), ct));
    }

    /// <summary>`DELETE /api/admin/users/{id}/avatar` — clear a user's avatar.</summary>
    [HttpDelete("{id:long}/avatar")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> RemoveAvatar(long id, CancellationToken ct = default)
        => Ok(await profiles.RemoveAvatarAsync(id, ct));
}
