using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Identity.Entities;

/// <summary>User-selected gender. Null = not specified.</summary>
public enum Gender
{
    Male   = 1,
    Female = 2,
    Other  = 3,
}

/// <summary>Profile visibility. New accounts default to <see cref="Public"/>.</summary>
public enum AccountType
{
    Public  = 1,
    Private = 2,
}

public class User : BaseEntity
{
    public string  Email          { get; set; } = "";
    public string  PasswordHash   { get; set; } = "";
    public string  Username       { get; set; } = "";
    public string? DisplayName    { get; set; }
    public string? AvatarUrl      { get; set; }
    public string? Bio            { get; set; }
    public bool    EmailVerified  { get; set; }
    public bool    IsActive       { get; set; } = true;
    public bool    IsAdmin        { get; set; }
    public bool    IsVerified     { get; set; }   // verified-creator badge (blue tick)
    public string? GoogleSubject  { get; set; }   // Google OAuth subject id
    public DateTime? LastLoginAtUtc { get; set; }

    // Fake-vs-Real upload access. An admin grants this once the user proves they post
    // trustworthy content; granting it also sets IsVerified (the blue tick).
    public bool      CanPostFakeVsReal         { get; set; }
    // Composite credibility signal — followers + likes received + Fake-vs-Real up-votes.
    // When it crosses the configured threshold the admin is notified to consider a grant.
    public int       CreatorScore              { get; set; }
    public DateTime? FvrCandidateNotifiedAtUtc { get; set; }   // set once, when first notified

    // Denormalized social counters. Maintained transactionally on follow / unfollow /
    // follow-request accept / post create / post delete (see SocialService, PostService).
    // Recomputable from the Follow / Post tables if they ever drift.
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostsCount     { get; set; }

    public Gender?     Gender      { get; set; }                       // null = not specified
    public AccountType AccountType { get; set; } = AccountType.Public;  // profile visibility

    public List<UserRole>     UserRoles     { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
    public List<UserDevice>   UserDevices   { get; set; } = [];
}
