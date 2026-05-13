using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Services;

public sealed class OtpService(
    AppDbContext  db,
    IBaseService  baseService,
    IEmailSender  email)
    : IOtpService
{
    private const int    CodeLength       = 6;
    private const int    ExpiryMinutes    = 10;
    private const int    RateLimitWindowMinutes = 60;
    private const int    RateLimitMaxSends      = 5;
    private const int    MaxVerifyAttempts      = 5;

    public Task<Result<bool>> SendAsync(OtpSendRequest req, CancellationToken ct)
        => baseService.ExecuteAsync("Otp.Send", async () =>
        {
            var email = req.Email.Trim().ToLowerInvariant();

            // Rate-limit per email + purpose
            var windowStart = DateTime.UtcNow.AddMinutes(-RateLimitWindowMinutes);
            var recentCount = await db.Set<OtpCode>()
                .CountAsync(o => o.Email == email && o.Purpose == req.Purpose && o.CreatedAtUtc >= windowStart, ct);
            if (recentCount >= RateLimitMaxSends)
                return Result<bool>.Validation(["Too many OTP requests. Try again later."]);

            // Lookup user (may be null for forgot-password on an unknown email — we still pretend success)
            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Email == email, ct);

            if (user is null && req.Purpose == OtpPurpose.PasswordReset)
            {
                // Don't leak enumeration: return success without dispatching anything.
                return Result<bool>.Success(true);
            }

            // Generate code (cryptographically random, zero-padded)
            var code = GenerateNumericCode(CodeLength);
            var hash = HashCode(code);

            db.Set<OtpCode>().Add(new OtpCode
            {
                UserId       = user?.Id,
                Email        = email,
                CodeHash     = hash,
                Purpose      = req.Purpose,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(ExpiryMinutes),
            });
            await db.SaveChangesAsync(ct);

            var subject = req.Purpose switch
            {
                OtpPurpose.VerifyEmail   => "Verify your True Capture email",
                OtpPurpose.PasswordReset => "Reset your True Capture password",
                _                         => "Your True Capture code",
            };
            var body = $"Your code is: {code}\nIt expires in {ExpiryMinutes} minutes.";

            await email.SendAsync(new EmailMessage(email, subject, body), ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<OtpVerifyResult>> VerifyAsync(OtpVerifyRequest req, CancellationToken ct)
        => baseService.ExecuteAsync("Otp.Verify", async () =>
        {
            var email = req.Email.Trim().ToLowerInvariant();
            var hash  = HashCode(req.Code);

            // Most-recent unused row for this email+purpose
            var row = await db.Set<OtpCode>()
                .Include(o => o.User)
                .Where(o => o.Email == email && o.Purpose == req.Purpose && o.UsedAtUtc == null)
                .OrderByDescending(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (row is null)
                return Result<OtpVerifyResult>.Unauthorized("Invalid or expired code.");

            if (DateTime.UtcNow >= row.ExpiresAtUtc)
                return Result<OtpVerifyResult>.Unauthorized("Invalid or expired code.");

            if (row.AttemptCount >= MaxVerifyAttempts)
                return Result<OtpVerifyResult>.Unauthorized("Too many attempts.");

            if (!CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.ASCII.GetBytes(row.CodeHash),
                    System.Text.Encoding.ASCII.GetBytes(hash)))
            {
                row.AttemptCount += 1;
                await db.SaveChangesAsync(ct);
                return Result<OtpVerifyResult>.Unauthorized("Invalid or expired code.");
            }

            row.UsedAtUtc = DateTime.UtcNow;
            if (row.User is not null && req.Purpose == OtpPurpose.VerifyEmail)
                row.User.EmailVerified = true;

            await db.SaveChangesAsync(ct);
            return Result<OtpVerifyResult>.Success(new OtpVerifyResult(row.User, row.Purpose));
        }, ct, useTransaction: true);

    private static string GenerateNumericCode(int length)
    {
        Span<byte> buf = stackalloc byte[4];
        var sb = new System.Text.StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            RandomNumberGenerator.Fill(buf);
            sb.Append((BitConverter.ToUInt32(buf) % 10).ToString());
        }
        return sb.ToString();
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}
