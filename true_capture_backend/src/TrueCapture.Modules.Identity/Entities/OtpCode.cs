using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Identity.Entities;

public enum OtpPurpose
{
    VerifyEmail   = 1,
    PasswordReset = 2,
}

public class OtpCode : BaseEntity
{
    public long?      UserId        { get; set; }   // populated when known; null for forgot-password on unknown email
    public string     Email         { get; set; } = "";
    public string     CodeHash      { get; set; } = "";
    public OtpPurpose Purpose       { get; set; }
    public DateTime   ExpiresAtUtc  { get; set; }
    public DateTime?  UsedAtUtc     { get; set; }
    public int        AttemptCount  { get; set; }

    public User? User { get; set; }

    public bool IsActive => UsedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
