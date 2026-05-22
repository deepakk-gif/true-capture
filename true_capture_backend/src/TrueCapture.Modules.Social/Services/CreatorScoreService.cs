using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

public sealed class CreatorScoreService(
    AppDbContext                  db,
    IOptions<CreatorScoreOptions> options,
    IFcmSender                    fcm,
    ILogger<CreatorScoreService>  log) : ICreatorScoreService
{
    /// <summary>FCM topic admin devices subscribe to for moderation alerts.</summary>
    public const string AdminTopic = "admins";

    private readonly CreatorScoreOptions _opt = options.Value;

    public async Task RecomputeAndCheckAsync(long userId, CancellationToken ct = default)
    {
        try
        {
            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null || user.IsAdmin) return;

            var likesReceived = await db.Set<PostLike>().AsNoTracking()
                .CountAsync(l => db.Set<Post>().Any(p => p.Id == l.PostId && p.AuthorId == userId), ct);

            var fvrUpVotes = await db.Set<Post>().AsNoTracking()
                .Where(p => p.AuthorId == userId && p.Type == PostType.FakeVsReal)
                .SumAsync(p => (int?)p.TrueVotesCount, ct) ?? 0;

            var score = _opt.FollowerWeight * user.FollowersCount
                      + _opt.LikeWeight     * likesReceived
                      + _opt.UpvoteWeight   * fvrUpVotes;

            user.CreatorScore = score;

            // Raise the milestone exactly once, for users who don't yet have access.
            if (!user.CanPostFakeVsReal
                && user.FvrCandidateNotifiedAtUtc is null
                && score >= _opt.Threshold)
            {
                user.FvrCandidateNotifiedAtUtc = DateTime.UtcNow;
                log.LogInformation(
                    "User {UserId} (@{Username}) crossed the Fake-vs-Real milestone with score {Score}",
                    user.Id, user.Username, score);
                await NotifyAdminsAsync(user, score, ct);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Score upkeep must never fail the surrounding engagement write.
            log.LogWarning(ex, "Creator-score recompute failed for user {UserId}", userId);
        }
    }

    private async Task NotifyAdminsAsync(User user, int score, CancellationToken ct)
    {
        try
        {
            await fcm.SendToTopicAsync(AdminTopic, new NotificationPayload(
                "Fake vs Real candidate",
                $"@{user.Username} reached a creator score of {score} — consider granting upload access.",
                new Dictionary<string, string> { ["type"] = "fvr_candidate", ["userId"] = user.Id.ToString() }),
                ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to push Fake-vs-Real candidate alert for user {UserId}", user.Id);
        }
    }
}
