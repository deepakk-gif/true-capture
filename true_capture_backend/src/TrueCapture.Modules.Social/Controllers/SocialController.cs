using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Social.Services;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Social.Controllers;

/// <summary>User search, profile viewing, and the follow graph.</summary>
[Authorize]
public sealed class SocialController(ISocialService svc) : BaseController
{
    /// <summary>`GET /api/users/search?q=&limit=` — search users by username / display name.</summary>
    [HttpGet("api/users/search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q, [FromQuery] int limit = 20, CancellationToken ct = default)
        => Ok(await svc.SearchAsync(CurrentUserId, q, limit, ct));

    /// <summary>`GET /api/users/{id}` — another user's profile (counts + follow-state + privacy).</summary>
    [HttpGet("api/users/{id:long}")]
    public async Task<IActionResult> Profile(long id, CancellationToken ct = default)
        => Ok(await svc.GetProfileAsync(CurrentUserId, id, ct));

    /// <summary>`POST /api/users/{id}/follow` — follow (public) or send a follow request (private).</summary>
    [HttpPost("api/users/{id:long}/follow")]
    public async Task<IActionResult> Follow(long id, CancellationToken ct = default)
        => Ok(await svc.FollowAsync(CurrentUserId, id, ct));

    /// <summary>`DELETE /api/users/{id}/follow` — unfollow or cancel a pending request.</summary>
    [HttpDelete("api/users/{id:long}/follow")]
    public async Task<IActionResult> Unfollow(long id, CancellationToken ct = default)
        => Ok(await svc.UnfollowAsync(CurrentUserId, id, ct));

    /// <summary>`GET /api/users/{id}/followers` — 403 when the account is private to the viewer.</summary>
    [HttpGet("api/users/{id:long}/followers")]
    public async Task<IActionResult> Followers(long id, [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await svc.GetFollowersAsync(CurrentUserId, id, cursor, ct));

    /// <summary>`GET /api/users/{id}/following` — 403 when the account is private to the viewer.</summary>
    [HttpGet("api/users/{id:long}/following")]
    public async Task<IActionResult> Following(long id, [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await svc.GetFollowingAsync(CurrentUserId, id, cursor, ct));

    /// <summary>`GET /api/users/{id}/posts` — 403 when the account is private to the viewer.</summary>
    [HttpGet("api/users/{id:long}/posts")]
    public async Task<IActionResult> Posts(long id, [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await svc.GetUserPostsAsync(CurrentUserId, id, cursor, ct));

    /// <summary>`GET /api/follow/requests` — incoming pending follow requests.</summary>
    [HttpGet("api/follow/requests")]
    public async Task<IActionResult> Requests([FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await svc.GetFollowRequestsAsync(CurrentUserId, cursor, ct));

    /// <summary>`POST /api/follow/requests/{requesterId}/accept`.</summary>
    [HttpPost("api/follow/requests/{requesterId:long}/accept")]
    public async Task<IActionResult> AcceptRequest(long requesterId, CancellationToken ct = default)
        => Ok(await svc.AcceptRequestAsync(CurrentUserId, requesterId, ct));

    /// <summary>`POST /api/follow/requests/{requesterId}/reject`.</summary>
    [HttpPost("api/follow/requests/{requesterId:long}/reject")]
    public async Task<IActionResult> RejectRequest(long requesterId, CancellationToken ct = default)
        => Ok(await svc.RejectRequestAsync(CurrentUserId, requesterId, ct));
}
