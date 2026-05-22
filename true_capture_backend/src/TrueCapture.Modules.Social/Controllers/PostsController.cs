using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Modules.Social.Services;
using TrueCapture.Shared.Constants;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Social.Controllers;

/// <summary>Post creation, detail, engagement (like / save / share / vote), comments and reports.</summary>
[Authorize]
public sealed class PostsController(
    IPostService           posts,
    IEngagementService     engagement,
    IPostModerationService moderation) : BaseController
{
    /// <summary>`POST /api/posts` — create a Normal or Fake-vs-Real post from finalized media.</summary>
    [HttpPost("api/posts")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest req, CancellationToken ct = default)
        => Ok(await posts.CreateAsync(CurrentUserId, req, ct));

    /// <summary>`GET /api/posts/{id}` — full post detail; records a view.</summary>
    [HttpGet("api/posts/{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken ct = default)
        => Ok(await engagement.GetPostAsync(CurrentUserId, id, ct));

    /// <summary>`DELETE /api/posts/{id}` — delete a post the caller authored.</summary>
    [HttpDelete("api/posts/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
        => Ok(await posts.DeleteAsync(id, CurrentUserId, ct));

    /// <summary>`POST /api/posts/{id}/like` — toggle a like.</summary>
    [HttpPost("api/posts/{id:long}/like")]
    public async Task<IActionResult> Like(long id, CancellationToken ct = default)
        => Ok(await engagement.ToggleLikeAsync(CurrentUserId, id, ct));

    /// <summary>`POST /api/posts/{id}/save` — toggle a bookmark.</summary>
    [HttpPost("api/posts/{id:long}/save")]
    public async Task<IActionResult> Save(long id, CancellationToken ct = default)
        => Ok(await engagement.ToggleSaveAsync(CurrentUserId, id, ct));

    /// <summary>`POST /api/posts/{id}/share` — record a share, return the canonical URL.</summary>
    [HttpPost("api/posts/{id:long}/share")]
    public async Task<IActionResult> Share(long id, CancellationToken ct = default)
        => Ok(await engagement.ShareAsync(CurrentUserId, id, ct));

    /// <summary>`POST /api/posts/{id}/vote` — vote real/fake on a Fake-vs-Real post.</summary>
    [HttpPost("api/posts/{id:long}/vote")]
    public async Task<IActionResult> Vote(
        long id, [FromBody] VoteRequest req, CancellationToken ct = default)
        => Ok(await engagement.VoteAsync(CurrentUserId, id, req.Value, ct));

    /// <summary>`POST /api/posts/{id}/report` — report a post to the moderation queue.</summary>
    [HttpPost("api/posts/{id:long}/report")]
    public async Task<IActionResult> Report(
        long id, [FromBody] ReportPostRequest req, CancellationToken ct = default)
        => Ok(await moderation.ReportPostAsync(CurrentUserId, id, req.Reason, req.OtherText, ct));

    /// <summary>`GET /api/posts/{id}/comments?cursor=` — top-level comments, oldest first.</summary>
    [HttpGet("api/posts/{id:long}/comments")]
    public async Task<IActionResult> Comments(
        long id, [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await engagement.GetCommentsAsync(CurrentUserId, id, cursor, ct));

    /// <summary>`POST /api/posts/{id}/comments` — add a comment or a 1-level reply.</summary>
    [HttpPost("api/posts/{id:long}/comments")]
    public async Task<IActionResult> AddComment(
        long id, [FromBody] AddCommentRequest req, CancellationToken ct = default)
        => Ok(await engagement.AddCommentAsync(CurrentUserId, id, req.Text, req.ParentCommentId, ct));

    /// <summary>`GET /api/comments/{commentId}/replies?cursor=` — replies to a comment.</summary>
    [HttpGet("api/comments/{commentId:long}/replies")]
    public async Task<IActionResult> Replies(
        long commentId, [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await engagement.GetRepliesAsync(CurrentUserId, commentId, cursor, ct));

    /// <summary>`POST /api/comments/{commentId}/like` — toggle a like on a comment.</summary>
    [HttpPost("api/comments/{commentId:long}/like")]
    public async Task<IActionResult> LikeComment(long commentId, CancellationToken ct = default)
        => Ok(await engagement.ToggleCommentLikeAsync(CurrentUserId, commentId, ct));

    /// <summary>`DELETE /api/comments/{commentId}` — remove a comment (author or post owner).</summary>
    [HttpDelete("api/comments/{commentId:long}")]
    public async Task<IActionResult> DeleteComment(long commentId, CancellationToken ct = default)
        => Ok(await engagement.DeleteCommentAsync(CurrentUserId, commentId, ct));

    /// <summary>`GET /api/users/me/saves?cursor=` — the caller's saved posts.</summary>
    [HttpGet("api/users/me/saves")]
    public async Task<IActionResult> Saved([FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await engagement.GetSavedAsync(CurrentUserId, cursor, ct));
}
