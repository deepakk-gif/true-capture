using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

/// <summary>
/// The activity-feed: emitting notifications (also fires an FCM push) and reading them back.
/// Other modules (Users suspension, Notifications admin-notice) call <see cref="EmitAsync"/>.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Queues a notification row on the current <c>DbContext</c> (the caller's
    /// <c>SaveChangesAsync</c> persists it) and sends a best-effort push. A self-notification
    /// (actor == recipient) is skipped.
    /// </summary>
    Task EmitAsync(
        long recipientUserId,
        NotificationType type,
        long? actorUserId = null,
        long? postId = null,
        string? text = null,
        CancellationToken ct = default);

    Task<Result<NotificationFeed>> GetFeedAsync(long userId, string? cursor, CancellationToken ct = default);
    Task<Result<NotificationUnreadResult>> UnreadCountAsync(long userId, CancellationToken ct = default);
    Task<Result<bool>> MarkAllReadAsync(long userId, CancellationToken ct = default);
}
