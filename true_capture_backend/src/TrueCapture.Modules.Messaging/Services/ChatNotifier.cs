using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Messaging.Hubs;
using TrueCapture.Modules.Messaging.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Messaging.Services;

public sealed class ChatNotifier(
    IHubContext<ChatHub, IChatClient> hub,
    AppDbContext                      db,
    IFcmSender                        fcm,
    ILogger<ChatNotifier>             log) : IChatNotifier
{
    public async Task BroadcastMessageAsync(
        IReadOnlyList<long> recipientUserIds, MessageDto message, CancellationToken ct)
    {
        try
        {
            foreach (var uid in recipientUserIds)
                await hub.Clients.Group(ChatHub.UserGroup(uid)).ReceiveMessage(message);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "SignalR ReceiveMessage broadcast failed");
        }
    }

    public async Task NotifyReadAsync(
        IReadOnlyList<long> recipientUserIds, long conversationId, long readerUserId,
        long lastReadMessageId, CancellationToken ct)
    {
        try
        {
            foreach (var uid in recipientUserIds)
                await hub.Clients.Group(ChatHub.UserGroup(uid))
                    .MessageRead(conversationId, readerUserId, lastReadMessageId);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "SignalR MessageRead broadcast failed");
        }
    }

    public async Task NotifyReactionAsync(
        IReadOnlyList<long> recipientUserIds, long conversationId, long messageId,
        IReadOnlyList<ReactionDto> reactions, CancellationToken ct)
    {
        try
        {
            foreach (var uid in recipientUserIds)
                await hub.Clients.Group(ChatHub.UserGroup(uid))
                    .ReactionUpdated(conversationId, messageId, reactions);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "SignalR ReactionUpdated broadcast failed");
        }
    }

    public async Task PushMessageAsync(
        long recipientUserId, long conversationId, long messageId,
        string senderName, string preview, CancellationToken ct)
    {
        try
        {
            var tokens = await db.Set<UserDevice>().AsNoTracking()
                .Where(d => d.UserId == recipientUserId)
                .Select(d => d.FcmToken)
                .ToListAsync(ct);
            if (tokens.Count == 0) return;

            var payload = new NotificationPayload(senderName, preview, new Dictionary<string, string>
            {
                ["type"]           = "message",
                ["conversationId"] = conversationId.ToString(),
                ["messageId"]      = messageId.ToString(),
                ["senderName"]     = senderName,
            });
            await fcm.SendToTokensAsync(tokens, payload, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "FCM message push failed for user {UserId}", recipientUserId);
        }
    }
}
