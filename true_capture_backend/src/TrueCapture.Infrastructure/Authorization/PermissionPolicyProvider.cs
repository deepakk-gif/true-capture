using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TrueCapture.Shared.Authorization;

namespace TrueCapture.Infrastructure.Authorization;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existing = await base.GetPolicyAsync(policyName);
        if (existing is not null) return existing;

        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var code = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new RequirePermissionAttribute(code))
                .Build();
        }

        if (policyName.StartsWith(RequireFeatureAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var code = policyName[RequireFeatureAttribute.PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new RequireFeatureAttribute(code))
                .Build();
        }

        return null;
    }
}
