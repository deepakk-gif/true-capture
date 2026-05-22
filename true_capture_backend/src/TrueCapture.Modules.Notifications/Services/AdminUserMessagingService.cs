using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Services;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Notifications.Services;

public sealed class AdminUserMessagingService(
    AppDbContext         db,
    IBaseService         baseService,
    IUserDeviceService   devices,
    INotificationService notifications,
    IEmailSender         emailSender) : IAdminUserMessagingService
{
    public Task<Result<bool>> NotifyAsync(long userId, string title, string body, CancellationToken ct)
        => baseService.ExecuteAsync("AdminUserMessaging.Notify", async () =>
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
                return Result<bool>.Validation(["Title and body are required."]);
            if (!await UserExistsAsync(userId, ct))
                return Result<bool>.NotFound("User not found.");

            await devices.PushToUserAsync(userId,
                new NotificationPayload(title.Trim(), body.Trim(),
                    new Dictionary<string, string> { ["type"] = "admin_notification" }), ct);
            return Result<bool>.Success(true);
        }, ct);

    public Task<Result<bool>> NoticeAsync(long userId, string title, string body, CancellationToken ct)
        => baseService.ExecuteAsync("AdminUserMessaging.Notice", async () =>
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
                return Result<bool>.Validation(["Title and body are required."]);
            if (!await UserExistsAsync(userId, ct))
                return Result<bool>.NotFound("User not found.");

            // An in-app notice is an AdminNotice item in the user's activity feed.
            await notifications.EmitAsync(userId, NotificationType.AdminNotice,
                text: $"{title.Trim()} — {body.Trim()}", ct: ct);
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<bool>> EmailAsync(long userId, string subject, string body, CancellationToken ct)
        => baseService.ExecuteAsync("AdminUserMessaging.Email", async () =>
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
                return Result<bool>.Validation(["Subject and body are required."]);

            var email = await db.Set<User>().AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);
            if (email is null)
                return Result<bool>.NotFound("User not found.");

            await emailSender.SendAsync(new EmailMessage(email, subject.Trim(), body.Trim()), ct);
            return Result<bool>.Success(true);
        }, ct);

    private Task<bool> UserExistsAsync(long userId, CancellationToken ct)
        => db.Set<User>().AnyAsync(u => u.Id == userId, ct);
}
