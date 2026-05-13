using TrueCapture.Modules.Identity.Entities;

namespace TrueCapture.Modules.Identity.Services;

public sealed record IssuedTokens(string AccessToken, string RefreshToken, DateTime AccessExpiresAtUtc);

public interface ITokenService
{
    IssuedTokens Issue(User user, IEnumerable<string> permissionCodes);
    string       HashRefreshToken(string token);
}
