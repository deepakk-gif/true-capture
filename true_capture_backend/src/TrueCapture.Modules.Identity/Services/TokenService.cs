using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Constants;

namespace TrueCapture.Modules.Identity.Services;

public sealed class JwtOptions
{
    public string Issuer        { get; set; } = "true-capture";
    public string Audience      { get; set; } = "true-capture-clients";
    public string SigningKey    { get; set; } = "";    // override via env var in prod
    public int    AccessMinutes { get; set; } = 15;
    public int    RefreshDays   { get; set; } = 30;
}

public sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _opt = options.Value;

    public IssuedTokens Issue(User user, IEnumerable<string> permissionCodes)
    {
        if (string.IsNullOrWhiteSpace(_opt.SigningKey) || _opt.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be set and >= 32 chars.");

        var now     = DateTime.UtcNow;
        var expires = now.AddMinutes(_opt.AccessMinutes);

        var claims = new List<Claim>
        {
            new(JwtClaims.UserId, user.Id.ToString()),
            new(JwtClaims.Email,  user.Email),
            new(JwtClaims.Name,   user.DisplayName ?? user.Username),
            new(JwtClaims.Role,   user.IsAdmin ? "Admin" : "User"),
            new(JwtClaims.Permissions, string.Join(',', permissionCodes)),
            new(JwtClaims.Features,    ""),
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt   = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims, now, expires, creds);
        var access = new JwtSecurityTokenHandler().WriteToken(jwt);

        var refresh = GenerateOpaqueToken();

        return new IssuedTokens(access, refresh, expires);
    }

    public string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
