using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>Records that a user shared a post — feeds <see cref="Post.SharesCount"/>.</summary>
public class PostShare : BaseEntity
{
    public long UserId { get; set; }
    public long PostId { get; set; }

    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
