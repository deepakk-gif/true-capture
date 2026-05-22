using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>A "like" edge between a user and a comment. Unique per (comment, user).</summary>
public class CommentLike : BaseEntity
{
    public long CommentId { get; set; }
    public long UserId    { get; set; }

    public Comment Comment { get; set; } = null!;
    public User    User    { get; set; } = null!;
}
