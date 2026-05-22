using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Messaging.Models;
using TrueCapture.Modules.Messaging.Services;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Messaging.Controllers;

/// <summary>1-to-1 conversations: list, start, message history, read state and pinning.</summary>
[Route("api/conversations")]
[Authorize]
public sealed class ConversationsController(
    IConversationService conversations,
    IMessageService      messages) : BaseController
{
    /// <summary>`GET /api/conversations?cursor=` — the caller's conversation list.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await conversations.ListAsync(CurrentUserId, cursor, ct));

    /// <summary>`POST /api/conversations` — get-or-create the direct chat with a user.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateConversationRequest req, CancellationToken ct = default)
        => Ok(await conversations.GetOrCreateDirectAsync(CurrentUserId, req.UserId, ct));

    /// <summary>`GET /api/conversations/{id}/messages?cursor=` — newest-first message page.</summary>
    [HttpGet("{id:long}/messages")]
    public async Task<IActionResult> Messages(
        long id, [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await messages.ListAsync(CurrentUserId, id, cursor, ct));

    /// <summary>`POST /api/conversations/{id}/messages` — send a text / image / video message.</summary>
    [HttpPost("{id:long}/messages")]
    public async Task<IActionResult> Send(
        long id, [FromBody] SendMessageRequest req, CancellationToken ct = default)
        => Ok(await messages.SendAsync(CurrentUserId, id, req, ct));

    /// <summary>`POST /api/conversations/{id}/read` — mark read up to a message id.</summary>
    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(
        long id, [FromBody] MarkReadRequest req, CancellationToken ct = default)
        => Ok(await conversations.MarkReadAsync(CurrentUserId, id, req.LastMessageId, ct));

    /// <summary>`POST /api/conversations/{id}/pin` — pin / unpin (max 3 pinned).</summary>
    [HttpPost("{id:long}/pin")]
    public async Task<IActionResult> Pin(
        long id, [FromBody] PinRequest req, CancellationToken ct = default)
        => Ok(await conversations.PinAsync(CurrentUserId, id, req.Pinned, ct));
}
