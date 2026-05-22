using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>State of a follow edge.</summary>
public enum FollowStatus
{
    /// <summary>An active follow — the follower sees the followee's content.</summary>
    Accepted = 1,

    /// <summary>A pending follow request awaiting the followee's approval (private accounts).</summary>
    Pending  = 2,
}

/// <summary>
/// A directed follow edge: <see cref="FollowerId"/> follows <see cref="FolloweeId"/>.
/// For a public followee the row is created <see cref="FollowStatus.Accepted"/>; for a
/// private followee it starts <see cref="FollowStatus.Pending"/> (a follow request).
/// </summary>
public class Follow : BaseEntity
{
    public long         FollowerId { get; set; }   // the user who follows
    public long         FolloweeId { get; set; }   // the user being followed
    public FollowStatus Status     { get; set; }

    public User Follower { get; set; } = null!;
    public User Followee { get; set; } = null!;
}
