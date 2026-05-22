using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>
/// A text comment on a post. <c>@username</c> tokens in <see cref="Text"/> mention users.
/// Replies are 1 level deep: a reply sets <see cref="ParentCommentId"/>; a reply to a
/// reply is rejected.
/// </summary>
public class Comment : BaseEntity
{
    public long   PostId          { get; set; }
    public long   AuthorId        { get; set; }
    public long?  ParentCommentId { get; set; }   // null = top-level comment
    public string Text            { get; set; } = "";
    public int    LikesCount      { get; set; }
    public bool   IsRemoved       { get; set; }   // soft delete — text shown as "[deleted]"

    public Post     Post   { get; set; } = null!;
    public User     Author { get; set; } = null!;
    public Comment? Parent { get; set; }
}
