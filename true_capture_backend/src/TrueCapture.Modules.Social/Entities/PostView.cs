using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>Records that a user has viewed a post — unique per (post, viewer) so
/// <see cref="Post.ViewCount"/> counts distinct viewers (drives trending).</summary>
public class PostView : BaseEntity
{
    public long PostId   { get; set; }
    public long ViewerId { get; set; }

    public Post Post { get; set; } = null!;
}
