namespace TrueCapture.Modules.Social.Services;

/// <summary>
/// Maintains <c>User.CreatorScore</c> and raises the Fake-vs-Real access milestone.
/// Call after any event that changes a user's followers / likes-received / up-votes.
/// </summary>
public interface ICreatorScoreService
{
    /// <summary>
    /// Recomputes <paramref name="userId"/>'s creator score from current data and, the
    /// first time it crosses the threshold, flags the user as a Fake-vs-Real access
    /// candidate and notifies the admin. Best-effort — never throws to the caller.
    /// </summary>
    Task RecomputeAndCheckAsync(long userId, CancellationToken ct = default);
}
