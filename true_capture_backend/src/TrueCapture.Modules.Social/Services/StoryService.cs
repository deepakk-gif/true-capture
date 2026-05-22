using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

public sealed class StoryService(
    AppDbContext         db,
    IBaseService         baseService,
    IFileStorage         fileStorage,
    INotificationService notifications) : IStoryService
{
    private const long MaxImageBytes  = 8 * 1024 * 1024;
    private const int  MaxCaptionLen  = 500;
    private const string StorageFolder = "stories";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    public Task<Result<StoryItem>> CreateAsync(long authorId, PostUpload image, string? caption, CancellationToken ct)
        => baseService.ExecuteAsync("Story.Create", async () =>
        {
            if (image.Length <= 0 || image.Length > MaxImageBytes)
                return Result<StoryItem>.Validation([$"Image must be 1 byte – {MaxImageBytes / (1024 * 1024)} MB."]);
            if (!AllowedContentTypes.Contains(image.ContentType))
                return Result<StoryItem>.Validation(["Image must be a JPEG, PNG, or WebP file."]);

            var trimmed = caption?.Trim();
            if (trimmed is { Length: > MaxCaptionLen })
                return Result<StoryItem>.Validation([$"Caption must be {MaxCaptionLen} characters or fewer."]);

            var stored = await fileStorage.SaveAsync(
                image.Content, image.FileName, image.ContentType, StorageFolder, ct);

            var story = new Story
            {
                AuthorId     = authorId,
                ImageUrl     = stored.Url,
                Caption      = string.IsNullOrWhiteSpace(trimmed) ? null : trimmed,
                ExpiresAtUtc = DateTime.UtcNow.Add(Lifetime),
            };
            db.Set<Story>().Add(story);

            // @mentions in the caption notify the mentioned users.
            var mentioned = Mentions.Extract(trimmed);
            if (mentioned.Count > 0)
            {
                var ids = await db.Set<User>().AsNoTracking()
                    .Where(u => mentioned.Contains(u.Username.ToLower()))
                    .Select(u => u.Id)
                    .ToListAsync(ct);
                foreach (var mid in ids)
                    await notifications.EmitAsync(mid, NotificationType.StoryMention, actorUserId: authorId, ct: ct);
            }

            await db.SaveChangesAsync(ct);

            return Result<StoryItem>.Success(new StoryItem(
                story.Id, story.AuthorId, story.ImageUrl, story.Caption, story.CreatedAtUtc, story.ExpiresAtUtc));
        }, ct, useTransaction: true);

    public Task<Result<StoryFeed>> GetFeedAsync(long viewerId, CancellationToken ct)
        => baseService.ExecuteAsync("Story.Feed", async () =>
        {
            var now = DateTime.UtcNow;
            var followingIds = db.Set<Follow>().AsNoTracking()
                .Where(f => f.FollowerId == viewerId && f.Status == FollowStatus.Accepted)
                .Select(f => f.FolloweeId);

            var rows = await db.Set<Story>().AsNoTracking()
                .Where(s => s.ExpiresAtUtc > now &&
                            (s.AuthorId == viewerId || followingIds.Contains(s.AuthorId)))
                .Join(db.Set<User>(), s => s.AuthorId, u => u.Id, (s, u) => new
                {
                    s.Id, s.AuthorId, s.ImageUrl, s.Caption, s.CreatedAtUtc, s.ExpiresAtUtc,
                    u.Username, u.DisplayName, u.AvatarUrl,
                })
                .ToListAsync(ct);

            var items = rows
                .GroupBy(r => r.AuthorId)
                .Select(g =>
                {
                    var head = g.First();
                    return new UserStories(
                        g.Key, head.Username, head.DisplayName, head.AvatarUrl,
                        g.OrderBy(r => r.Id)
                         .Select(r => new StoryItem(r.Id, r.AuthorId, r.ImageUrl, r.Caption,
                             r.CreatedAtUtc, r.ExpiresAtUtc))
                         .ToList());
                })
                // Viewer's own stories first, then most-recently-updated authors.
                .OrderByDescending(us => us.AuthorId == viewerId)
                .ThenByDescending(us => us.Stories.Max(s => s.CreatedAtUtc))
                .ToList();

            return Result<StoryFeed>.Success(new StoryFeed(items));
        }, ct);

    public Task<Result<bool>> DeleteAsync(long storyId, long userId, CancellationToken ct)
        => baseService.ExecuteAsync("Story.Delete", async () =>
        {
            var story = await db.Set<Story>().FirstOrDefaultAsync(s => s.Id == storyId, ct);
            if (story is null) return Result<bool>.NotFound("Story not found.");
            if (story.AuthorId != userId)
                return Result<bool>.Forbidden("You can only delete your own stories.");

            var imageUrl = story.ImageUrl;
            db.Set<Story>().Remove(story);
            await db.SaveChangesAsync(ct);
            await fileStorage.DeleteAsync(imageUrl, ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
}
