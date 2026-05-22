using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

/// <summary>Builds the two post feeds: the Home tab and the Fake vs Real tab.</summary>
public interface IFeedService
{
    /// <param name="channel">"home" (Normal posts) or "fake_vs_real" (Fake-vs-Real posts).</param>
    Task<Result<FeedResult>> GetAsync(
        long viewerId, string? channel, string? cursor, CancellationToken ct = default);
}
