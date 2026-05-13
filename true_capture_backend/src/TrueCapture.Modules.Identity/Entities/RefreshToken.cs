using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Identity.Entities;

public class RefreshToken : BaseEntity
{
    public long      UserId        { get; set; }
    public string    TokenHash     { get; set; } = "";
    public DateTime  ExpiresAtUtc  { get; set; }
    public DateTime? RevokedAtUtc  { get; set; }
    public string?   ReplacedByHash { get; set; }
    public string?   UserAgent     { get; set; }
    public string?   IpAddress     { get; set; }

    public User User { get; set; } = null!;

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
