using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>Orders one <see cref="MediaAsset"/> within a <see cref="Post"/>.</summary>
public class PostMedia : BaseEntity
{
    public long PostId       { get; set; }
    public long MediaAssetId { get; set; }
    public int  Position     { get; set; }

    public Post       Post  { get; set; } = null!;
    public MediaAsset Media { get; set; } = null!;
}
