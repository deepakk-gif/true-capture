using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Infrastructure.Services;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Services;

public sealed class UserDeviceService(
    AppDbContext               db,
    IBaseService               baseService,
    IFcmSender                 fcm,
    IOptions<FirebaseOptions>  fbOpt,
    ILogger<UserDeviceService> log) : IUserDeviceService
{
    private readonly FirebaseOptions _fb = fbOpt.Value;

    public Task<Result<bool>> RegisterAsync(long userId, string fcmToken, string? deviceType, CancellationToken ct)
        => baseService.ExecuteAsync("UserDevice.Register", async () =>
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
                return Result<bool>.Success(true);

            var existing = await db.Set<UserDevice>().FirstOrDefaultAsync(d => d.FcmToken == fcmToken, ct);
            if (existing is null)
            {
                db.Set<UserDevice>().Add(new UserDevice
                {
                    UserId        = userId,
                    FcmToken      = fcmToken,
                    DeviceType    = deviceType,
                    LastUsedAtUtc = DateTime.UtcNow,
                });
            }
            else
            {
                existing.UserId        = userId;
                existing.DeviceType    = deviceType ?? existing.DeviceType;
                existing.LastUsedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);

            // Best-effort topic subscribe — never block the auth flow on FCM availability.
            try
            {
                await fcm.SubscribeAsync([fcmToken], _fb.DefaultTopic, ct);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "FCM topic subscribe failed for user {UserId}", userId);
            }

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<bool>> RemoveAsync(long userId, string fcmToken, CancellationToken ct)
        => baseService.ExecuteAsync("UserDevice.Remove", async () =>
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
                return Result<bool>.Success(true);

            try
            {
                await fcm.UnsubscribeAsync([fcmToken], _fb.DefaultTopic, ct);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "FCM topic unsubscribe failed for user {UserId}", userId);
            }

            var row = await db.Set<UserDevice>()
                .FirstOrDefaultAsync(d => d.UserId == userId && d.FcmToken == fcmToken, ct);
            if (row is not null)
            {
                db.Set<UserDevice>().Remove(row);
                await db.SaveChangesAsync(ct);
            }

            return Result<bool>.Success(true);
        }, ct, useTransaction: false);

    public async Task PushToUserAsync(long userId, NotificationPayload payload, CancellationToken ct)
    {
        try
        {
            var tokens = await db.Set<UserDevice>().AsNoTracking()
                .Where(d => d.UserId == userId)
                .Select(d => d.FcmToken)
                .ToListAsync(ct);
            if (tokens.Count == 0) return;

            var result = await fcm.SendToTokensAsync(tokens, payload, ct);
            if (result.InvalidTokens.Count > 0)
                await PruneInvalidAsync(result.InvalidTokens, ct);
        }
        catch (Exception ex)
        {
            // Best-effort — a push failure must never break the calling flow.
            log.LogWarning(ex, "Push to user {UserId} failed", userId);
        }
    }

    public async Task PruneInvalidAsync(IReadOnlyList<string> invalidTokens, CancellationToken ct)
    {
        if (invalidTokens.Count == 0) return;

        var rows = await db.Set<UserDevice>()
            .Where(d => invalidTokens.Contains(d.FcmToken))
            .ToListAsync(ct);
        if (rows.Count == 0) return;

        db.Set<UserDevice>().RemoveRange(rows);
        await db.SaveChangesAsync(ct);

        // Best-effort topic cleanup for the just-pruned tokens.
        try
        {
            await fcm.UnsubscribeAsync(invalidTokens, _fb.DefaultTopic, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "FCM topic unsubscribe for invalid tokens failed");
        }
    }
}
