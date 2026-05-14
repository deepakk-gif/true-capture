using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Modules.Notifications.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Notifications.Services;

public sealed class AdminNotificationService(
    AppDbContext        db,
    IBaseService        baseService,
    IFcmSender          fcm,
    IUserDeviceService  devices) : IAdminNotificationService
{
    private const int FcmMulticastBatch = 500;

    public Task<Result<SendNotificationResultDto>> SendToTopicAsync(SendTopicDto dto, CancellationToken ct)
        => baseService.ExecuteAsync("AdminNotifications.SendToTopic", async () =>
        {
            if (string.IsNullOrWhiteSpace(dto.Topic))
                return Result<SendNotificationResultDto>.Validation(["Topic is required."]);
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                return Result<SendNotificationResultDto>.Validation(["Title and body are required."]);

            await fcm.SendToTopicAsync(dto.Topic, new NotificationPayload(dto.Title, dto.Body, dto.Data), ct);

            // Topic sends don't surface per-device counts; report 1 fanout to FCM.
            return Result<SendNotificationResultDto>.Success(
                new SendNotificationResultDto(SentCount: 1, FailedCount: 0, InvalidTokensPruned: 0, TargetedDeviceCount: 0));
        }, ct, useTransaction: false);

    public Task<Result<SendNotificationResultDto>> SendToUsersAsync(SendUsersDto dto, CancellationToken ct)
        => baseService.ExecuteAsync("AdminNotifications.SendToUsers", async () =>
        {
            if (dto.UserIds.Count == 0)
                return Result<SendNotificationResultDto>.Validation(["At least one userId is required."]);
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                return Result<SendNotificationResultDto>.Validation(["Title and body are required."]);

            var tokens = await db.Set<UserDevice>().AsNoTracking()
                .Where(d => dto.UserIds.Contains(d.UserId))
                .Select(d => d.FcmToken)
                .ToListAsync(ct);

            var result = await FanOutAsync(tokens, new NotificationPayload(dto.Title, dto.Body, dto.Data), ct);
            return Result<SendNotificationResultDto>.Success(result);
        }, ct, useTransaction: false);

    public Task<Result<SendNotificationResultDto>> SendToFilteredAsync(SendFilteredDto dto, CancellationToken ct)
        => baseService.ExecuteAsync("AdminNotifications.SendToFiltered", async () =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                return Result<SendNotificationResultDto>.Validation(["Title and body are required."]);

            // Mirror filter logic from AdminUsersService.ListAsync.
            var users = db.Set<User>().AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(dto.Search))
            {
                var term = dto.Search.Trim().ToLowerInvariant();
                users = users.Where(u =>
                    u.Email.ToLower().Contains(term) ||
                    u.Username.ToLower().Contains(term));
            }
            if (dto.IsActive   is bool a) users = users.Where(u => u.IsActive   == a);
            if (dto.IsAdmin    is bool b) users = users.Where(u => u.IsAdmin    == b);
            if (dto.IsVerified is bool c) users = users.Where(u => u.IsVerified == c);
            if (dto.HasGoogle  is bool g) users = g
                ? users.Where(u => u.GoogleSubject != null && u.GoogleSubject != "")
                : users.Where(u => u.GoogleSubject == null || u.GoogleSubject == "");

            var tokens = await users
                .Join(db.Set<UserDevice>().AsNoTracking(),
                    u => u.Id,
                    d => d.UserId,
                    (_, d) => d.FcmToken)
                .ToListAsync(ct);

            var result = await FanOutAsync(tokens, new NotificationPayload(dto.Title, dto.Body, dto.Data), ct);
            return Result<SendNotificationResultDto>.Success(result);
        }, ct, useTransaction: false);

    private async Task<SendNotificationResultDto> FanOutAsync(
        List<string> tokens, NotificationPayload payload, CancellationToken ct)
    {
        if (tokens.Count == 0)
            return new SendNotificationResultDto(0, 0, 0, 0);

        var sent       = 0;
        var failed     = 0;
        var invalidAll = new List<string>();

        for (var i = 0; i < tokens.Count; i += FcmMulticastBatch)
        {
            var batch = tokens.GetRange(i, Math.Min(FcmMulticastBatch, tokens.Count - i));
            var res   = await fcm.SendToTokensAsync(batch, payload, ct);
            sent   += res.SuccessCount;
            failed += res.FailureCount;
            invalidAll.AddRange(res.InvalidTokens);
        }

        if (invalidAll.Count > 0)
            await devices.PruneInvalidAsync(invalidAll, ct);

        return new SendNotificationResultDto(sent, failed, invalidAll.Count, tokens.Count);
    }
}
