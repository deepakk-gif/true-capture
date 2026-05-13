using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Services;

public sealed class GoogleAuthOptions
{
    public string ClientId { get; set; } = "";
}

public sealed class AuthService(
    AppDbContext              db,
    IBaseService              baseService,
    ITokenService             tokens,
    IOtpService               otps,
    IOptions<GoogleAuthOptions> googleOpt) : IAuthService
{
    private readonly GoogleAuthOptions _googleOpt = googleOpt.Value;

    public Task<Result<AuthTokensDto>> RegisterAsync(RegisterDto dto, CancellationToken ct)
        => baseService.ExecuteAsync("Auth.Register", async () =>
        {
            var email    = dto.Email.Trim().ToLowerInvariant();
            var username = dto.Username.Trim();

            if (await db.Set<User>().AnyAsync(u => u.Email    == email,    ct))
                return Result<AuthTokensDto>.Conflict("An account with that email already exists.");
            if (await db.Set<User>().AnyAsync(u => u.Username == username, ct))
                return Result<AuthTokensDto>.Conflict("That username is taken.");

            var user = new User
            {
                Email         = email,
                Username      = username,
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
                EmailVerified = false,
                IsActive      = true,
            };
            db.Set<User>().Add(user);
            await db.SaveChangesAsync(ct);

            var perms  = await LoadPermissionCodesAsync(user.Id, ct);
            var issued = IssueAndPersistRefresh(user, perms, null, null);
            await db.SaveChangesAsync(ct);

            return Result<AuthTokensDto>.Success(
                new AuthTokensDto(issued.AccessToken, issued.RefreshToken, issued.AccessExpiresAtUtc));
        }, ct, useTransaction: true);

    public Task<Result<AuthTokensDto>> LoginAsync(LoginDto dto, string? userAgent, string? ip, CancellationToken ct)
        => baseService.ExecuteAsync("Auth.Login", async () =>
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user  = await db.Set<User>().FirstOrDefaultAsync(u => u.Email == email, ct);

            if (user is null || !user.IsActive)
                return Result<AuthTokensDto>.Unauthorized("Invalid credentials.");
            if (string.IsNullOrEmpty(user.PasswordHash))
                return Result<AuthTokensDto>.Unauthorized("This account uses a social provider — sign in with Google.");
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Result<AuthTokensDto>.Unauthorized("Invalid credentials.");

            user.LastLoginAtUtc = DateTime.UtcNow;

            var perms  = await LoadPermissionCodesAsync(user.Id, ct);
            var issued = IssueAndPersistRefresh(user, perms, userAgent, ip);
            await db.SaveChangesAsync(ct);

            return Result<AuthTokensDto>.Success(
                new AuthTokensDto(issued.AccessToken, issued.RefreshToken, issued.AccessExpiresAtUtc));
        }, ct, useTransaction: true);

    public Task<Result<AuthTokensDto>> RefreshAsync(RefreshDto dto, string? userAgent, string? ip, CancellationToken ct)
        => baseService.ExecuteAsync("Auth.Refresh", async () =>
        {
            var hash = tokens.HashRefreshToken(dto.RefreshToken);
            var stored = await db.Set<RefreshToken>()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

            if (stored is null || !stored.IsActive || !stored.User.IsActive)
                return Result<AuthTokensDto>.Unauthorized("Refresh token is invalid or expired.");

            stored.RevokedAtUtc = DateTime.UtcNow;

            var perms  = await LoadPermissionCodesAsync(stored.UserId, ct);
            var issued = tokens.Issue(stored.User, perms);
            stored.ReplacedByHash = tokens.HashRefreshToken(issued.RefreshToken);

            db.Set<RefreshToken>().Add(new RefreshToken
            {
                UserId       = stored.UserId,
                TokenHash    = stored.ReplacedByHash,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                UserAgent    = userAgent,
                IpAddress    = ip,
            });
            await db.SaveChangesAsync(ct);

            return Result<AuthTokensDto>.Success(
                new AuthTokensDto(issued.AccessToken, issued.RefreshToken, issued.AccessExpiresAtUtc));
        }, ct, useTransaction: true);

    public Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct)
        => baseService.ExecuteAsync("Auth.Logout", async () =>
        {
            var hash = tokens.HashRefreshToken(refreshToken);
            var stored = await db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            if (stored is null) return Result<bool>.Success(true);   // idempotent

            stored.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);

    public Task<Result<AuthTokensDto>> VerifyOtpAndIssueAsync(
        VerifyOtpAndIssueDto dto, string? userAgent, string? ip, CancellationToken ct)
        => baseService.ExecuteAsync("Auth.VerifyOtpAndIssue", async () =>
        {
            var verify = await otps.VerifyAsync(new OtpVerifyRequest(dto.Email, dto.Code, dto.Purpose), ct);
            if (!verify.IsSuccess)
                return Result<AuthTokensDto>.Unauthorized(verify.Errors.FirstOrDefault() ?? "Invalid or expired code.");

            var user = verify.Value!.User;
            if (user is null || !user.IsActive)
                return Result<AuthTokensDto>.Unauthorized("Account is not active.");

            user.LastLoginAtUtc = DateTime.UtcNow;

            var perms  = await LoadPermissionCodesAsync(user.Id, ct);
            var issued = IssueAndPersistRefresh(user, perms, userAgent, ip);
            await db.SaveChangesAsync(ct);

            return Result<AuthTokensDto>.Success(
                new AuthTokensDto(issued.AccessToken, issued.RefreshToken, issued.AccessExpiresAtUtc));
        }, ct, useTransaction: true);

    public Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct)
        => otps.SendAsync(new OtpSendRequest(dto.Email, OtpPurpose.PasswordReset), ct);

    public Task<Result<bool>> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct)
        => baseService.ExecuteAsync("Auth.ResetPassword", async () =>
        {
            var verify = await otps.VerifyAsync(
                new OtpVerifyRequest(dto.Email, dto.Code, OtpPurpose.PasswordReset), ct);
            if (!verify.IsSuccess)
                return Result<bool>.Unauthorized(verify.Errors.FirstOrDefault() ?? "Invalid or expired code.");

            var user = verify.Value!.User
                ?? await db.Set<User>().FirstOrDefaultAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant(), ct);
            if (user is null || !user.IsActive)
                return Result<bool>.Unauthorized("Account is not active.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);

            // Revoke every active refresh token for this user
            var now    = DateTime.UtcNow;
            var active = await db.Set<RefreshToken>()
                .Where(t => t.UserId == user.Id && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
                .ToListAsync(ct);
            foreach (var t in active) t.RevokedAtUtc = now;

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<AuthTokensDto>> GoogleSignInAsync(
        GoogleSignInDto dto, string? userAgent, string? ip, CancellationToken ct)
        => baseService.ExecuteAsync("Auth.GoogleSignIn", async () =>
        {
            if (string.IsNullOrWhiteSpace(_googleOpt.ClientId))
                return Result<AuthTokensDto>.Failure("Google sign-in is not configured.");

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _googleOpt.ClientId },
                    });
            }
            catch (InvalidJwtException)
            {
                return Result<AuthTokensDto>.Unauthorized("Invalid Google token.");
            }

            if (string.IsNullOrEmpty(payload.Subject) || string.IsNullOrEmpty(payload.Email))
                return Result<AuthTokensDto>.Unauthorized("Google token missing required claims.");

            var email = payload.Email.Trim().ToLowerInvariant();

            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.GoogleSubject == payload.Subject, ct)
                    ?? await db.Set<User>().FirstOrDefaultAsync(u => u.Email == email, ct);

            if (user is null)
            {
                user = new User
                {
                    Email         = email,
                    Username      = await GenerateUniqueUsernameAsync(email, ct),
                    PasswordHash  = "",        // no password — Google account
                    DisplayName   = payload.Name,
                    AvatarUrl     = payload.Picture,
                    EmailVerified = payload.EmailVerified ?? true,
                    IsActive      = true,
                    GoogleSubject = payload.Subject,
                };
                db.Set<User>().Add(user);
                await db.SaveChangesAsync(ct);
            }
            else if (string.IsNullOrEmpty(user.GoogleSubject))
            {
                user.GoogleSubject = payload.Subject;
                if (!user.EmailVerified) user.EmailVerified = payload.EmailVerified ?? true;
            }

            if (!user.IsActive)
                return Result<AuthTokensDto>.Unauthorized("Account is not active.");

            user.LastLoginAtUtc = DateTime.UtcNow;

            var perms  = await LoadPermissionCodesAsync(user.Id, ct);
            var issued = IssueAndPersistRefresh(user, perms, userAgent, ip);
            await db.SaveChangesAsync(ct);

            return Result<AuthTokensDto>.Success(
                new AuthTokensDto(issued.AccessToken, issued.RefreshToken, issued.AccessExpiresAtUtc));
        }, ct, useTransaction: true);

    private IssuedTokens IssueAndPersistRefresh(User user, List<string> perms, string? userAgent, string? ip)
    {
        var issued = tokens.Issue(user, perms);
        db.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId       = user.Id,
            TokenHash    = tokens.HashRefreshToken(issued.RefreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            UserAgent    = userAgent,
            IpAddress    = ip,
        });
        return issued;
    }

    private async Task<string> GenerateUniqueUsernameAsync(string email, CancellationToken ct)
    {
        var basePart = email.Split('@')[0];
        var candidate = basePart;
        var suffix = 0;
        while (await db.Set<User>().AnyAsync(u => u.Username == candidate, ct))
        {
            suffix++;
            candidate = $"{basePart}{suffix}";
        }
        return candidate;
    }

    private async Task<List<string>> LoadPermissionCodesAsync(long userId, CancellationToken ct)
    {
        return await db.Set<UserRole>()
            .Where(ur => ur.UserId == userId)
            .Join(db.Set<RolePermission>(),
                ur => ur.RoleId,
                rp => rp.RoleId,
                (ur, rp) => rp.PermissionId)
            .Join(db.Set<Permission>(),
                pid => pid,
                p   => p.Id,
                (_, p) => p.Code)
            .Distinct()
            .ToListAsync(ct);
    }
}
