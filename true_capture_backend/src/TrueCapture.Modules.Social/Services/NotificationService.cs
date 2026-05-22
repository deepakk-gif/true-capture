using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

public sealed class NotificationService(
    AppDbContext       db,
    IBaseService       baseService,
    IUserDeviceService devices) : INotificationService
{
    private const int PageSize = 30;

    public async Task EmitAsync(
        long recipientUserId, NotificationType type, long? actorUserId = null,
        long? postId = null, string? text = null, CancellationToken ct = default)
    {
        if (actorUserId == recipientUserId) return;   // never notify yourself

        // Queue the row — the caller's SaveChangesAsync commits it within its own unit of work.
        db.Set<Notification>().Add(new Notification
        {
            RecipientUserId = recipientUserId,
            Type            = type,
            ActorUserId     = actorUserId,
            PostId          = postId,
            Text            = text,
        });

        var actorName = actorUserId is long aid
            ? await db.Set<User>().AsNoTracking()
                .Where(u => u.Id == aid)
                .Select(u => u.DisplayName ?? u.Username)
                .FirstOrDefaultAsync(ct)
            : null;

        // Best-effort push — PushToUserAsync never throws.
        await devices.PushToUserAsync(recipientUserId,
            new NotificationPayload(PushTitle(type), PushBody(type, actorName, text),
                new Dictionary<string, string> { ["type"] = "activity" }), ct);
    }

    public Task<Result<NotificationFeed>> GetFeedAsync(long userId, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Notification.Feed", async () =>
        {
            var q = db.Set<Notification>().AsNoTracking().Where(n => n.RecipientUserId == userId);
            if (long.TryParse(cursor, out var c)) q = q.Where(n => n.Id < c);

            var rows = await q.OrderByDescending(n => n.Id).Take(PageSize + 1)
                .Select(n => new
                {
                    n.Id, n.Type, n.ActorUserId, n.PostId, n.Text, n.IsRead, n.CreatedAtUtc,
                    ActorUsername    = n.Actor != null ? n.Actor.Username : null,
                    ActorDisplayName = n.Actor != null ? n.Actor.DisplayName : null,
                    ActorAvatarUrl   = n.Actor != null ? n.Actor.AvatarUrl : null,
                })
                .ToListAsync(ct);

            string? next = null;
            if (rows.Count > PageSize)
            {
                next = rows[PageSize - 1].Id.ToString();
                rows.RemoveAt(rows.Count - 1);
            }

            // Resolve post thumbnails in one query (PostId is a loose reference, no FK).
            var postIds = rows.Where(r => r.PostId != null).Select(r => r.PostId!.Value).Distinct().ToList();
            var postImages = postIds.Count == 0
                ? new Dictionary<long, string>()
                : await db.Set<Post>().AsNoTracking()
                    .Where(p => postIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.CoverUrl, ct);

            var items = rows.Select(r => new NotificationItem(
                r.Id, r.Type.ToString(), r.ActorUserId, r.ActorUsername, r.ActorDisplayName,
                r.ActorAvatarUrl, r.PostId,
                r.PostId != null ? postImages.GetValueOrDefault(r.PostId.Value) : null,
                r.Text, r.IsRead, r.CreatedAtUtc)).ToList();

            return Result<NotificationFeed>.Success(new NotificationFeed(items, next));
        }, ct);

    public Task<Result<NotificationUnreadResult>> UnreadCountAsync(long userId, CancellationToken ct)
        => baseService.ExecuteAsync("Notification.UnreadCount", async () =>
        {
            var count = await db.Set<Notification>()
                .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, ct);
            return Result<NotificationUnreadResult>.Success(new NotificationUnreadResult(count));
        }, ct);

    public Task<Result<bool>> MarkAllReadAsync(long userId, CancellationToken ct)
        => baseService.ExecuteAsync("Notification.MarkAllRead", async () =>
        {
            await db.Set<Notification>()
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
            return Result<bool>.Success(true);
        }, ct);

    private static string PushTitle(NotificationType type) => type switch
    {
        NotificationType.FollowRequest    => "New follow request",
        NotificationType.FollowAccepted   => "Follow request accepted",
        NotificationType.NewFollower      => "New follower",
        NotificationType.PostLiked        => "New like",
        NotificationType.Commented        => "New comment",
        NotificationType.Mentioned        => "You were mentioned",
        NotificationType.StoryMention     => "Mentioned in a story",
        NotificationType.AccountSuspended => "Account suspended",
        NotificationType.AdminNotice      => "Notice",
        _                                 => "Notification",
    };

    private static string PushBody(NotificationType type, string? actor, string? text)
    {
        var who = string.IsNullOrWhiteSpace(actor) ? "Someone" : actor;
        return type switch
        {
            NotificationType.FollowRequest    => $"{who} requested to follow you",
            NotificationType.FollowAccepted   => $"{who} accepted your follow request",
            NotificationType.NewFollower      => $"{who} started following you",
            NotificationType.PostLiked        => $"{who} liked your post",
            NotificationType.Commented        => $"{who} commented on your post",
            NotificationType.Mentioned        => $"{who} mentioned you in a comment",
            NotificationType.StoryMention     => $"{who} mentioned you in a story",
            NotificationType.AccountSuspended => text ?? "Your account has been suspended.",
            NotificationType.AdminNotice      => text ?? "You have a new notice.",
            _                                 => "You have a new notification.",
        };
    }
}
