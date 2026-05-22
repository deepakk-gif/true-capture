using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>A "like" edge between a user and a post. Unique per (post, user).</summary>
public class PostLike : BaseEntity
{
    public long PostId { get; set; }
    public long UserId { get; set; }

    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
