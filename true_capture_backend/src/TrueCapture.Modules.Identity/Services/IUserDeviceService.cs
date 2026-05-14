using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Services;

public interface IUserDeviceService
{
    /// <summary>
    /// Upserts a UserDevice row for (userId, fcmToken). If the token already maps to a different user
    /// (device handed off between accounts), reassigns the row. Best-effort subscribes the token to the
    /// configured default topic ("all").
    /// </summary>
    Task<Result<bool>> RegisterAsync(long userId, string fcmToken, string? deviceType, CancellationToken ct = default);

    /// <summary>
    /// Removes the (userId, fcmToken) row and unsubscribes the token from the default topic.
    /// Idempotent — missing rows succeed silently.
    /// </summary>
    Task<Result<bool>> RemoveAsync(long userId, string fcmToken, CancellationToken ct = default);

    /// <summary>Bulk-deletes UserDevice rows whose FcmToken FCM reported as no longer valid.</summary>
    Task PruneInvalidAsync(IReadOnlyList<string> invalidTokens, CancellationToken ct = default);
}
