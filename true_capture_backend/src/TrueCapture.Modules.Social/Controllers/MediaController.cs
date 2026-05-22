using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Modules.Social.Services;
using TrueCapture.Shared.Constants;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Social.Controllers;

/// <summary>
/// Signed-URL media pipeline. The client: (1) `POST /api/media/uploads` to reserve a
/// slot, (2) `PUT` the raw bytes to the returned URL, (3) `POST /api/media/finalize`.
/// </summary>
[Route("api/media")]
[Authorize]
public sealed class MediaController(IMediaService media) : BaseController
{
    /// <summary>`POST /api/media/uploads` — reserve an upload slot.</summary>
    [HttpPost("uploads")]
    public async Task<IActionResult> RequestUpload(
        [FromBody] RequestUploadDto dto, CancellationToken ct = default)
        => Ok(await media.RequestUploadAsync(CurrentUserId, dto, ct));

    /// <summary>`PUT /api/media/blob/{uploadId}` — upload the raw bytes for a reserved slot.</summary>
    [HttpPut("blob/{uploadId:long}")]
    [DisableRequestSizeLimit]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<IActionResult> PutBlob(long uploadId, CancellationToken ct = default)
        => Ok(await media.StoreBlobAsync(CurrentUserId, uploadId, Request.Body, ct));

    /// <summary>`POST /api/media/finalize` — confirm the upload and make the asset usable.</summary>
    [HttpPost("finalize")]
    public async Task<IActionResult> Finalize(
        [FromBody] FinalizeUploadDto dto, CancellationToken ct = default)
        => Ok(await media.FinalizeAsync(CurrentUserId, dto, ct));
}
