using TrueCapture.Modules.Messaging.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Messaging.Services;

/// <summary>Conversation list, direct-conversation creation, pinning and read state.</summary>
public interface IConversationService
{
    Task<Result<ConversationListResult>> ListAsync(long userId, string? cursor, CancellationToken ct = default);

    /// <summary>Finds the 1-to-1 conversation with <paramref name="otherUserId"/>, creating it if absent.</summary>
    Task<Result<ConversationDto>> GetOrCreateDirectAsync(long userId, long otherUserId, CancellationToken ct = default);

    /// <summary>Pins / unpins a conversation for the caller — at most 3 pinned.</summary>
    Task<Result<bool>> PinAsync(long userId, long conversationId, bool pinned, CancellationToken ct = default);

    /// <summary>Marks the conversation read up to <paramref name="lastMessageId"/> for the caller.</summary>
    Task<Result<bool>> MarkReadAsync(long userId, long conversationId, long lastMessageId, CancellationToken ct = default);
}
