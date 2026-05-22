using TrueCapture.Modules.Messaging.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Messaging.Services;

/// <summary>Message history, sending, emoji reactions and deletion.</summary>
public interface IMessageService
{
    /// <summary>Newest-first page of messages in a conversation (the caller must be a participant).</summary>
    Task<Result<MessageListResult>> ListAsync(
        long userId, long conversationId, string? cursor, CancellationToken ct = default);

    Task<Result<MessageDto>> SendAsync(
        long senderId, long conversationId, SendMessageRequest req, CancellationToken ct = default);

    /// <summary>Sets / replaces / clears (empty emoji) the caller's reaction on a message.</summary>
    Task<Result<IReadOnlyList<ReactionDto>>> ReactAsync(
        long userId, long messageId, string? emoji, CancellationToken ct = default);

    Task<Result<bool>> DeleteAsync(long userId, long messageId, CancellationToken ct = default);
}
