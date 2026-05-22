using TrueCapture.Modules.Messaging.Models;

namespace TrueCapture.Modules.Messaging.Services;

/// <summary>
/// Fans a chat event out over both channels: SignalR (live clients) and FCM
/// (background / killed clients). All methods are best-effort and never throw.
/// </summary>
public interface IChatNotifier
{
    Task BroadcastMessageAsync(IReadOnlyList<long> recipientUserIds, MessageDto message, CancellationToken ct = default);

    Task NotifyReadAsync(
        IReadOnlyList<long> recipientUserIds, long conversationId, long readerUserId,
        long lastReadMessageId, CancellationToken ct = default);

    Task NotifyReactionAsync(
        IReadOnlyList<long> recipientUserIds, long conversationId, long messageId,
        IReadOnlyList<ReactionDto> reactions, CancellationToken ct = default);

    /// <summary>FCM data-push of a new message to one recipient's devices.</summary>
    Task PushMessageAsync(
        long recipientUserId, long conversationId, long messageId,
        string senderName, string preview, CancellationToken ct = default);
}
