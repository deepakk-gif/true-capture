using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

/// <summary>Ephemeral 24-hour image stories.</summary>
public interface IStoryService
{
    Task<Result<StoryItem>> CreateAsync(long authorId, PostUpload image, string? caption, CancellationToken ct = default);

    /// <summary>Active stories of the viewer + the people they follow, grouped by author.</summary>
    Task<Result<StoryFeed>> GetFeedAsync(long viewerId, CancellationToken ct = default);

    Task<Result<bool>> DeleteAsync(long storyId, long userId, CancellationToken ct = default);
}
