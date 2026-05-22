using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Messaging.Entities;

/// <summary>
/// A 1-to-1 chat between two users. Last-message fields are denormalized so the
/// conversation list renders without joining the <see cref="Message"/> table.
/// </summary>
public class Conversation : BaseEntity
{
    public DateTime  LastMessageAtUtc   { get; set; }
    public string?   LastMessagePreview { get; set; }
    public long?     LastMessageSenderId { get; set; }

    public List<ConversationParticipant> Participants { get; set; } = [];
}
