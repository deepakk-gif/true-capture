using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Services;

public sealed record RegisterDto(
    string  Email,
    string  Username,
    string  Password,
    string? FcmToken   = null,
    string? DeviceType = null);

public sealed record LoginDto(
    string  Email,
    string  Password,
    string? FcmToken   = null,
    string? DeviceType = null);

public sealed record RefreshDto(
    string  RefreshToken,
    string? FcmToken   = null,
    string? DeviceType = null);

public sealed record AuthTokensDto(string AccessToken, string RefreshToken, DateTime AccessExpiresAtUtc);

public sealed record ForgotPasswordDto(string Email);
public sealed record ResetPasswordDto(string Email, string Code, string NewPassword);

public sealed record VerifyOtpAndIssueDto(
    string     Email,
    string     Code,
    OtpPurpose Purpose,
    string?    FcmToken   = null,
    string?    DeviceType = null);

public sealed record GoogleSignInDto(
    string  IdToken,
    string? FcmToken   = null,
    string? DeviceType = null);

public interface IAuthService
{
    Task<Result<AuthTokensDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<Result<AuthTokensDto>> LoginAsync(LoginDto dto, string? userAgent, string? ip, CancellationToken ct = default);
    Task<Result<AuthTokensDto>> RefreshAsync(RefreshDto dto, string? userAgent, string? ip, CancellationToken ct = default);

    /// <summary>Revokes the refresh token. If <paramref name="fcmToken"/> is supplied, also removes that device row.</summary>
    Task<Result<bool>> LogoutAsync(string refreshToken, long currentUserId, string? fcmToken, CancellationToken ct = default);

    /// <summary>Verifies an OTP and issues fresh tokens if the OTP resolves to a user (used for email verification).</summary>
    Task<Result<AuthTokensDto>> VerifyOtpAndIssueAsync(VerifyOtpAndIssueDto dto, string? userAgent, string? ip, CancellationToken ct = default);

    /// <summary>Sends a password-reset OTP. Always returns Success to avoid enumeration.</summary>
    Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default);

    /// <summary>Consumes a password-reset OTP, updates PasswordHash, revokes all active refresh tokens.</summary>
    Task<Result<bool>> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);

    /// <summary>Validates a Google ID token; creates-or-matches user by GoogleSubject; returns tokens.</summary>
    Task<Result<AuthTokensDto>> GoogleSignInAsync(GoogleSignInDto dto, string? userAgent, string? ip, CancellationToken ct = default);
}
