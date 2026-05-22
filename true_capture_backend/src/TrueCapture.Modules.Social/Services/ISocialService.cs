using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

/// <summary>The follow graph + user search + profile viewing.</summary>
public interface ISocialService
{
    Task<Result<UserSearchResult>> SearchAsync(long viewerId, string? query, int limit, CancellationToken ct = default);

    Task<Result<UserProfileView>> GetProfileAsync(long viewerId, long targetId, CancellationToken ct = default);

    /// <summary>Follow a public user (instant) or send a follow request to a private user.</summary>
    Task<Result<FollowActionResult>> FollowAsync(long viewerId, long targetId, CancellationToken ct = default);

    /// <summary>Unfollow, or cancel a pending follow request.</summary>
    Task<Result<FollowActionResult>> UnfollowAsync(long viewerId, long targetId, CancellationToken ct = default);

    Task<Result<FollowListResult>> GetFollowersAsync(long viewerId, long targetId, string? cursor, CancellationToken ct = default);
    Task<Result<FollowListResult>> GetFollowingAsync(long viewerId, long targetId, string? cursor, CancellationToken ct = default);

    /// <summary>Incoming pending follow requests for the current user.</summary>
    Task<Result<FollowListResult>> GetFollowRequestsAsync(long viewerId, string? cursor, CancellationToken ct = default);

    Task<Result<bool>> AcceptRequestAsync(long viewerId, long requesterId, CancellationToken ct = default);
    Task<Result<bool>> RejectRequestAsync(long viewerId, long requesterId, CancellationToken ct = default);

    /// <summary>A user's posts, gated by the same public/private rule as the profile.</summary>
    Task<Result<PostListResult>> GetUserPostsAsync(long viewerId, long targetId, string? cursor, CancellationToken ct = default);
}
