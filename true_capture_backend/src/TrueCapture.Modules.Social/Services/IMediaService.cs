using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

/// <summary>
/// The signed-URL media pipeline: reserve a slot, accept the uploaded bytes, then
/// finalize the asset so it can be attached to a post.
/// </summary>
public interface IMediaService
{
    /// <summary>Validates the request, reserves a storage slot + pending row, and
    /// returns where the client should PUT the bytes.</summary>
    Task<Result<UploadTicket>> RequestUploadAsync(long ownerId, RequestUploadDto dto, CancellationToken ct = default);

    /// <summary>Persists the uploaded bytes for a reserved (pending) asset.</summary>
    Task<Result<bool>> StoreBlobAsync(long ownerId, long uploadId, Stream content, CancellationToken ct = default);

    /// <summary>Verifies the bytes are present and flips the asset to <c>ready</c>.</summary>
    Task<Result<MediaAssetDto>> FinalizeAsync(long ownerId, FinalizeUploadDto dto, CancellationToken ct = default);
}
