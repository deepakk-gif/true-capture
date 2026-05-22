using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

public sealed class MediaService(
    AppDbContext db,
    IBaseService baseService,
    IFileStorage fileStorage) : IMediaService
{
    private const long MaxPhotoBytes = 25L  * 1024 * 1024;   // 25 MB
    private const long MaxVideoBytes = 200L * 1024 * 1024;   // 200 MB
    private const string StorageFolder = "posts";
    private static readonly TimeSpan UploadTtl = TimeSpan.FromMinutes(15);

    // GIF and audio are intentionally absent — only still photos and video are allowed.
    private static readonly HashSet<string> PhotoMimes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
    private static readonly HashSet<string> VideoMimes =
        new(StringComparer.OrdinalIgnoreCase) { "video/mp4", "video/quicktime" };

    public Task<Result<UploadTicket>> RequestUploadAsync(long ownerId, RequestUploadDto dto, CancellationToken ct)
        => baseService.ExecuteAsync("Media.RequestUpload", async () =>
        {
            if (!TryParseKind(dto.Kind, out var kind))
                return Result<UploadTicket>.Validation(["Media kind must be 'photo' or 'video'."]);

            var allowed = kind == MediaKind.Photo ? PhotoMimes : VideoMimes;
            if (!allowed.Contains(dto.MimeType))
                return Result<UploadTicket>.Validation(
                    [$"'{dto.MimeType}' is not an allowed {dto.Kind} type. GIF and audio files are not supported."]);

            if (dto.ByteSize <= 0)
                return Result<UploadTicket>.Validation(["Upload size must be greater than zero."]);

            var cap = kind == MediaKind.Photo ? MaxPhotoBytes : MaxVideoBytes;
            if (dto.ByteSize > cap)
                return Result<UploadTicket>.PayloadTooLarge(
                    $"{dto.Kind} must be {cap / (1024 * 1024)} MB or smaller.");

            var slot = fileStorage.ReserveSlot(StorageFolder, dto.MimeType);
            var asset = new MediaAsset
            {
                OwnerId    = ownerId,
                Kind       = kind,
                Status     = MediaStatus.Pending,
                StorageKey = slot.Key,
                Url        = slot.Url,
                MimeType   = dto.MimeType,
                ByteSize   = dto.ByteSize,
            };
            db.Set<MediaAsset>().Add(asset);
            await db.SaveChangesAsync(ct);

            return Result<UploadTicket>.Success(new UploadTicket(
                asset.Id, $"/api/media/blob/{asset.Id}", DateTime.UtcNow.Add(UploadTtl)));
        }, ct);

    public Task<Result<bool>> StoreBlobAsync(long ownerId, long uploadId, Stream content, CancellationToken ct)
        => baseService.ExecuteAsync("Media.StoreBlob", async () =>
        {
            var asset = await db.Set<MediaAsset>().FirstOrDefaultAsync(a => a.Id == uploadId, ct);
            if (asset is null) return Result<bool>.NotFound("Upload not found.");
            if (asset.OwnerId != ownerId) return Result<bool>.Forbidden("This upload belongs to another user.");
            if (asset.Status != MediaStatus.Pending)
                return Result<bool>.Validation(["This upload has already been finalized."]);

            await fileStorage.WriteAsync(asset.StorageKey, content, ct);
            return Result<bool>.Success(true);
        }, ct);

    public Task<Result<MediaAssetDto>> FinalizeAsync(long ownerId, FinalizeUploadDto dto, CancellationToken ct)
        => baseService.ExecuteAsync("Media.Finalize", async () =>
        {
            var asset = await db.Set<MediaAsset>().FirstOrDefaultAsync(a => a.Id == dto.UploadId, ct);
            if (asset is null) return Result<MediaAssetDto>.NotFound("Upload not found.");
            if (asset.OwnerId != ownerId) return Result<MediaAssetDto>.Forbidden("This upload belongs to another user.");
            if (asset.Status == MediaStatus.Ready)
                return Result<MediaAssetDto>.Success(MediaAssetDto(asset));
            if (asset.Status == MediaStatus.Failed)
                return Result<MediaAssetDto>.Validation(["This upload failed processing."]);

            if (!await fileStorage.ExistsAsync(asset.StorageKey, ct))
            {
                asset.Status    = MediaStatus.Failed;
                asset.ErrorCode = "bytes_missing";
                await db.SaveChangesAsync(ct);
                return Result<MediaAssetDto>.Validation(["No uploaded bytes were found for this upload."]);
            }

            // MVP processing: photos serve their own bytes as the thumbnail; video
            // transcoding (HLS rungs + frame thumbnail) is a documented follow-up.
            asset.CaptureMetadata = string.IsNullOrWhiteSpace(dto.CaptureMetadata) ? null : dto.CaptureMetadata;
            asset.ThumbnailUrl    = asset.Kind == MediaKind.Photo ? asset.Url : null;
            asset.Status          = MediaStatus.Ready;
            await db.SaveChangesAsync(ct);

            return Result<MediaAssetDto>.Success(MediaAssetDto(asset));
        }, ct);

    private static bool TryParseKind(string? raw, out MediaKind kind)
    {
        switch (raw?.Trim().ToLowerInvariant())
        {
            case "photo": kind = MediaKind.Photo; return true;
            case "video": kind = MediaKind.Video; return true;
            default:      kind = MediaKind.Photo; return false;
        }
    }

    internal static MediaAssetDto MediaAssetDto(MediaAsset a) => new(
        a.Id,
        a.Kind == MediaKind.Video ? "video" : "photo",
        a.Status switch { MediaStatus.Ready => "ready", MediaStatus.Failed => "failed", _ => "pending" },
        a.Url, a.ThumbnailUrl, a.DurationSeconds, a.Width, a.Height);
}
