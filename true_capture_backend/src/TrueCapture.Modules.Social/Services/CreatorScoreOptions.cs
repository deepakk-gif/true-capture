namespace TrueCapture.Modules.Social.Services;

/// <summary>
/// Binds the <c>CreatorScore</c> section of appsettings.json. The score is a composite
/// signal of how much a user is trusted to post the right content:
/// <c>FollowerWeight·followers + LikeWeight·likesReceived + UpvoteWeight·fvrUpVotes</c>.
/// When it crosses <see cref="Threshold"/> the admin is notified to consider granting
/// Fake-vs-Real upload access.
/// </summary>
public sealed class CreatorScoreOptions
{
    public const string SectionName = "CreatorScore";

    public int FollowerWeight { get; set; } = 2;
    public int LikeWeight     { get; set; } = 1;
    public int UpvoteWeight   { get; set; } = 3;
    public int Threshold      { get; set; } = 500;
}
