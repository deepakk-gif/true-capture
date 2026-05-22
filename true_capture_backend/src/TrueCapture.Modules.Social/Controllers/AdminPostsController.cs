using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Modules.Social.Services;
using TrueCapture.Shared.Authorization;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Social.Controllers;

/// <summary>Admin post tooling — Fake-vs-Real publishing, moderation, and access grants.</summary>
[AdminOnly]
public sealed class AdminPostsController(
    IPostService           posts,
    IPostModerationService moderation) : BaseController
{
    /// <summary>`POST /api/admin/posts` — admin publishes a Fake-vs-Real post.</summary>
    [HttpPost("api/admin/posts")]
    [RequirePermission("Posts.Create")]
    public async Task<IActionResult> Create(
        [FromBody] AdminCreatePostRequest req, CancellationToken ct = default)
        => Ok(await posts.AdminCreateAsync(CurrentUserId, req, ct));

    /// <summary>`GET /api/admin/users/{id}/posts` — any user's posts (moderation grid).</summary>
    [HttpGet("api/admin/users/{id:long}/posts")]
    public async Task<IActionResult> UserPosts(
        long id, [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await posts.GetByUserAsync(id, cursor, ct));

    /// <summary>`DELETE /api/admin/posts/{id}` — hard-delete any post.</summary>
    [HttpDelete("api/admin/posts/{id:long}")]
    [RequirePermission("Posts.Moderate")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
        => Ok(await posts.AdminDeleteAsync(id, ct));

    /// <summary>`GET /api/admin/post-reports?status=&cursor=` — the moderation queue.</summary>
    [HttpGet("api/admin/post-reports")]
    [RequirePermission("Posts.Moderate")]
    public async Task<IActionResult> Reports(
        [FromQuery] string? status, [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await moderation.ListReportsAsync(status, cursor, ct));

    /// <summary>`PATCH /api/admin/post-reports/{id}` — resolve a report.</summary>
    [HttpPatch("api/admin/post-reports/{id:long}")]
    [RequirePermission("Posts.Moderate")]
    public async Task<IActionResult> ResolveReport(
        long id, [FromBody] ResolveReportRequest req, CancellationToken ct = default)
        => Ok(await moderation.ResolveReportAsync(CurrentUserId, id, req.Action, req.Reason, ct));

    /// <summary>`GET /api/admin/fvr-candidates?cursor=` — users flagged for Fake-vs-Real access.</summary>
    [HttpGet("api/admin/fvr-candidates")]
    [RequirePermission("Posts.Moderate")]
    public async Task<IActionResult> FvrCandidates(
        [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await moderation.ListFvrCandidatesAsync(cursor, ct));

    /// <summary>`POST /api/admin/users/{id}/fake-vs-real-access` — grant / revoke access.</summary>
    [HttpPost("api/admin/users/{id:long}/fake-vs-real-access")]
    [RequirePermission("Posts.Moderate")]
    public async Task<IActionResult> GrantFvrAccess(
        long id, [FromBody] GrantFvrAccessRequest req, CancellationToken ct = default)
        => Ok(await moderation.GrantFvrAccessAsync(CurrentUserId, id, req.Granted, ct));
}
