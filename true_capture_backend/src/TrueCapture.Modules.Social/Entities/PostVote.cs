using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>
/// A true/false vote on a Fake-vs-Real post. One vote per (post, user) — re-voting
/// updates the row. <c>Value=true</c> means "real", <c>false</c> means "fake".
/// </summary>
public class PostVote : BaseEntity
{
    public long PostId { get; set; }
    public long UserId { get; set; }
    public bool Value  { get; set; }

    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
