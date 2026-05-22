using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

public sealed class SocialService(
    AppDbContext         db,
    IBaseService         baseService,
    INotificationService notifications) : ISocialService
{
    // ---- Search ---------------------------------------------------------

    public Task<Result<UserSearchResult>> SearchAsync(long viewerId, string? query, int limit, CancellationToken ct)
        => baseService.ExecuteAsync("Social.Search", async () =>
        {
            var term = query?.Trim().ToLowerInvariant() ?? "";
            if (term.Length == 0)
                return Result<UserSearchResult>.Success(new UserSearchResult([]));

            limit = Math.Clamp(limit <= 0 ? 20 : limit, 1, 50);

            var users = await db.Set<User>().AsNoTracking()
                .Where(u => u.Id != viewerId && u.IsActive &&
                    (u.Username.ToLower().Contains(term) ||
                     (u.DisplayName != null && u.DisplayName.ToLower().Contains(term))))
                .OrderBy(u => u.Username)
                .Take(limit)
                .Select(u => new { u.Id, u.Username, u.DisplayName, u.AvatarUrl, u.IsVerified })
                .ToListAsync(ct);

            var ids = users.Select(u => u.Id).ToList();
            var followStates = await FollowStatesAsync(viewerId, ids, ct);

            // Mutual followers = people the viewer follows who also follow the result user.
            var viewerFollowing = db.Set<Follow>().AsNoTracking()
                .Where(f => f.FollowerId == viewerId && f.Status == FollowStatus.Accepted)
                .Select(f => f.FolloweeId);

            var mutualRows = await db.Set<Follow>().AsNoTracking()
                .Where(f => ids.Contains(f.FolloweeId) && f.Status == FollowStatus.Accepted &&
                            viewerFollowing.Contains(f.FollowerId))
                .Join(db.Set<User>(), f => f.FollowerId, u => u.Id,
                    (f, u) => new { f.FolloweeId, Name = u.DisplayName ?? u.Username })
                .ToListAsync(ct);

            var mutualByUser = mutualRows
                .GroupBy(m => m.FolloweeId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

            var items = users.Select(u =>
            {
                var mutual = mutualByUser.GetValueOrDefault(u.Id) ?? [];
                return new UserSearchItem(
                    u.Id, u.Username, u.DisplayName, u.AvatarUrl, u.IsVerified,
                    MutualFollowers:      mutual.Take(2).ToList(),
                    MutualFollowersCount: mutual.Count,
                    FollowState:          followStates.GetValueOrDefault(u.Id, FollowStates.None));
            }).ToList();

            return Result<UserSearchResult>.Success(new UserSearchResult(items));
        }, ct);

    // ---- Profile --------------------------------------------------------

    public Task<Result<UserProfileView>> GetProfileAsync(long viewerId, long targetId, CancellationToken ct)
        => baseService.ExecuteAsync("Social.GetProfile", async () =>
        {
            var target = await db.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetId, ct);
            if (target is null) return Result<UserProfileView>.NotFound("User not found.");

            var isMe        = targetId == viewerId;
            var followState = (await FollowStatesAsync(viewerId, [targetId], ct))
                                  .GetValueOrDefault(targetId, FollowStates.None);
            var followsMe   = await db.Set<Follow>().AsNoTracking().AnyAsync(
                f => f.FollowerId == targetId && f.FolloweeId == viewerId && f.Status == FollowStatus.Accepted, ct);
            var canView     = isMe || target.AccountType == AccountType.Public ||
                              followState == FollowStates.Following;

            return Result<UserProfileView>.Success(new UserProfileView(
                target.Id, target.Username, target.DisplayName, target.AvatarUrl, target.Bio,
                IsBlueTick:     target.IsVerified,
                AccountType:    target.AccountType.ToString().ToLowerInvariant(),
                JoinedAtUtc:    target.CreatedAtUtc,
                FollowersCount: target.FollowersCount,
                FollowingCount: target.FollowingCount,
                PostsCount:     target.PostsCount,
                FollowState:    followState,
                FollowsMe:      followsMe,
                IsMe:           isMe,
                CanViewContent: canView));
        }, ct);

    // ---- Follow / unfollow ---------------------------------------------

    public Task<Result<FollowActionResult>> FollowAsync(long viewerId, long targetId, CancellationToken ct)
        => baseService.ExecuteAsync("Social.Follow", async () =>
        {
            if (targetId == viewerId)
                return Result<FollowActionResult>.Validation(["You cannot follow yourself."]);

            var target = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == targetId, ct);
            if (target is null || !target.IsActive)
                return Result<FollowActionResult>.NotFound("User not found.");

            var existing = await db.Set<Follow>()
                .FirstOrDefaultAsync(f => f.FollowerId == viewerId && f.FolloweeId == targetId, ct);
            if (existing is not null)
                return Result<FollowActionResult>.Success(new FollowActionResult(StateOf(existing.Status)));

            var status = target.AccountType == AccountType.Private
                ? FollowStatus.Pending
                : FollowStatus.Accepted;

            db.Set<Follow>().Add(new Follow
            {
                FollowerId = viewerId,
                FolloweeId = targetId,
                Status     = status,
            });

            // Activity notification + push (EmitAsync queues the row; SaveChanges commits both).
            await notifications.EmitAsync(targetId,
                status == FollowStatus.Pending
                    ? NotificationType.FollowRequest
                    : NotificationType.NewFollower,
                actorUserId: viewerId, ct: ct);
            await db.SaveChangesAsync(ct);

            // An accepted edge (public account) takes effect immediately; a pending
            // request does not count until the followee accepts it.
            if (status == FollowStatus.Accepted)
                await AdjustFollowCountsAsync(followerId: viewerId, followeeId: targetId, delta: +1, ct);

            return Result<FollowActionResult>.Success(new FollowActionResult(StateOf(status)));
        }, ct, useTransaction: true);

    public Task<Result<FollowActionResult>> UnfollowAsync(long viewerId, long targetId, CancellationToken ct)
        => baseService.ExecuteAsync("Social.Unfollow", async () =>
        {
            var existing = await db.Set<Follow>()
                .FirstOrDefaultAsync(f => f.FollowerId == viewerId && f.FolloweeId == targetId, ct);
            if (existing is not null)
            {
                var wasAccepted = existing.Status == FollowStatus.Accepted;
                db.Set<Follow>().Remove(existing);
                await db.SaveChangesAsync(ct);

                // Only an accepted edge contributed to the counters.
                if (wasAccepted)
                    await AdjustFollowCountsAsync(followerId: viewerId, followeeId: targetId, delta: -1, ct);
            }
            return Result<FollowActionResult>.Success(new FollowActionResult(FollowStates.None));
        }, ct, useTransaction: true);

    // ---- Followers / following / requests ------------------------------

    public Task<Result<FollowListResult>> GetFollowersAsync(long viewerId, long targetId, string? cursor, CancellationToken ct)
        => GuardedListAsync(viewerId, targetId, cursor, ct,
            db.Set<Follow>().AsNoTracking()
                .Where(f => f.FolloweeId == targetId && f.Status == FollowStatus.Accepted),
            otherIsFollower: true);

    public Task<Result<FollowListResult>> GetFollowingAsync(long viewerId, long targetId, string? cursor, CancellationToken ct)
        => GuardedListAsync(viewerId, targetId, cursor, ct,
            db.Set<Follow>().AsNoTracking()
                .Where(f => f.FollowerId == targetId && f.Status == FollowStatus.Accepted),
            otherIsFollower: false);

    public Task<Result<FollowListResult>> GetFollowRequestsAsync(long viewerId, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Social.FollowRequests", async () =>
        {
            var q = db.Set<Follow>().AsNoTracking()
                .Where(f => f.FolloweeId == viewerId && f.Status == FollowStatus.Pending);
            return Result<FollowListResult>.Success(
                await PageFollowRowsAsync(viewerId, q, otherIsFollower: true, cursor, ct));
        }, ct);

    public Task<Result<bool>> AcceptRequestAsync(long viewerId, long requesterId, CancellationToken ct)
        => baseService.ExecuteAsync("Social.AcceptRequest", async () =>
        {
            var row = await db.Set<Follow>().FirstOrDefaultAsync(
                f => f.FollowerId == requesterId && f.FolloweeId == viewerId && f.Status == FollowStatus.Pending, ct);
            if (row is null) return Result<bool>.NotFound("Follow request not found.");

            row.Status = FollowStatus.Accepted;
            await notifications.EmitAsync(requesterId, NotificationType.FollowAccepted,
                actorUserId: viewerId, ct: ct);
            await db.SaveChangesAsync(ct);

            // The pending request now becomes an active edge — count it.
            await AdjustFollowCountsAsync(followerId: requesterId, followeeId: viewerId, delta: +1, ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<bool>> RejectRequestAsync(long viewerId, long requesterId, CancellationToken ct)
        => baseService.ExecuteAsync("Social.RejectRequest", async () =>
        {
            var row = await db.Set<Follow>().FirstOrDefaultAsync(
                f => f.FollowerId == requesterId && f.FolloweeId == viewerId && f.Status == FollowStatus.Pending, ct);
            if (row is null) return Result<bool>.NotFound("Follow request not found.");

            db.Set<Follow>().Remove(row);
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ---- Posts (privacy-gated list) ------------------------------------

    public Task<Result<PostListResult>> GetUserPostsAsync(long viewerId, long targetId, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Social.UserPosts", async () =>
        {
            var target = await db.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetId, ct);
            if (target is null) return Result<PostListResult>.NotFound("User not found.");
            if (!await CanViewAsync(viewerId, target, ct))
                return Result<PostListResult>.Forbidden("This account is private.");

            var q = db.Set<Post>().AsNoTracking().Where(p => p.AuthorId == targetId);
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

    // ---- Helpers --------------------------------------------------------

    /// <summary>Runs a followers/following list behind the public/private gate.</summary>
    private Task<Result<FollowListResult>> GuardedListAsync(
        long viewerId, long targetId, string? cursor, CancellationToken ct,
        IQueryable<Follow> followQuery, bool otherIsFollower)
        => baseService.ExecuteAsync("Social.FollowList", async () =>
        {
            var target = await db.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetId, ct);
            if (target is null) return Result<FollowListResult>.NotFound("User not found.");
            if (!await CanViewAsync(viewerId, target, ct))
                return Result<FollowListResult>.Forbidden("This account is private.");

            return Result<FollowListResult>.Success(
                await PageFollowRowsAsync(viewerId, followQuery, otherIsFollower, cursor, ct));
        }, ct);

    /// <summary>
    /// Pages a filtered <see cref="Follow"/> query (newest id first) and loads the "other"
    /// user on each edge — <paramref name="otherIsFollower"/> picks which side that is.
    /// Keeps the keyset paging on raw entity columns so EF can translate it.
    /// </summary>
    private async Task<FollowListResult> PageFollowRowsAsync(
        long viewerId, IQueryable<Follow> followQuery, bool otherIsFollower,
        string? cursor, CancellationToken ct)
    {
        if (Paging.DecodeCursor(cursor) is long c) followQuery = followQuery.Where(f => f.Id < c);

        var page = await followQuery
            .OrderByDescending(f => f.Id)
            .Take(Paging.PageSize + 1)
            .Select(f => new { f.Id, OtherId = otherIsFollower ? f.FollowerId : f.FolloweeId })
            .ToListAsync(ct);

        string? next = null;
        if (page.Count > Paging.PageSize)
        {
            next = page[Paging.PageSize - 1].Id.ToString();
            page.RemoveAt(page.Count - 1);
        }

        var orderedIds   = page.Select(p => p.OtherId).ToList();
        var users        = await db.Set<User>().AsNoTracking()
            .Where(u => orderedIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);
        var followStates = await FollowStatesAsync(viewerId, orderedIds, ct);

        var items = orderedIds
            .Where(users.ContainsKey)
            .Select(uid =>
            {
                var u = users[uid];
                return new FollowUserItem(
                    u.Id, u.Username, u.DisplayName, u.AvatarUrl, u.IsVerified,
                    followStates.GetValueOrDefault(u.Id, FollowStates.None));
            })
            .ToList();

        return new FollowListResult(items, next);
    }

    /// <summary>Maps each target id to the viewer's follow-state ("following" / "requested"); absent = "none".</summary>
    private async Task<Dictionary<long, string>> FollowStatesAsync(
        long viewerId, IReadOnlyCollection<long> targetIds, CancellationToken ct)
    {
        if (targetIds.Count == 0) return [];
        var rows = await db.Set<Follow>().AsNoTracking()
            .Where(f => f.FollowerId == viewerId && targetIds.Contains(f.FolloweeId))
            .Select(f => new { f.FolloweeId, f.Status })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.FolloweeId, r => StateOf(r.Status));
    }

    private async Task<bool> CanViewAsync(long viewerId, User target, CancellationToken ct)
    {
        if (target.Id == viewerId || target.AccountType == AccountType.Public) return true;
        return await db.Set<Follow>().AsNoTracking().AnyAsync(
            f => f.FollowerId == viewerId && f.FolloweeId == target.Id && f.Status == FollowStatus.Accepted, ct);
    }

    /// <summary>
    /// Atomically shifts the denormalized follower/following counters for one accepted
    /// edge (<paramref name="delta"/> = +1 on follow/accept, -1 on unfollow). Uses
    /// <c>ExecuteUpdate</c> so concurrent follows of the same user don't collide on the
    /// row-version token; runs inside the caller's transaction.
    /// </summary>
    private async Task AdjustFollowCountsAsync(long followerId, long followeeId, int delta, CancellationToken ct)
    {
        await db.Set<User>().Where(u => u.Id == followeeId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.FollowersCount, u => u.FollowersCount + delta), ct);
        await db.Set<User>().Where(u => u.Id == followerId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.FollowingCount, u => u.FollowingCount + delta), ct);
    }

    private static string StateOf(FollowStatus s)
        => s == FollowStatus.Accepted ? FollowStates.Following : FollowStates.Requested;
}
