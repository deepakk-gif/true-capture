using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Modules.Social.Services;
using TrueCapture.Shared.Constants;
using TrueCapture.Shared.Controllers;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Controllers;

[Route("api/stories")]
[Authorize]
public sealed class StoriesController(IStoryService stories) : BaseController
{
    /// <summary>`GET /api/stories` — active stories of the viewer + people they follow.</summary>
    [HttpGet]
    public async Task<IActionResult> Feed(CancellationToken ct = default)
        => Ok(await stories.GetFeedAsync(CurrentUserId, ct));

    /// <summary>`POST /api/stories` — post an image story (multipart: `file` + optional `caption`).</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<IActionResult> Create(
        [FromForm] IFormFile? file, [FromForm] string? caption, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return Ok(Result<StoryItem>.Validation(["No image was uploaded."]));

        await using var stream = file.OpenReadStream();
        return Ok(await stories.CreateAsync(
            CurrentUserId,
            new PostUpload(stream, file.FileName, file.ContentType, file.Length),
            caption, ct));
    }

    /// <summary>`DELETE /api/stories/{id}` — delete a story the caller authored.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
        => Ok(await stories.DeleteAsync(id, CurrentUserId, ct));
}
