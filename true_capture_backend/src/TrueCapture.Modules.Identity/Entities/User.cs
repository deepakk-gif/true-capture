using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Identity.Entities;

public class User : BaseEntity
{
    public string  Email          { get; set; } = "";
    public string  PasswordHash   { get; set; } = "";
    public string  Username       { get; set; } = "";
    public string? DisplayName    { get; set; }
    public string? AvatarUrl      { get; set; }
    public string? Bio            { get; set; }
    public bool    EmailVerified  { get; set; }
    public bool    IsActive       { get; set; } = true;
    public bool    IsAdmin        { get; set; }
    public bool    IsVerified     { get; set; }   // verified-creator badge
    public string? GoogleSubject  { get; set; }   // Google OAuth subject id
    public DateTime? LastLoginAtUtc { get; set; }

    public List<UserRole>     UserRoles     { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
