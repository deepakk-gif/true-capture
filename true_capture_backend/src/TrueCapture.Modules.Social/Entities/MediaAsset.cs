using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>Photo or video — the only media kinds allowed (GIF / audio are rejected).</summary>
public enum MediaKind
{
    Photo = 1,
    Video = 2,
}

/// <summary>
/// Lifecycle of an uploaded asset. A post is only public once every asset it
/// references is <see cref="Ready"/>.
/// </summary>
public enum MediaStatus
{
    Pending = 1,   // slot reserved / bytes may still be uploading
    Ready   = 2,   // bytes present, validated, usable in a post
    Failed  = 3,   // processing failed (see ErrorCode)
}

/// <summary>
/// One uploaded media asset produced by the signed-URL pipeline:
/// <c>POST /api/media/uploads</c> reserves the row + storage slot, the client PUTs
/// the bytes, then <c>POST /api/media/finalize</c> flips it to <see cref="MediaStatus.Ready"/>.
/// </summary>
public class MediaAsset : BaseEntity
{
    public long        OwnerId         { get; set; }
    public MediaKind   Kind            { get; set; }
    public MediaStatus Status          { get; set; } = MediaStatus.Pending;

    public string      StorageKey      { get; set; } = "";   // provider key, e.g. "posts/ab12.jpg"
    public string      Url             { get; set; } = "";   // public URL the bytes are served from
    public string?     ThumbnailUrl    { get; set; }

    public string      MimeType        { get; set; } = "";
    public long        ByteSize        { get; set; }
    public int?        DurationSeconds { get; set; }          // videos only
    public int?        Width           { get; set; }
    public int?        Height          { get; set; }

    /// <summary>Client capture-metadata JSON (device, timestamp, optional GPS) — stored verbatim.</summary>
    public string?     CaptureMetadata { get; set; }
    public string?     ErrorCode       { get; set; }
}
