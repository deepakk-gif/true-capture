using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Messaging.Entities;
using TrueCapture.Modules.Messaging.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Messaging.Services;

public sealed class ConversationService(
    AppDbContext  db,
    IBaseService  baseService,
    IChatNotifier notifier) : IConversationService
{
    private const int PageSize  = 30;
    private const int MaxPinned = 3;

    public Task<Result<ConversationListResult>> ListAsync(long userId, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Conversation.List", async () =>
        {
            var q = db.Set<ConversationParticipant>().AsNoTracking()
                .Where(p => p.UserId == userId)
                .Join(db.Set<Conversation>(), p => p.ConversationId, c => c.Id,
                    (p, c) => new { Part = p, Conv = c });

            if (DateTime.TryParse(cursor, out var before))
                q = q.Where(x => x.Conv.LastMessageAtUtc < before.ToUniversalTime());

            var rows = await q.OrderByDescending(x => x.Conv.LastMessageAtUtc)
                .Take(PageSize + 1).ToListAsync(ct);

            string? next = null;
            if (rows.Count > PageSize)
            {
                next = rows[PageSize - 1].Conv.LastMessageAtUtc.ToString("o");
                rows.RemoveAt(rows.Count - 1);
            }

            // Resolve the other participant of each conversation in one query.
            var convIds = rows.Select(r => r.Conv.Id).ToList();
            var others = await db.Set<ConversationParticipant>().AsNoTracking()
                .Where(p => convIds.Contains(p.ConversationId) && p.UserId != userId)
                .Join(db.Set<User>(), p => p.UserId, u => u.Id,
                    (p, u) => new { p.ConversationId, User = u })
                .ToListAsync(ct);
            var otherByConv = others.ToDictionary(x => x.ConversationId, x => x.User);

            var items = rows
                .Where(r => otherByConv.ContainsKey(r.Conv.Id))
                .Select(r =>
                {
                    var u = otherByConv[r.Conv.Id];
                    return new ConversationDto(
                        r.Conv.Id,
                        new ChatUserDto(u.Id, u.Username, u.DisplayName, u.AvatarUrl, u.IsVerified),
                        r.Conv.LastMessagePreview, r.Conv.LastMessageAtUtc,
                        r.Conv.LastMessageSenderId, r.Part.IsPinned, r.Part.UnreadCount);
                })
                .ToList();

            return Result<ConversationListResult>.Success(new ConversationListResult(items, next));
        }, ct);

    public Task<Result<ConversationDto>> GetOrCreateDirectAsync(long userId, long otherUserId, CancellationToken ct)
        => baseService.ExecuteAsync("Conversation.GetOrCreate", async () =>
        {
            if (otherUserId == userId)
                return Result<ConversationDto>.Validation(["You cannot start a conversation with yourself."]);

            var other = await db.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == otherUserId, ct);
            if (other is null) return Result<ConversationDto>.NotFound("User not found.");

            var conv = await db.Set<Conversation>()
                .Where(c => c.Participants.Any(p => p.UserId == userId)
                            && c.Participants.Any(p => p.UserId == otherUserId))
                .FirstOrDefaultAsync(ct);

            bool isPinned = false;
            int unread = 0;
            if (conv is null)
            {
                conv = new Conversation { LastMessageAtUtc = DateTime.UtcNow };
                conv.Participants.Add(new ConversationParticipant { UserId = userId });
                conv.Participants.Add(new ConversationParticipant { UserId = otherUserId });
                db.Set<Conversation>().Add(conv);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                var mine = await db.Set<ConversationParticipant>().AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ConversationId == conv.Id && p.UserId == userId, ct);
                isPinned = mine?.IsPinned ?? false;
                unread = mine?.UnreadCount ?? 0;
            }

            return Result<ConversationDto>.Success(new ConversationDto(
                conv.Id,
                new ChatUserDto(other.Id, other.Username, other.DisplayName, other.AvatarUrl, other.IsVerified),
                conv.LastMessagePreview, conv.LastMessageAtUtc, conv.LastMessageSenderId,
                isPinned, unread));
        }, ct, useTransaction: true);

    public Task<Result<bool>> PinAsync(long userId, long conversationId, bool pinned, CancellationToken ct)
        => baseService.ExecuteAsync("Conversation.Pin", async () =>
        {
            var part = await db.Set<ConversationParticipant>()
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, ct);
            if (part is null) return Result<bool>.NotFound("Conversation not found.");

            if (pinned && !part.IsPinned)
            {
                var pinnedCount = await db.Set<ConversationParticipant>()
                    .CountAsync(p => p.UserId == userId && p.IsPinned, ct);
                if (pinnedCount >= MaxPinned)
                    return Result<bool>.Validation([$"You can pin at most {MaxPinned} chats."]);
            }

            part.IsPinned    = pinned;
            part.PinnedAtUtc = pinned ? DateTime.UtcNow : null;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<bool>> MarkReadAsync(long userId, long conversationId, long lastMessageId, CancellationToken ct)
        => baseService.ExecuteAsync("Conversation.MarkRead", async () =>
        {
            var part = await db.Set<ConversationParticipant>()
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, ct);
            if (part is null) return Result<bool>.NotFound("Conversation not found.");

            if (lastMessageId > (part.LastReadMessageId ?? 0))
                part.LastReadMessageId = lastMessageId;
            part.UnreadCount = 0;
            await db.SaveChangesAsync(ct);

            var others = await db.Set<ConversationParticipant>().AsNoTracking()
                .Where(p => p.ConversationId == conversationId && p.UserId != userId)
                .Select(p => p.UserId).ToListAsync(ct);
            await notifier.NotifyReadAsync(others, conversationId, userId,
                part.LastReadMessageId ?? lastMessageId, ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
}
