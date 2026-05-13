namespace TrueCapture.Shared.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireCaptchaAttribute : Attribute { }
