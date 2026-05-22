using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

public sealed class EngagementService(
    AppDbContext         db,
    IBaseService         baseService,
    INotificationService notifications,
    ICreatorScoreService creatorScore) : IEngagementService
{
    private const int CommentPageSize  = 30;
    private const int MaxCommentLength = 2000;
    private const string ShareBaseUrl  = "https://truecapture.app/post";

    // ─────────────────────────────── Post detail ──────────────────────────────

    public Task<Result<PostDto>> GetPostAsync(long viewerId, long postId, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.GetPost", async () =>
        {
            var post = await db.Set<Post>().AsNoTracking()
                .Include(p => p.Media).ThenInclude(m => m.Media)
                .Include(p => p.References)
                .FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null || post.Status == PostStatus.Removed)
                return Result<PostDto>.NotFound("Post not found.");

            var gate = await CanViewPostAsync(viewerId, post, ct);
            if (!gate.IsSuccess) return Result<PostDto>.Forbidden(gate.Errors.FirstOrDefault());

            // Record a distinct view (drives trending) and reflect it in the response.
            var alreadyViewed = await db.Set<PostView>()
                .AnyAsync(v => v.PostId == postId && v.ViewerId == viewerId, ct);
            if (!alreadyViewed)
            {
                db.Set<PostView>().Add(new PostView { PostId = postId, ViewerId = viewerId });
                await db.Set<Post>().Where(p => p.Id == postId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);
                await db.SaveChangesAsync(ct);
                post.ViewCount += 1;
            }

            return Result<PostDto>.Success(await PostMapping.BuildAsync(db, viewerId, post, ct));
        }, ct, useTransaction: true);

    // ─────────────────────────────── Like ─────────────────────────────────────

    public Task<Result<LikeResult>> ToggleLikeAsync(long userId, long postId, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.ToggleLike", async () =>
        {
            var post = await db.Set<Post>().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null) return Result<LikeResult>.NotFound("Post not found.");
            var gate = await CanViewPostAsync(userId, post, ct);
            if (!gate.IsSuccess) return Result<LikeResult>.Forbidden(gate.Errors.FirstOrDefault());

            var existing = await db.Set<PostLike>()
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, ct);
            bool liked;
            if (existing is null)
            {
                db.Set<PostLike>().Add(new PostLike { PostId = postId, UserId = userId });
                post.LikesCount += 1;
                liked = true;
                await notifications.EmitAsync(post.AuthorId, NotificationType.PostLiked,
                    actorUserId: userId, postId: postId, ct: ct);
            }
            else
            {
                db.Set<PostLike>().Remove(existing);
                post.LikesCount = Math.Max(0, post.LikesCount - 1);
                liked = false;
            }
            await db.SaveChangesAsync(ct);

            // The author's like tally feeds their creator score / Fake-vs-Real milestone.
            await creatorScore.RecomputeAndCheckAsync(post.AuthorId, ct);
            return Result<LikeResult>.Success(new LikeResult(liked, post.LikesCount));
        }, ct, useTransaction: true);

    // ─────────────────────────────── Save ─────────────────────────────────────

    public Task<Result<SaveResult>> ToggleSaveAsync(long userId, long postId, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.ToggleSave", async () =>
        {
            var post = await db.Set<Post>().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null) return Result<SaveResult>.NotFound("Post not found.");
            var gate = await CanViewPostAsync(userId, post, ct);
            if (!gate.IsSuccess) return Result<SaveResult>.Forbidden(gate.Errors.FirstOrDefault());

            var existing = await db.Set<PostSave>()
                .FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == userId, ct);
            bool saved;
            if (existing is null)
            {
                db.Set<PostSave>().Add(new PostSave { PostId = postId, UserId = userId });
                saved = true;
            }
            else
            {
                db.Set<PostSave>().Remove(existing);
                saved = false;
            }
            await db.SaveChangesAsync(ct);
            return Result<SaveResult>.Success(new SaveResult(saved));
        }, ct, useTransaction: true);

    public Task<Result<PostListResult>> GetSavedAsync(long userId, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.GetSaved", async () =>
        {
            var q = db.Set<PostSave>().AsNoTracking().Where(s => s.UserId == userId);
            if (Paging.DecodeCursor(cursor) is long c) q = q.Where(s => s.Id < c);

            var rows = await q.OrderByDescending(s => s.Id).Take(Paging.PageSize + 1)
                .Join(db.Set<Post>(), s => s.PostId, p => p.Id, (s, p) => new { SaveId = s.Id, Post = p })
                .ToListAsync(ct);

            string? next = null;
            if (rows.Count > Paging.PageSize)
            {
                next = rows[Paging.PageSize - 1].SaveId.ToString();
                rows.RemoveAt(rows.Count - 1);
            }
            return Result<PostListResult>.Success(
                new PostListResult(rows.Select(r => PostItem.From(r.Post)).ToList(), next));
        }, ct);

    // ─────────────────────────────── Share ────────────────────────────────────

    public Task<Result<ShareResult>> ShareAsync(long userId, long postId, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.Share", async () =>
        {
            var post = await db.Set<Post>().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null) return Result<ShareResult>.NotFound("Post not found.");
            var gate = await CanViewPostAsync(userId, post, ct);
            if (!gate.IsSuccess) return Result<ShareResult>.Forbidden(gate.Errors.FirstOrDefault());

            db.Set<PostShare>().Add(new PostShare { PostId = postId, UserId = userId });
            post.SharesCount += 1;
            await db.SaveChangesAsync(ct);
            return Result<ShareResult>.Success(new ShareResult($"{ShareBaseUrl}/{post.ShareId}"));
        }, ct, useTransaction: true);

    // ─────────────────────────────── Vote ─────────────────────────────────────

    public Task<Result<VoteResult>> VoteAsync(long userId, long postId, bool value, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.Vote", async () =>
        {
            var post = await db.Set<Post>().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null) return Result<VoteResult>.NotFound("Post not found.");
            if (post.Type != PostType.FakeVsReal)
                return Result<VoteResult>.Validation(["Voting is only available on Fake vs Real posts."]);

            var existing = await db.Set<PostVote>()
                .FirstOrDefaultAsync(v => v.PostId == postId && v.UserId == userId, ct);
            if (existing is null)
            {
                db.Set<PostVote>().Add(new PostVote { PostId = postId, UserId = userId, Value = value });
                if (value) post.TrueVotesCount += 1; else post.FalseVotesCount += 1;
            }
            else if (existing.Value != value)
            {
                existing.Value = value;
                if (value)
                {
                    post.TrueVotesCount  += 1;
                    post.FalseVotesCount  = Math.Max(0, post.FalseVotesCount - 1);
                }
                else
                {
                    post.FalseVotesCount += 1;
                    post.TrueVotesCount   = Math.Max(0, post.TrueVotesCount - 1);
                }
            }
            await db.SaveChangesAsync(ct);

            await creatorScore.RecomputeAndCheckAsync(post.AuthorId, ct);
            return Result<VoteResult>.Success(
                new VoteResult(post.TrueVotesCount, post.FalseVotesCount, value));
        }, ct, useTransaction: true);

    // ─────────────────────────────── Comments ─────────────────────────────────

    public Task<Result<CommentListResult>> GetCommentsAsync(
        long viewerId, long postId, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.GetComments", async () =>
        {
            var post = await db.Set<Post>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null) return Result<CommentListResult>.NotFound("Post not found.");
            var gate = await CanViewPostAsync(viewerId, post, ct);
            if (!gate.IsSuccess) return Result<CommentListResult>.Forbidden(gate.Errors.FirstOrDefault());

            var q = db.Set<Comment>().AsNoTracking()
                .Where(c => c.PostId == postId && c.ParentCommentId == null);
            return Result<CommentListResult>.Success(await PageCommentsAsync(viewerId, q, cursor, ct));
        }, ct);

    public Task<Result<CommentListResult>> GetRepliesAsync(
        long viewerId, long commentId, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.GetReplies", async () =>
        {
            var parent = await db.Set<Comment>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == commentId, ct);
            if (parent is null) return Result<CommentListResult>.NotFound("Comment not found.");
            var post = await db.Set<Post>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == parent.PostId, ct);
            if (post is null) return Result<CommentListResult>.NotFound("Post not found.");
            var gate = await CanViewPostAsync(viewerId, post, ct);
            if (!gate.IsSuccess) return Result<CommentListResult>.Forbidden(gate.Errors.FirstOrDefault());

            var q = db.Set<Comment>().AsNoTracking().Where(c => c.ParentCommentId == commentId);
            return Result<CommentListResult>.Success(await PageCommentsAsync(viewerId, q, cursor, ct));
        }, ct);

    public Task<Result<CommentItem>> AddCommentAsync(
        long userId, long postId, string text, long? parentCommentId, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.AddComment", async () =>
        {
            var trimmed = text?.Trim() ?? "";
            if (trimmed.Length == 0)
                return Result<CommentItem>.Validation(["Comment cannot be empty."]);
            if (trimmed.Length > MaxCommentLength)
                return Result<CommentItem>.Validation([$"Comment must be {MaxCommentLength} characters or fewer."]);

            var post = await db.Set<Post>().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null) return Result<CommentItem>.NotFound("Post not found.");
            var gate = await CanViewPostAsync(userId, post, ct);
            if (!gate.IsSuccess) return Result<CommentItem>.Forbidden(gate.Errors.FirstOrDefault());

            long? replyTargetAuthor = null;
            if (parentCommentId is long pid)
            {
                var parent = await db.Set<Comment>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == pid, ct);
                if (parent is null || parent.PostId != postId)
                    return Result<CommentItem>.Validation(["The comment being replied to was not found."]);
                // 1-level rule: a reply to a reply is rejected.
                if (parent.ParentCommentId is not null)
                    return Result<CommentItem>.Validation(["Replies can only be one level deep."]);
                replyTargetAuthor = parent.AuthorId;
            }

            var comment = new Comment
            {
                PostId          = postId,
                AuthorId        = userId,
                ParentCommentId = parentCommentId,
                Text            = trimmed,
            };
            db.Set<Comment>().Add(comment);
            post.CommentsCount += 1;

            await notifications.EmitAsync(post.AuthorId, NotificationType.Commented,
                actorUserId: userId, postId: postId, ct: ct);
            if (replyTargetAuthor is long rta && rta != post.AuthorId)
                await notifications.EmitAsync(rta, NotificationType.Commented,
                    actorUserId: userId, postId: postId, ct: ct);

            foreach (var mid in await ResolveMentionAsync(trimmed, ct))
                await notifications.EmitAsync(mid, NotificationType.Mentioned,
                    actorUserId: userId, postId: postId, ct: ct);

            await db.SaveChangesAsync(ct);

            var me = await db.Set<User>().AsNoTracking().FirstAsync(u => u.Id == userId, ct);
            return Result<CommentItem>.Success(new CommentItem(
                comment.Id, postId, parentCommentId, me.Id, me.Username, me.DisplayName,
                me.AvatarUrl, me.IsVerified, comment.Text, 0, false, 0, false, comment.CreatedAtUtc));
        }, ct, useTransaction: true);

    public Task<Result<bool>> DeleteCommentAsync(long userId, long commentId, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.DeleteComment", async () =>
        {
            var comment = await db.Set<Comment>().FirstOrDefaultAsync(c => c.Id == commentId, ct);
            if (comment is null) return Result<bool>.NotFound("Comment not found.");
            if (comment.IsRemoved) return Result<bool>.Success(true);

            var post = await db.Set<Post>().FirstOrDefaultAsync(p => p.Id == comment.PostId, ct);
            // The comment author OR the post owner may remove it.
            if (comment.AuthorId != userId && post?.AuthorId != userId)
                return Result<bool>.Forbidden("You can only delete your own comments.");

            comment.IsRemoved = true;                 // soft delete — keeps the thread shape
            if (post is not null) post.CommentsCount = Math.Max(0, post.CommentsCount - 1);
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<LikeResult>> ToggleCommentLikeAsync(long userId, long commentId, CancellationToken ct)
        => baseService.ExecuteAsync("Engagement.ToggleCommentLike", async () =>
        {
            var comment = await db.Set<Comment>().FirstOrDefaultAsync(c => c.Id == commentId, ct);
            if (comment is null) return Result<LikeResult>.NotFound("Comment not found.");

            var existing = await db.Set<CommentLike>()
                .FirstOrDefaultAsync(cl => cl.CommentId == commentId && cl.UserId == userId, ct);
            bool liked;
            if (existing is null)
            {
                db.Set<CommentLike>().Add(new CommentLike { CommentId = commentId, UserId = userId });
                comment.LikesCount += 1;
                liked = true;
            }
            else
            {
                db.Set<CommentLike>().Remove(existing);
                comment.LikesCount = Math.Max(0, comment.LikesCount - 1);
                liked = false;
            }
            await db.SaveChangesAsync(ct);
            return Result<LikeResult>.Success(new LikeResult(liked, comment.LikesCount));
        }, ct, useTransaction: true);

    // ─────────────────────────────── Helpers ──────────────────────────────────

    /// <summary>
    /// Pages a comment query oldest-first (keyset on ascending id) and resolves each
    /// row's author, like-state and reply count.
    /// </summary>
    private async Task<CommentListResult> PageCommentsAsync(
        long viewerId, IQueryable<Comment> q, string? cursor, CancellationToken ct)
    {
        if (Paging.DecodeCursor(cursor) is long c) q = q.Where(cm => cm.Id > c);

        var raw = await q.OrderBy(cm => cm.Id).Take(CommentPageSize + 1)
            .Join(db.Set<User>(), cm => cm.AuthorId, u => u.Id, (cm, u) => new { cm, u })
            .ToListAsync(ct);

        string? next = null;
        if (raw.Count > CommentPageSize)
        {
            next = raw[CommentPageSize - 1].cm.Id.ToString();
            raw.RemoveAt(raw.Count - 1);
        }

        var ids = raw.Select(x => x.cm.Id).ToList();
        var likedIds = ids.Count == 0
            ? []
            : await db.Set<CommentLike>().AsNoTracking()
                .Where(cl => ids.Contains(cl.CommentId) && cl.UserId == viewerId)
                .Select(cl => cl.CommentId).ToListAsync(ct);
        var likedSet = likedIds.ToHashSet();

        var replyCounts = ids.Count == 0
            ? new Dictionary<long, int>()
            : await db.Set<Comment>().AsNoTracking()
                .Where(r => r.ParentCommentId != null && ids.Contains(r.ParentCommentId.Value))
                .GroupBy(r => r.ParentCommentId!.Value)
                .Select(g => new { ParentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ParentId, x => x.Count, ct);

        var items = raw.Select(x => new CommentItem(
            x.cm.Id, x.cm.PostId, x.cm.ParentCommentId, x.u.Id, x.u.Username,
            x.u.DisplayName, x.u.AvatarUrl, x.u.IsVerified,
            x.cm.IsRemoved ? "[deleted]" : x.cm.Text,
            x.cm.LikesCount, likedSet.Contains(x.cm.Id),
            replyCounts.GetValueOrDefault(x.cm.Id),
            x.cm.IsRemoved, x.cm.CreatedAtUtc)).ToList();

        return new CommentListResult(items, next);
    }

    /// <summary>Distinct ids of existing users mentioned by <c>@username</c> in text.</summary>
    private async Task<List<long>> ResolveMentionAsync(string text, CancellationToken ct)
    {
        var usernames = Mentions.Extract(text);
        if (usernames.Count == 0) return [];
        return await db.Set<User>().AsNoTracking()
            .Where(u => usernames.Contains(u.Username.ToLower()))
            .Select(u => u.Id)
            .ToListAsync(ct);
    }

    /// <summary>Fake-vs-Real posts are always public; Normal posts honour author privacy.</summary>
    private async Task<Result> CanViewPostAsync(long viewerId, Post post, CancellationToken ct)
    {
        if (post.Type == PostType.FakeVsReal) return Result.Success();
        if (post.AuthorId == viewerId) return Result.Success();

        var author = await db.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == post.AuthorId, ct);
        if (author is null) return Result.NotFound("Post not found.");
        if (author.AccountType == AccountType.Public) return Result.Success();

        var follows = await db.Set<Follow>().AsNoTracking().AnyAsync(
            f => f.FollowerId == viewerId && f.FolloweeId == author.Id
                 && f.Status == FollowStatus.Accepted, ct);
        return follows ? Result.Success() : Result.Forbidden("This account is private.");
    }
}
