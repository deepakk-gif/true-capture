using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>An ephemeral image story. Expires 24h after creation.</summary>
public class Story : BaseEntity
{
    public long      AuthorId     { get; set; }
    public string    ImageUrl     { get; set; } = "";
    public string?   Caption      { get; set; }
    public DateTime  ExpiresAtUtc { get; set; }

    public User Author { get; set; } = null!;
}
