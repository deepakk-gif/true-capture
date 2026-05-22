namespace TrueCapture.Modules.Messaging.Models;

/// <summary>The other participant of a 1-to-1 conversation.</summary>
public sealed record ChatUserDto(
    long    Id,
    string  Username,
    string? DisplayName,
    string? AvatarUrl,
    bool    IsBlueTick);

/// <summary>A row in the conversation list.</summary>
public sealed record ConversationDto(
    long        Id,
    ChatUserDto OtherUser,
    string?     LastMessagePreview,
    DateTime    LastMessageAtUtc,
    long?       LastMessageSenderId,
    bool        IsPinned,
    int         UnreadCount);

public sealed record ConversationListResult(IReadOnlyList<ConversationDto> Items, string? NextCursor);

/// <summary>The quoted message shown above a reply.</summary>
public sealed record MessageReplyDto(
    long    Id,
    long    SenderId,
    string  SenderName,
    string  Type,        // "text" | "image" | "video"
    string  Preview);

/// <summary>An aggregated emoji reaction bucket on a message.</summary>
public sealed record ReactionDto(string Emoji, int Count, bool Mine);

/// <summary>One chat message.</summary>
public sealed record MessageDto(
    long     Id,
    long     ConversationId,
    long     SenderId,
    string   Type,           // "text" | "image" | "video"
    string?  Text,
    string?  MediaUrl,
    string?  ThumbnailUrl,
    int?     MediaWidth,
    int?     MediaHeight,
    MessageReplyDto? ReplyTo,
    IReadOnlyList<ReactionDto> Reactions,
    DateTime CreatedAtUtc);

public sealed record MessageListResult(IReadOnlyList<MessageDto> Items, string? NextCursor);

// ─────────────────────────────── Request bodies ───────────────────────────────

/// <summary>Body of `POST /api/conversations`.</summary>
public sealed record CreateConversationRequest(long UserId);

/// <summary>Body of `POST /api/conversations/{id}/messages`.</summary>
public sealed record SendMessageRequest(
    string  Type,            // "text" | "image" | "video"
    string? Text,
    string? MediaUrl,
    string? ThumbnailUrl,
    int?    MediaWidth,
    int?    MediaHeight,
    long?   ReplyToMessageId);

/// <summary>Body of `POST /api/conversations/{id}/read`.</summary>
public sealed record MarkReadRequest(long LastMessageId);

/// <summary>Body of `POST /api/conversations/{id}/pin`.</summary>
public sealed record PinRequest(bool Pinned);

/// <summary>Body of `POST /api/messages/{id}/react` — empty/null emoji clears the reaction.</summary>
public sealed record ReactRequest(string? Emoji);
