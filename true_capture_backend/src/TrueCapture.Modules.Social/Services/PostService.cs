using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

public sealed class PostService(
    AppDbContext         db,
    IBaseService         baseService,
    INotificationService notifications) : IPostService
{
    private const int MaxCaptionLen   = 2200;
    private const int MaxMediaPerPost = 10;
    private const int MaxReferences   = 20;

    public Task<Result<PostDto>> CreateAsync(long authorId, CreatePostRequest req, CancellationToken ct)
        => baseService.ExecuteAsync("Post.Create", async () =>
        {
            var type = ParseType(req.Type);
            if (type is null)
                return Result<PostDto>.Validation(["Post type must be 'normal' or 'fakeVsReal'."]);

            if (type == PostType.FakeVsReal)
            {
                var canPost = await db.Set<User>().AsNoTracking()
                    .Where(u => u.Id == authorId).Select(u => u.CanPostFakeVsReal).FirstOrDefaultAsync(ct);
                if (!canPost)
                    return Result<PostDto>.Forbidden("You do not have access to upload Fake vs Real posts.");
            }

            return await CreateInternalAsync(
                authorId, type.Value, isAdminPost: false,
                req.MediaAssetIds, req.Caption, req.References, ct);
        }, ct, useTransaction: true);

    public Task<Result<PostDto>> AdminCreateAsync(long adminId, AdminCreatePostRequest req, CancellationToken ct)
        => baseService.ExecuteAsync("Post.AdminCreate", () =>
            CreateInternalAsync(
                adminId, PostType.FakeVsReal, isAdminPost: true,
                req.MediaAssetIds, req.Caption, req.References, ct),
            ct, useTransaction: true);

    private async Task<Result<PostDto>> CreateInternalAsync(
        long authorId, PostType type, bool isAdminPost,
        List<long> mediaIds, string? caption, List<string>? references, CancellationToken ct)
    {
        // ── Media ────────────────────────────────────────────────────────────────
        var distinctIds = mediaIds?.Distinct().ToList() ?? [];
        if (distinctIds.Count == 0)
            return Result<PostDto>.Validation(["A post must include at least one media item."]);
        if (distinctIds.Count > MaxMediaPerPost)
            return Result<PostDto>.Validation([$"A post may include at most {MaxMediaPerPost} media items."]);

        var assets = await db.Set<MediaAsset>()
            .Where(a => distinctIds.Contains(a.Id) && a.OwnerId == authorId)
            .ToListAsync(ct);
        if (assets.Count != distinctIds.Count)
            return Result<PostDto>.Validation(["One or more media items were not found or are not yours."]);
        if (assets.Any(a => a.Status != MediaStatus.Ready))
            return Result<PostDto>.Validation(["One or more media items are still processing."]);

        var hasPhoto = assets.Any(a => a.Kind == MediaKind.Photo);
        var hasVideo = assets.Any(a => a.Kind == MediaKind.Video);
        if (hasPhoto && hasVideo)
            return Result<PostDto>.Validation(["A post cannot mix photos and videos."]);

        var kind = hasVideo ? PostKind.Video
                 : assets.Count >= 2 ? PostKind.Carousel
                 : PostKind.Photo;

        // ── Caption ──────────────────────────────────────────────────────────────
        var trimmedCaption = caption?.Trim();
        if (trimmedCaption is { Length: > MaxCaptionLen })
            return Result<PostDto>.Validation([$"Caption must be {MaxCaptionLen} characters or fewer."]);
        if (type == PostType.FakeVsReal && string.IsNullOrWhiteSpace(trimmedCaption))
            return Result<PostDto>.Validation(["A caption is required for Fake vs Real posts."]);

        // ── Reference links (Fake vs Real only) ──────────────────────────────────
        var refs = type == PostType.FakeVsReal
            ? (references ?? [])
                .Select(r => r?.Trim() ?? "")
                .Where(r => r.Length > 0)
                .Distinct()
                .ToList()
            : [];
        if (type == PostType.FakeVsReal)
        {
            if (refs.Count == 0)
                return Result<PostDto>.Validation(["At least one reference link is required for Fake vs Real posts."]);
            if (refs.Count > MaxReferences)
                return Result<PostDto>.Validation([$"A post may include at most {MaxReferences} reference links."]);
            if (refs.Any(r => !IsHttpUrl(r)))
                return Result<PostDto>.Validation(["Reference links must be valid http(s) URLs."]);
        }

        // ── Persist ──────────────────────────────────────────────────────────────
        var ordered = distinctIds
            .Select(id => assets.First(a => a.Id == id))
            .ToList();
        var cover = ordered[0].ThumbnailUrl ?? ordered[0].Url;

        var post = new Post
        {
            AuthorId    = authorId,
            Type        = type,
            Kind        = kind,
            IsAdminPost = isAdminPost,
            Status      = PostStatus.Live,
            Caption     = string.IsNullOrWhiteSpace(trimmedCaption) ? null : trimmedCaption,
            CoverUrl    = cover,
            ShareId     = NewShareId(),
        };
        for (var i = 0; i < ordered.Count; i++)
            post.Media.Add(new PostMedia { MediaAssetId = ordered[i].Id, Position = i });
        for (var i = 0; i < refs.Count; i++)
            post.References.Add(new PostReference { Url = refs[i], Position = i });

        db.Set<Post>().Add(post);
        await db.SaveChangesAsync(ct);

        await ResolveAndNotifyMentionsAsync(authorId, post.Id, trimmedCaption, ct);

        // Keep the author's denormalized post counter in step (same transaction).
        await db.Set<User>().Where(u => u.Id == authorId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PostsCount, u => u.PostsCount + 1), ct);

        var saved = await LoadFullAsync(post.Id, ct);
        return Result<PostDto>.Success(await PostMapping.BuildAsync(db, authorId, saved!, ct));
    }

    public Task<Result<bool>> DeleteAsync(long postId, long currentUserId, CancellationToken ct)
        => baseService.ExecuteAsync("Post.Delete", async () =>
        {
            var post = await db.Set<Post>().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null) return Result<bool>.NotFound("Post not found.");
            if (post.AuthorId != currentUserId)
                return Result<bool>.Forbidden("You can only delete your own posts.");
            return await RemoveAsync(post, ct);
        }, ct, useTransaction: true);

    public Task<Result<bool>> AdminDeleteAsync(long postId, CancellationToken ct)
        => baseService.ExecuteAsync("Post.AdminDelete", async () =>
        {
            var post = await db.Set<Post>().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null) return Result<bool>.NotFound("Post not found.");
            return await RemoveAsync(post, ct);
        }, ct, useTransaction: true);

    public Task<Result<PostListResult>> GetByUserAsync(long authorId, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Post.GetByUser", async () =>
        {
            var q = db.Set<Post>().AsNoTracking().Where(p => p.AuthorId == authorId);
            if (Paging.DecodeCursor(cursor) is long c) q = q.Where(p => p.Id < c);

            var rows = await q.OrderByDescending(p => p.Id).Take(Paging.PageSize + 1).ToListAsync(ct);
            string? next = null;
            if (rows.Count > Paging.PageSize)
            {
                next = rows[Paging.PageSize - 1].Id.ToString();
                rows.RemoveAt(rows.Count - 1);
            }
            return Result<PostListResult>.Success(
                new PostListResult(rows.Select(PostItem.From).ToList(), next));
        }, ct);

    private async Task<Result<bool>> RemoveAsync(Post post, CancellationToken ct)
    {
        var authorId = post.AuthorId;
        db.Set<Post>().Remove(post);     // FK cascade drops media links, comments, likes, ...
        await db.SaveChangesAsync(ct);

        await db.Set<User>().Where(u => u.Id == authorId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PostsCount, u => u.PostsCount - 1), ct);
        return Result<bool>.Success(true);
    }

    private async Task<Post?> LoadFullAsync(long postId, CancellationToken ct)
        => await db.Set<Post>().AsNoTracking()
            .Include(p => p.Media).ThenInclude(m => m.Media)
            .Include(p => p.References)
            .FirstOrDefaultAsync(p => p.Id == postId, ct);

    /// <summary>
    /// Resolves <c>@username</c> mentions in a caption, persisting + notifying only
    /// where the mentioned user is public OR followed by the author.
    /// </summary>
    private async Task ResolveAndNotifyMentionsAsync(
        long authorId, long postId, string? caption, CancellationToken ct)
    {
        var usernames = Mentions.Extract(caption);
        if (usernames.Count == 0) return;

        var candidates = await db.Set<User>().AsNoTracking()
            .Where(u => usernames.Contains(u.Username.ToLower()) && u.Id != authorId)
            .Select(u => new { u.Id, u.AccountType })
            .ToListAsync(ct);

        foreach (var c in candidates)
        {
            var allowed = c.AccountType == AccountType.Public
                || await db.Set<Follow>().AsNoTracking().AnyAsync(
                    f => f.FollowerId == authorId && f.FolloweeId == c.Id
                         && f.Status == FollowStatus.Accepted, ct);
            if (!allowed) continue;

            db.Set<PostMention>().Add(new PostMention { PostId = postId, MentionedUserId = c.Id });
            await notifications.EmitAsync(c.Id, NotificationType.Mentioned,
                actorUserId: authorId, postId: postId, ct: ct);
        }
        await db.SaveChangesAsync(ct);
    }

    private static PostType? ParseType(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "normal"     => PostType.Normal,
        "fakevsreal" => PostType.FakeVsReal,
        _            => null,
    };

    private static bool IsHttpUrl(string s) =>
        Uri.TryCreate(s, UriKind.Absolute, out var u) &&
        (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    private static string NewShareId() => Guid.NewGuid().ToString("N")[..12];
}
