using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

/// <summary>
/// Post engagement — detail view (records a view), like / save / share / vote toggles,
/// and threaded comments (1 reply level). All reads are privacy-gated for Normal posts;
/// Fake-vs-Real posts are always visible.
/// </summary>
public interface IEngagementService
{
    Task<Result<PostDto>> GetPostAsync(long viewerId, long postId, CancellationToken ct = default);

    Task<Result<LikeResult>>  ToggleLikeAsync (long userId, long postId, CancellationToken ct = default);
    Task<Result<SaveResult>>  ToggleSaveAsync (long userId, long postId, CancellationToken ct = default);
    Task<Result<ShareResult>> ShareAsync      (long userId, long postId, CancellationToken ct = default);
    Task<Result<VoteResult>>  VoteAsync       (long userId, long postId, bool value, CancellationToken ct = default);

    Task<Result<PostListResult>> GetSavedAsync(long userId, string? cursor, CancellationToken ct = default);

    Task<Result<CommentListResult>> GetCommentsAsync(long viewerId, long postId, string? cursor, CancellationToken ct = default);
    Task<Result<CommentListResult>> GetRepliesAsync (long viewerId, long commentId, string? cursor, CancellationToken ct = default);
    Task<Result<CommentItem>>       AddCommentAsync (long userId, long postId, string text, long? parentCommentId, CancellationToken ct = default);
    Task<Result<bool>>              DeleteCommentAsync(long userId, long commentId, CancellationToken ct = default);
    Task<Result<LikeResult>>        ToggleCommentLikeAsync(long userId, long commentId, CancellationToken ct = default);
}
