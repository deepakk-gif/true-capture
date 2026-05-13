using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Services;

public sealed record OtpSendRequest(string Email, OtpPurpose Purpose);
public sealed record OtpVerifyRequest(string Email, string Code, OtpPurpose Purpose);

public interface IOtpService
{
    /// <summary>
    /// Generates a 6-digit OTP for the email + purpose, stores its hash with a 10-minute
    /// expiry, and dispatches it via the configured <see cref="IEmailSender"/>.
    /// Enforces a per-email send rate-limit (max 5 in any rolling 60-minute window).
    /// Always returns <c>Success</c> for unknown emails to avoid enumeration leaks.
    /// </summary>
    Task<Result<bool>> SendAsync(OtpSendRequest request, CancellationToken ct = default);

    /// <summary>
    /// Verifies a submitted OTP. Marks the row used on success.
    /// Returns the resolved <see cref="User"/> when one exists.
    /// </summary>
    Task<Result<OtpVerifyResult>> VerifyAsync(OtpVerifyRequest request, CancellationToken ct = default);
}

public sealed record OtpVerifyResult(User? User, OtpPurpose Purpose);
