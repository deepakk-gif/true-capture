using TrueCapture.Modules.Messaging.Models;

namespace TrueCapture.Modules.Messaging.Hubs;

/// <summary>
/// Server → client SignalR events. Each connected user is in the group
/// <c>user:{userId}</c>, so the server pushes by addressing that group.
/// </summary>
public interface IChatClient
{
    /// <summary>A new message landed in one of the user's conversations.</summary>
    Task ReceiveMessage(MessageDto message);

    /// <summary>The other participant read up to <paramref name="lastReadMessageId"/>.</summary>
    Task MessageRead(long conversationId, long readerUserId, long lastReadMessageId);

    /// <summary>A message's reaction set changed.</summary>
    Task ReactionUpdated(long conversationId, long messageId, IReadOnlyList<ReactionDto> reactions);
}
