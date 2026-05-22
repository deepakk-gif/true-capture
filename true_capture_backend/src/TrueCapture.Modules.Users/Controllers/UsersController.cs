using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCapture.Modules.Users.Models;
using TrueCapture.Modules.Users.Services;
using TrueCapture.Shared.Constants;
using TrueCapture.Shared.Controllers;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Users.Controllers;

/// <summary>Self-service profile endpoints for the signed-in user.</summary>
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserProfileService profiles) : BaseController
{
    /// <summary>`GET /api/users/me` — the current user's profile.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct = default)
        => Ok(await profiles.GetAsync(CurrentUserId, ct));

    /// <summary>`PUT /api/users/me` — update the current user's display name + bio.</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest req, CancellationToken ct = default)
        => Ok(await profiles.UpdateAsync(CurrentUserId, req, ct));

    /// <summary>`POST /api/users/me/avatar` — upload a new avatar (multipart, field name `file`).</summary>
    [HttpPost("me/avatar")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return Ok(Result<UserProfileResponse>.Validation(["No file was uploaded."]));

        await using var stream = file.OpenReadStream();
        return Ok(await profiles.SetAvatarAsync(
            CurrentUserId,
            new AvatarUpload(stream, file.FileName, file.ContentType, file.Length), ct));
    }

    /// <summary>`DELETE /api/users/me/avatar` — clear the current user's avatar.</summary>
    [HttpDelete("me/avatar")]
    public async Task<IActionResult> RemoveAvatar(CancellationToken ct = default)
        => Ok(await profiles.RemoveAvatarAsync(CurrentUserId, ct));
}
