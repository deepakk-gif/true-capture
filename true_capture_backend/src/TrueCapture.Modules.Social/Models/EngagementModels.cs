namespace TrueCapture.Modules.Social.Models;

/// <summary>A comment or reply. <see cref="ParentCommentId"/> null = top-level.</summary>
public sealed record CommentItem(
    long      Id,
    long      PostId,
    long?     ParentCommentId,
    long      AuthorId,
    string    AuthorUsername,
    string?   AuthorDisplayName,
    string?   AuthorAvatarUrl,
    bool      AuthorIsBlueTick,
    string    Text,
    int       LikeCount,
    bool      LikedByMe,
    int       RepliesCount,
    bool      IsRemoved,
    DateTime  CreatedAtUtc);

public sealed record CommentListResult(IReadOnlyList<CommentItem> Items, string? NextCursor);

/// <summary>Body of `POST /api/posts/{id}/comments` — `parentCommentId` set = a reply.</summary>
public sealed record AddCommentRequest(string Text, long? ParentCommentId);

/// <summary>One ephemeral story.</summary>
public sealed record StoryItem(
    long      Id,
    long      AuthorId,
    string    ImageUrl,
    string?   Caption,
    DateTime  CreatedAtUtc,
    DateTime  ExpiresAtUtc);

/// <summary>An author and their currently-active stories — one ring in the story tray.</summary>
public sealed record UserStories(
    long      AuthorId,
    string    Username,
    string?   DisplayName,
    string?   AvatarUrl,
    IReadOnlyList<StoryItem> Stories);

public sealed record StoryFeed(IReadOnlyList<UserStories> Items);
