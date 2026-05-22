using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueCapture.Modules.Social.Services;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Social.Controllers;

/// <summary>Backs the mobile Home tab and Fake vs Real tab.</summary>
[Route("api/feed")]
[Authorize]
public sealed class FeedController(IFeedService feed) : BaseController
{
    /// <summary>
    /// `GET /api/feed?channel=&cursor=` — `channel=home` (default) or `channel=fake_vs_real`.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? channel, [FromQuery] string? cursor, CancellationToken ct = default)
        => Ok(await feed.GetAsync(CurrentUserId, channel, cursor, ct));
}
