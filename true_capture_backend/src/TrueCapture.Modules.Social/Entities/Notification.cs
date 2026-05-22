using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>Kind of activity a <see cref="Notification"/> records.</summary>
public enum NotificationType
{
    FollowRequest    = 1,
    FollowAccepted   = 2,
    NewFollower      = 3,
    PostLiked        = 4,
    Commented        = 5,
    Mentioned        = 6,
    StoryMention     = 7,
    AccountSuspended = 8,
    AdminNotice      = 9,
}

/// <summary>
/// A single item in a user's activity feed. <see cref="ActorUserId"/> is who triggered
/// it (avatar in the UI); <see cref="PostId"/> is the related post when one applies
/// (thumbnail in the UI); <see cref="Text"/> carries any extra body (admin notice / caption).
/// </summary>
public class Notification : BaseEntity
{
    public long             RecipientUserId { get; set; }
    public NotificationType Type            { get; set; }
    public long?            ActorUserId     { get; set; }
    public long?            PostId          { get; set; }
    public string?          Text            { get; set; }
    public bool             IsRead          { get; set; }

    public User? Actor { get; set; }
}
