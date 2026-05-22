using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

public sealed class FeedService(AppDbContext db, IBaseService baseService) : IFeedService
{
    public Task<Result<FeedResult>> GetAsync(
        long viewerId, string? channel, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Feed.Get", async () =>
        {
            var fakeVsReal = IsFakeVsRealChannel(channel);

            var q = db.Set<Post>().AsNoTracking()
                .Include(p => p.Media).ThenInclude(m => m.Media)
                .Include(p => p.References)
                .Where(p => p.Status == PostStatus.Live);

            if (fakeVsReal)
            {
                // Fake vs Real posts are always public — even from private accounts.
                q = q.Where(p => p.Type == PostType.FakeVsReal);
            }
            else
            {
                // Home: Normal posts from admins, the viewer, followed accounts, or public accounts.
                var followed = db.Set<Follow>().AsNoTracking()
                    .Where(f => f.FollowerId == viewerId && f.Status == FollowStatus.Accepted)
                    .Select(f => f.FolloweeId);
                q = q.Where(p => p.Type == PostType.Normal)
                     .Where(p => p.IsAdminPost
                              || p.AuthorId == viewerId
                              || followed.Contains(p.AuthorId)
                              || db.Set<User>().Any(u => u.Id == p.AuthorId
                                                         && u.AccountType == AccountType.Public));
            }

            if (Paging.DecodeCursor(cursor) is long c) q = q.Where(p => p.Id < c);

            var rows = await q.OrderByDescending(p => p.Id).Take(Paging.PageSize + 1).ToListAsync(ct);

            string? next = null;
            if (rows.Count > Paging.PageSize)
            {
                next = rows[Paging.PageSize - 1].Id.ToString();
                rows.RemoveAt(rows.Count - 1);
            }

            var items = new List<PostDto>(rows.Count);
            foreach (var p in rows)
                items.Add(await PostMapping.BuildAsync(db, viewerId, p, ct));

            return Result<FeedResult>.Success(new FeedResult(items, next));
        }, ct);

    private static bool IsFakeVsRealChannel(string? channel) =>
        channel?.Trim().ToLowerInvariant() is "fake_vs_real" or "fakevsreal" or "fvr";
}
