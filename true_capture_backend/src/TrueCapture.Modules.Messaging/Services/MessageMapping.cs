using TrueCapture.Modules.Messaging.Entities;
using TrueCapture.Modules.Messaging.Models;

namespace TrueCapture.Modules.Messaging.Services;

/// <summary>Maps <see cref="Message"/> rows into <see cref="MessageDto"/>.</summary>
internal static class MessageMapping
{
    public static string TypeName(MessageType t) => t switch
    {
        MessageType.Image => "image",
        MessageType.Video => "video",
        _                 => "text",
    };

    /// <summary>Short text shown as the conversation-list preview / reply quote.</summary>
    public static string Preview(Message m) => m.Type switch
    {
        MessageType.Image => "📷 Photo",
        MessageType.Video => "🎞 Video",
        _                 => m.Text ?? "",
    };

    /// <summary>
    /// Builds the wire DTO. The message must be loaded with <c>Sender</c>,
    /// <c>ReplyTo.Sender</c> and <c>Reactions</c> included.
    /// </summary>
    public static MessageDto ToDto(Message m, long viewerId)
    {
        var reactions = m.Reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new ReactionDto(g.Key, g.Count(), g.Any(r => r.UserId == viewerId)))
            .ToList();

        MessageReplyDto? reply = null;
        if (m.ReplyTo is { } rt)
        {
            reply = new MessageReplyDto(
                rt.Id, rt.SenderId,
                rt.Sender?.DisplayName ?? rt.Sender?.Username ?? "",
                TypeName(rt.Type), Preview(rt));
        }

        return new MessageDto(
            m.Id, m.ConversationId, m.SenderId, TypeName(m.Type),
            m.Text, m.MediaUrl, m.ThumbnailUrl, m.MediaWidth, m.MediaHeight,
            reply, reactions, m.CreatedAtUtc);
    }
}
