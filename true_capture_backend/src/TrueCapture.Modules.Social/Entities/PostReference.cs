using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>A reference link on a Fake-vs-Real post — at least one is mandatory.</summary>
public class PostReference : BaseEntity
{
    public long   PostId   { get; set; }
    public string Url      { get; set; } = "";
    public int    Position { get; set; }

    public Post Post { get; set; } = null!;
}
