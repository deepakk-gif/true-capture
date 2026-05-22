using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Messaging.Entities;
using TrueCapture.Modules.Messaging.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Messaging.Services;

public sealed class MessageService(
    AppDbContext  db,
    IBaseService  baseService,
    IChatNotifier notifier) : IMessageService
{
    private const int PageSize       = 30;
    private const int MaxTextLength  = 4000;

    public Task<Result<MessageListResult>> ListAsync(
        long userId, long conversationId, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Message.List", async () =>
        {
            if (!await IsParticipantAsync(userId, conversationId, ct))
                return Result<MessageListResult>.Forbidden("You are not part of this conversation.");

            var q = db.Set<Message>().AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.ReplyTo).ThenInclude(r => r!.Sender)
                .Include(m => m.Reactions)
                .Where(m => m.ConversationId == conversationId);

            if (long.TryParse(cursor, out var c)) q = q.Where(m => m.Id < c);

            // Newest-first page — the client renders bottom-to-top.
            var rows = await q.OrderByDescending(m => m.Id).Take(PageSize + 1).ToListAsync(ct);

            string? next = null;
            if (rows.Count > PageSize)
            {
                next = rows[PageSize - 1].Id.ToString();
                rows.RemoveAt(rows.Count - 1);
            }

            return Result<MessageListResult>.Success(new MessageListResult(
                rows.Select(m => MessageMapping.ToDto(m, userId)).ToList(), next));
        }, ct);

    public Task<Result<MessageDto>> SendAsync(
        long senderId, long conversationId, SendMessageRequest req, CancellationToken ct)
        => baseService.ExecuteAsync("Message.Send", async () =>
        {
            var participantIds = await ParticipantIdsAsync(conversationId, ct);
            if (!participantIds.Contains(senderId))
                return Result<MessageDto>.Forbidden("You are not part of this conversation.");

            if (!TryParseType(req.Type, out var type))
                return Result<MessageDto>.Validation(["Message type must be 'text', 'image' or 'video'."]);

            var text = req.Text?.Trim();
            if (type == MessageType.Text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return Result<MessageDto>.Validation(["A text message cannot be empty."]);
                if (text!.Length > MaxTextLength)
                    return Result<MessageDto>.Validation([$"A message must be {MaxTextLength} characters or fewer."]);
            }
            else if (string.IsNullOrWhiteSpace(req.MediaUrl))
            {
                return Result<MessageDto>.Validation(["A media URL is required for image / video messages."]);
            }

            if (req.ReplyToMessageId is long replyId)
            {
                var validReply = await db.Set<Message>().AsNoTracking()
                    .AnyAsync(m => m.Id == replyId && m.ConversationId == conversationId, ct);
                if (!validReply)
                    return Result<MessageDto>.Validation(["The message being replied to was not found."]);
            }

            var message = new Message
            {
                ConversationId   = conversationId,
                SenderId         = senderId,
                Type             = type,
                Text             = type == MessageType.Text ? text : null,
                MediaUrl         = type == MessageType.Text ? null : req.MediaUrl,
                ThumbnailUrl     = req.ThumbnailUrl,
                MediaWidth       = req.MediaWidth,
                MediaHeight      = req.MediaHeight,
                ReplyToMessageId = req.ReplyToMessageId,
            };
            db.Set<Message>().Add(message);
            await db.SaveChangesAsync(ct);

            var preview = MessageMapping.Preview(message);
            if (preview.Length > 280) preview = preview[..280];
            await db.Set<Conversation>().Where(c => c.Id == conversationId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.LastMessageAtUtc, message.CreatedAtUtc)
                    .SetProperty(c => c.LastMessagePreview, preview)
                    .SetProperty(c => c.LastMessageSenderId, senderId), ct);

            // Sender has implicitly read their own message; everyone else gains an unread.
            await db.Set<ConversationParticipant>()
                .Where(p => p.ConversationId == conversationId && p.UserId == senderId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.LastReadMessageId, message.Id)
                    .SetProperty(p => p.UnreadCount, 0), ct);
            await db.Set<ConversationParticipant>()
                .Where(p => p.ConversationId == conversationId && p.UserId != senderId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.UnreadCount, p => p.UnreadCount + 1), ct);

            var full = await LoadFullAsync(message.Id, ct);
            var dto  = MessageMapping.ToDto(full!, senderId);

            await notifier.BroadcastMessageAsync(participantIds, dto, ct);

            var senderName = await db.Set<User>().AsNoTracking()
                .Where(u => u.Id == senderId)
                .Select(u => u.DisplayName ?? u.Username)
                .FirstOrDefaultAsync(ct) ?? "New message";
            foreach (var rid in participantIds.Where(id => id != senderId))
                await notifier.PushMessageAsync(rid, conversationId, message.Id, senderName, preview, ct);

            return Result<MessageDto>.Success(dto);
        }, ct, useTransaction: true);

    public Task<Result<IReadOnlyList<ReactionDto>>> ReactAsync(
        long userId, long messageId, string? emoji, CancellationToken ct)
        => baseService.ExecuteAsync("Message.React", async () =>
        {
            var message = await db.Set<Message>().FirstOrDefaultAsync(m => m.Id == messageId, ct);
            if (message is null) return Result<IReadOnlyList<ReactionDto>>.NotFound("Message not found.");

            var participantIds = await ParticipantIdsAsync(message.ConversationId, ct);
            if (!participantIds.Contains(userId))
                return Result<IReadOnlyList<ReactionDto>>.Forbidden("You are not part of this conversation.");

            var existing = await db.Set<MessageReaction>()
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId, ct);
            var trimmed = emoji?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                if (existing is not null) db.Set<MessageReaction>().Remove(existing);
            }
            else if (existing is null)
            {
                db.Set<MessageReaction>().Add(
                    new MessageReaction { MessageId = messageId, UserId = userId, Emoji = trimmed });
            }
            else
            {
                existing.Emoji = trimmed;   // one reaction per user — replace it
            }
            await db.SaveChangesAsync(ct);

            var all = await db.Set<MessageReaction>().AsNoTracking()
                .Where(r => r.MessageId == messageId).ToListAsync(ct);
            var reactions = all
                .GroupBy(r => r.Emoji)
                .Select(g => new ReactionDto(g.Key, g.Count(), g.Any(r => r.UserId == userId)))
                .ToList();

            await notifier.NotifyReactionAsync(participantIds, message.ConversationId, messageId, reactions, ct);
            return Result<IReadOnlyList<ReactionDto>>.Success(reactions);
        }, ct, useTransaction: true);

    public Task<Result<bool>> DeleteAsync(long userId, long messageId, CancellationToken ct)
        => baseService.ExecuteAsync("Message.Delete", async () =>
        {
            var message = await db.Set<Message>().FirstOrDefaultAsync(m => m.Id == messageId, ct);
            if (message is null) return Result<bool>.NotFound("Message not found.");
            if (message.SenderId != userId)
                return Result<bool>.Forbidden("You can only delete your own messages.");

            message.IsDeleted = true;   // soft delete — global query filter hides it
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    private Task<bool> IsParticipantAsync(long userId, long conversationId, CancellationToken ct)
        => db.Set<ConversationParticipant>().AsNoTracking()
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId, ct);

    private async Task<List<long>> ParticipantIdsAsync(long conversationId, CancellationToken ct)
        => await db.Set<ConversationParticipant>().AsNoTracking()
            .Where(p => p.ConversationId == conversationId)
            .Select(p => p.UserId).ToListAsync(ct);

    private Task<Message?> LoadFullAsync(long messageId, CancellationToken ct)
        => db.Set<Message>().AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.ReplyTo).ThenInclude(r => r!.Sender)
            .Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

    private static bool TryParseType(string? raw, out MessageType type)
    {
        switch (raw?.Trim().ToLowerInvariant())
        {
            case "text":  type = MessageType.Text;  return true;
            case "image": type = MessageType.Image; return true;
            case "video": type = MessageType.Video; return true;
            default:      type = MessageType.Text;  return false;
        }
    }
}
