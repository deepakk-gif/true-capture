using Microsoft.AspNetCore.Authorization;

namespace TrueCapture.Shared.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireFeatureAttribute : AuthorizeAttribute, IAuthorizationRequirement
{
    public const string PolicyPrefix = "Feature:";
    public string FeatureCode { get; }

    public RequireFeatureAttribute(string code) : base(PolicyPrefix + code)
        => FeatureCode = code;
}
