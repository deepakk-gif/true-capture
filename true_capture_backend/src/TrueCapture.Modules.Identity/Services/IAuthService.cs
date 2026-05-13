using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Services;

public sealed record RegisterDto(string Email, string Username, string Password);
public sealed record LoginDto(string Email, string Password);
public sealed record RefreshDto(string RefreshToken);
public sealed record AuthTokensDto(string AccessToken, string RefreshToken, DateTime AccessExpiresAtUtc);

public interface IAuthService
{
    Task<Result<AuthTokensDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<Result<AuthTokensDto>> LoginAsync(LoginDto dto, string? userAgent, string? ip, CancellationToken ct = default);
    Task<Result<AuthTokensDto>> RefreshAsync(RefreshDto dto, string? userAgent, string? ip, CancellationToken ct = default);
    Task<Result<bool>>          LogoutAsync(string refreshToken, CancellationToken ct = default);
}
