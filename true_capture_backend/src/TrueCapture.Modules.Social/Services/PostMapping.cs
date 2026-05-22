using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;

namespace TrueCapture.Modules.Social.Services;

/// <summary>Shared helpers that turn <see cref="Post"/> rows into <see cref="PostDto"/>.</summary>
internal static class PostMapping
{
    public static string TypeName(PostType t) => t == PostType.FakeVsReal ? "fakeVsReal" : "normal";

    public static string KindName(PostKind k) => k switch
    {
        PostKind.Carousel => "carousel",
        PostKind.Video    => "video",
        _                 => "photo",
    };

    public static string StatusName(PostStatus s) => s switch
    {
        PostStatus.PendingReview => "pendingReview",
        PostStatus.Removed       => "removed",
        _                        => "live",
    };

    /// <summary>Viewer → author follow-state: "none" | "following" | "requested".</summary>
    public static async Task<string> FollowStateAsync(
        AppDbContext db, long viewerId, long authorId, CancellationToken ct)
    {
        if (viewerId == authorId) return FollowStates.None;
        var follow = await db.Set<Follow>().AsNoTracking()
            .FirstOrDefaultAsync(f => f.FollowerId == viewerId && f.FolloweeId == authorId, ct);
        return follow?.Status switch
        {
            FollowStatus.Accepted => FollowStates.Following,
            FollowStatus.Pending  => FollowStates.Requested,
            _                     => FollowStates.None,
        };
    }

    /// <summary>
    /// Builds the full <see cref="PostDto"/> for one post. The post must be loaded with
    /// <c>Media.Media</c> and <c>References</c> included.
    /// </summary>
    public static async Task<PostDto> BuildAsync(
        AppDbContext db, long viewerId, Post post, CancellationToken ct)
    {
        var author = await db.Set<User>().AsNoTracking().FirstAsync(u => u.Id == post.AuthorId, ct);
        var followState = await FollowStateAsync(db, viewerId, post.AuthorId, ct);

        var likedByMe = await db.Set<PostLike>().AsNoTracking()
            .AnyAsync(l => l.PostId == post.Id && l.UserId == viewerId, ct);
        var savedByMe = await db.Set<PostSave>().AsNoTracking()
            .AnyAsync(s => s.PostId == post.Id && s.UserId == viewerId, ct);

        bool? myVote = null;
        if (post.Type == PostType.FakeVsReal)
        {
            var vote = await db.Set<PostVote>().AsNoTracking()
                .FirstOrDefaultAsync(v => v.PostId == post.Id && v.UserId == viewerId, ct);
            myVote = vote?.Value;
        }

        var media = post.Media
            .OrderBy(m => m.Position)
            .Select(m => new PostMediaDto(
                m.Media.Id,
                m.Media.Kind == MediaKind.Video ? "video" : "photo",
                m.Media.Url, m.Media.ThumbnailUrl, m.Media.DurationSeconds, m.Position))
            .ToList();

        var references = post.References
            .OrderBy(r => r.Position)
            .Select(r => r.Url)
            .ToList();

        return new PostDto(
            post.Id,
            TypeName(post.Type),
            KindName(post.Kind),
            StatusName(post.Status),
            post.IsAdminPost,
            post.ShareId,
            new PostAuthorDto(author.Id, author.Username, author.DisplayName,
                author.AvatarUrl, author.IsVerified, followState),
            post.Caption,
            media,
            references,
            post.CreatedAtUtc,
            post.ViewCount,
            post.LikesCount,
            post.CommentsCount,
            post.SharesCount,
            post.TrueVotesCount,
            post.FalseVotesCount,
            likedByMe,
            savedByMe,
            myVote);
    }
}
