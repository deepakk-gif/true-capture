using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Social.Services;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Social.Controllers;

/// <summary>The signed-in user's activity feed.</summary>
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService svc) : BaseController
{
    /// <summary>`GET /api/notifications?cursor=` — newest-first activity feed.</summary>
    [HttpGet]
    public async Task<IActionResult> Feed([FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await svc.GetFeedAsync(CurrentUserId, cursor, ct));

    /// <summary>`GET /api/notifications/unread-count` — unread badge count.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct = default)
        => Ok(await svc.UnreadCountAsync(CurrentUserId, ct));

    /// <summary>`POST /api/notifications/read-all` — marks every notification read.</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
        => Ok(await svc.MarkAllReadAsync(CurrentUserId, ct));
}
