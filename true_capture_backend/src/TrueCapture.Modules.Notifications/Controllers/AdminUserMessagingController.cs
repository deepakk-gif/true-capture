using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Notifications.Models;
using TrueCapture.Modules.Notifications.Services;
using TrueCapture.Shared.Authorization;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Notifications.Controllers;

/// <summary>Admin per-user messaging — push notification, in-app notice, and email.</summary>
[Route("api/admin/users")]
[AdminOnly]
[RequirePermission("Users.Manage")]
public sealed class AdminUserMessagingController(IAdminUserMessagingService svc) : BaseController
{
    /// <summary>`POST /api/admin/users/{id}/notify` — send an FCM push to the user.</summary>
    [HttpPost("{id:long}/notify")]
    public async Task<IActionResult> Notify(
        long id, [FromBody] SendUserNotificationDto dto, CancellationToken ct = default)
        => Ok(await svc.NotifyAsync(id, dto.Title, dto.Body, ct));

    /// <summary>`POST /api/admin/users/{id}/notice` — create an in-app notice for the user.</summary>
    [HttpPost("{id:long}/notice")]
    public async Task<IActionResult> Notice(
        long id, [FromBody] SendUserNoticeDto dto, CancellationToken ct = default)
        => Ok(await svc.NoticeAsync(id, dto.Title, dto.Body, ct));

    /// <summary>`POST /api/admin/users/{id}/email` — email the user.</summary>
    [HttpPost("{id:long}/email")]
    public async Task<IActionResult> Email(
        long id, [FromBody] SendUserEmailDto dto, CancellationToken ct = default)
        => Ok(await svc.EmailAsync(id, dto.Subject, dto.Body, ct));
}
