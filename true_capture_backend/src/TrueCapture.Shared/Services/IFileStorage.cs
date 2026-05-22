namespace TrueCapture.Shared.Services;

/// <summary>A file persisted by an <see cref="IFileStorage"/> implementation.</summary>
/// <param name="Url">
/// Public URL the file is served from. May be root-relative (e.g. "/media/avatars/x.jpg")
/// when no public base URL is configured — clients prepend their own API base in that case.
/// </param>
/// <param name="Key">Provider-specific key used to delete the file later.</param>
public sealed record StoredFile(string Url, string Key);

/// <summary>
/// Blob/file persistence abstraction. The shipped implementation is
/// <c>LocalFileStorage</c> (disk + static-file serving); production can swap in an
/// S3-backed implementation without touching callers — see <c>docs/file-storage.md</c>.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Persists <paramref name="content"/> under <paramref name="folder"/> and returns
    /// its public URL + storage key. The stored filename is randomised to avoid collisions.
    /// </summary>
    Task<StoredFile> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    /// <summary>
    /// Best-effort delete by the URL or key returned from <see cref="SaveAsync"/>.
    /// A null/blank/unrecognised value is a no-op — callers use this for cleanup and
    /// must not have it fail the surrounding operation.
    /// </summary>
    Task DeleteAsync(string? urlOrKey, CancellationToken ct = default);

    /// <summary>
    /// Reserves a storage slot (randomised key + public URL) for the signed-URL upload
    /// pipeline — no bytes are written yet. The client later PUTs the bytes, which the
    /// API persists via <see cref="WriteAsync"/>.
    /// </summary>
    StoredFile ReserveSlot(string folder, string contentType);

    /// <summary>Persists <paramref name="content"/> at a previously reserved <paramref name="key"/>.</summary>
    Task WriteAsync(string key, Stream content, CancellationToken ct = default);

    /// <summary>True once bytes exist at <paramref name="key"/> (used to verify a finalize call).</summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
