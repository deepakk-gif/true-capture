namespace TrueCapture.Modules.Social.Models;

/// <summary>One activity-feed entry. <c>Type</c> drives the icon, text and tap target.</summary>
public sealed record NotificationItem(
    long      Id,
    string    Type,             // FollowRequest | FollowAccepted | NewFollower | PostLiked |
                                // Commented | Mentioned | StoryMention | AccountSuspended | AdminNotice
    long?     ActorUserId,
    string?   ActorUsername,
    string?   ActorDisplayName,
    string?   ActorAvatarUrl,
    long?     PostId,
    string?   PostImageUrl,     // thumbnail for like/comment/mention entries
    string?   Text,             // extra body (admin notice, suspension reason, ...)
    bool      IsRead,
    DateTime  CreatedAtUtc);

public sealed record NotificationFeed(IReadOnlyList<NotificationItem> Items, string? NextCursor);

public sealed record NotificationUnreadResult(int Count);
