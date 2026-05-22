using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Messaging.Entities;

/// <summary>
/// A user's emoji reaction on a message. One row per (message, user) — reacting
/// again replaces the emoji; clearing it removes the row.
/// </summary>
public class MessageReaction : BaseEntity
{
    public long   MessageId { get; set; }
    public long   UserId    { get; set; }
    public string Emoji     { get; set; } = "";

    public Message Message { get; set; } = null!;
    public User    User    { get; set; } = null!;
}
