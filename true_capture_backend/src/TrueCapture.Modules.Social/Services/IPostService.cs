using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

/// <summary>An uploaded file, decoupled from ASP.NET's <c>IFormFile</c> — used by stories.</summary>
public sealed record PostUpload(Stream Content, string FileName, string ContentType, long Length);

/// <summary>Create / delete posts, and the admin (privacy-bypassing) per-user post listing.</summary>
public interface IPostService
{
    /// <summary>Creates a Normal post, or a Fake-vs-Real post when the author has access.</summary>
    Task<Result<PostDto>> CreateAsync(long authorId, CreatePostRequest req, CancellationToken ct = default);

    /// <summary>Admin publishes a Fake-vs-Real post (`is_admin_post = true`).</summary>
    Task<Result<PostDto>> AdminCreateAsync(long adminId, AdminCreatePostRequest req, CancellationToken ct = default);

    /// <summary>Deletes a post the caller authored.</summary>
    Task<Result<bool>> DeleteAsync(long postId, long currentUserId, CancellationToken ct = default);

    /// <summary>Hard-deletes any post — admin moderation.</summary>
    Task<Result<bool>> AdminDeleteAsync(long postId, CancellationToken ct = default);

    /// <summary>Lists a user's posts ignoring privacy — admin report.</summary>
    Task<Result<PostListResult>> GetByUserAsync(long authorId, string? cursor, CancellationToken ct = default);
}
