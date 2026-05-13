using Microsoft.AspNetCore.Authorization;
using TrueCapture.Shared.Authorization;
using TrueCapture.Shared.Constants;

namespace TrueCapture.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<RequirePermissionAttribute>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequirePermissionAttribute  requirement)
    {
        var raw = context.User.FindFirst(JwtClaims.Permissions)?.Value;
        if (string.IsNullOrEmpty(raw)) return Task.CompletedTask;

        var perms = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (perms.Contains(requirement.PermissionCode, StringComparer.OrdinalIgnoreCase))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class FeatureAuthorizationHandler : AuthorizationHandler<RequireFeatureAttribute>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequireFeatureAttribute     requirement)
    {
        var raw = context.User.FindFirst(JwtClaims.Features)?.Value;
        if (string.IsNullOrEmpty(raw)) return Task.CompletedTask;

        var feats = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (feats.Contains(requirement.FeatureCode, StringComparer.OrdinalIgnoreCase))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
