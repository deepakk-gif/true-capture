using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Services;

public sealed class AuthService(
    AppDbContext   db,
    IBaseService   baseService,
    ITokenService  tokens) : IAuthService
{
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

            var perms = await LoadPermissionCodesAsync(user.Id, ct);
            var issued = tokens.Issue(user, perms);

            db.Set<RefreshToken>().Add(new RefreshToken
            {
                UserId       = user.Id,
                TokenHash    = tokens.HashRefreshToken(issued.RefreshToken),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            });
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
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Result<AuthTokensDto>.Unauthorized("Invalid credentials.");

            user.LastLoginAtUtc = DateTime.UtcNow;

            var perms  = await LoadPermissionCodesAsync(user.Id, ct);
            var issued = tokens.Issue(user, perms);

            db.Set<RefreshToken>().Add(new RefreshToken
            {
                UserId       = user.Id,
                TokenHash    = tokens.HashRefreshToken(issued.RefreshToken),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                UserAgent    = userAgent,
                IpAddress    = ip,
            });
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

            if (stored is null || !stored.IsActive)
                return Result<AuthTokensDto>.Unauthorized("Refresh token is invalid or expired.");

            // Rotate
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
