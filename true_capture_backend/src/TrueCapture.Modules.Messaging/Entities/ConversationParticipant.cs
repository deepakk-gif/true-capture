using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Messaging.Entities;

/// <summary>
/// One user's membership in a <see cref="Conversation"/> — carries that user's
/// per-conversation state: pin, unread count and last-read marker.
/// </summary>
public class ConversationParticipant : BaseEntity
{
    public long      ConversationId    { get; set; }
    public long      UserId            { get; set; }
    public bool      IsPinned          { get; set; }
    public DateTime? PinnedAtUtc       { get; set; }
    public long?     LastReadMessageId { get; set; }
    public int       UnreadCount       { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User         User         { get; set; } = null!;
}
