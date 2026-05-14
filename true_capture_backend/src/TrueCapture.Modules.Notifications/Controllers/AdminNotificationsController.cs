using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Notifications.Models;
using TrueCapture.Modules.Notifications.Services;
using TrueCapture.Shared.Authorization;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Notifications.Controllers;

[Route("api/admin/notifications")]
[AdminOnly]
public sealed class AdminNotificationsController(IAdminNotificationService svc) : BaseController
{
    /// <summary>Broadcasts to every device subscribed to a topic (e.g. "all").</summary>
    [HttpPost("send-topic")]
    public async Task<IActionResult> SendTopic([FromBody] SendTopicDto dto, CancellationToken ct = default)
        => Ok(await svc.SendToTopicAsync(dto, ct));

    /// <summary>Multicasts to every device belonging to the supplied user IDs.</summary>
    [HttpPost("send-users")]
    public async Task<IActionResult> SendUsers([FromBody] SendUsersDto dto, CancellationToken ct = default)
        => Ok(await svc.SendToUsersAsync(dto, ct));

    /// <summary>
    /// Multicasts to every device belonging to users matching the same filter set as
    /// <c>GET /api/admin/users</c> (search / isActive / isAdmin / isVerified / hasGoogle).
    /// </summary>
    [HttpPost("send-filtered")]
    public async Task<IActionResult> SendFiltered([FromBody] SendFilteredDto dto, CancellationToken ct = default)
        => Ok(await svc.SendToFilteredAsync(dto, ct));
}
