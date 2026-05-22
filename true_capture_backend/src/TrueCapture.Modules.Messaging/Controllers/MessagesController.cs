using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Messaging.Models;
using TrueCapture.Modules.Messaging.Services;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Messaging.Controllers;

/// <summary>Per-message actions: emoji reactions and deletion.</summary>
[Route("api/messages")]
[Authorize]
public sealed class MessagesController(IMessageService messages) : BaseController
{
    /// <summary>`POST /api/messages/{id}/react` — set / replace / clear the caller's reaction.</summary>
    [HttpPost("{id:long}/react")]
    public async Task<IActionResult> React(
        long id, [FromBody] ReactRequest req, CancellationToken ct = default)
        => Ok(await messages.ReactAsync(CurrentUserId, id, req.Emoji, ct));

    /// <summary>`DELETE /api/messages/{id}` — delete a message the caller sent.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
        => Ok(await messages.DeleteAsync(CurrentUserId, id, ct));
}
