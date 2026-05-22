using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Messaging.Entities;

/// <summary>Content kind of a <see cref="Message"/>.</summary>
public enum MessageType
{
    Text  = 1,
    Image = 2,
    Video = 3,
}

/// <summary>
/// One chat message. Media (image / video) is uploaded via the signed-URL media
/// pipeline; the message stores the resulting public URL + thumbnail URL.
/// </summary>
public class Message : BaseEntity
{
    public long        ConversationId    { get; set; }
    public long        SenderId          { get; set; }
    public MessageType Type              { get; set; } = MessageType.Text;
    public string?     Text              { get; set; }
    public string?     MediaUrl          { get; set; }
    public string?     ThumbnailUrl      { get; set; }   // video / image preview
    public int?        MediaWidth        { get; set; }
    public int?        MediaHeight       { get; set; }
    public long?       ReplyToMessageId  { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User         Sender       { get; set; } = null!;
    public Message?     ReplyTo      { get; set; }

    public List<MessageReaction> Reactions { get; set; } = [];
}
