namespace TrueCapture.Shared.Constants;

public static class Schemas
{
    public const string Identity      = "identity";
    public const string Audit         = "audit";
    public const string Cms           = "cms";
    public const string Social        = "social";
    public const string Notifications = "notifications";
    public const string Messaging     = "messaging";
}

public static class JwtClaims
{
    public const string UserId       = "sub";
    public const string Email        = "email";
    public const string Name         = "name";
    public const string Role         = "role";
    public const string Permissions  = "perms";
    public const string Features     = "feats";
}

public static class RateLimitPolicies
{
    public const string Auth   = "auth";
    public const string Public = "public";
    public const string Upload = "upload";
}
