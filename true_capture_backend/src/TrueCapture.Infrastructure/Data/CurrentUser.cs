using Microsoft.AspNetCore.Http;
using TrueCapture.Shared.Constants;

namespace TrueCapture.Infrastructure.Data;

public sealed class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    public long? UserId
    {
        get
        {
            var raw = http.HttpContext?.User?.FindFirst(JwtClaims.UserId)?.Value;
            return long.TryParse(raw, out var id) ? id : null;
        }
    }
}
