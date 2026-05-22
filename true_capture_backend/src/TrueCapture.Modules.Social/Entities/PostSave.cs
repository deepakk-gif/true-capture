using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>A bookmark — the user saved this post to view later. Unique per (user, post).</summary>
public class PostSave : BaseEntity
{
    public long UserId { get; set; }
    public long PostId { get; set; }

    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
