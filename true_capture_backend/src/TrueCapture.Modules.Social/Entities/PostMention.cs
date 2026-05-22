using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>
/// A resolved <c>@username</c> mention in a post caption. Only persisted when the
/// mentioned user is public OR followed by the author.
/// </summary>
public class PostMention : BaseEntity
{
    public long PostId          { get; set; }
    public long MentionedUserId { get; set; }

    public Post Post          { get; set; } = null!;
    public User MentionedUser { get; set; } = null!;
}
