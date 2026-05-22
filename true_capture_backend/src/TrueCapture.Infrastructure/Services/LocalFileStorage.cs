using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Services;

/// <summary>
/// Stores uploads on the local filesystem; the API serves the root as static files
/// (wired in Program.cs via <c>UseFileStorage</c>). The returned <see cref="StoredFile.Url"/>
/// is <c>{PublicBaseUrl}{RequestPath}/{folder}/{file}</c> — root-relative when no
/// public base URL is set, so clients prepend their own API base.
///
/// This is the dev/default provider. For production swap in an S3-backed
/// <see cref="IFileStorage"/> — see <c>docs/file-storage.md</c>.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly StorageOptions             _opt;
    private readonly ILogger<LocalFileStorage>  _log;

    public LocalFileStorage(
        IOptions<StorageOptions>    options,
        IHostEnvironment            env,
        ILogger<LocalFileStorage>   log)
    {
        _opt = options.Value;
        _log = log;
        RootPath = Path.IsPathRooted(_opt.Local.RootPath)
            ? _opt.Local.RootPath
            : Path.Combine(env.ContentRootPath, _opt.Local.RootPath);
        Directory.CreateDirectory(RootPath);
    }

    /// <summary>Absolute filesystem root — Program.cs reads this to wire static-file serving.</summary>
    public string RootPath { get; }

    /// <summary>The URL path prefix (e.g. "/media") the root is served under.</summary>
    public string RequestPath => "/" + _opt.Local.RequestPath.Trim('/');

    public async Task<StoredFile> SaveAsync(
        Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ExtensionForContentType(contentType);

        var safeFolder = folder.Trim('/', '\\');
        var key        = $"{safeFolder}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath   = Path.Combine(RootPath, key.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            await content.CopyToAsync(fs, ct);

        var url = $"{_opt.PublicBaseUrl.TrimEnd('/')}{RequestPath}/{key}";
        _log.LogInformation("Stored file {Key}", key);
        return new StoredFile(url, key);
    }

    public StoredFile ReserveSlot(string folder, string contentType)
    {
        var ext        = ExtensionForContentType(contentType);
        var safeFolder = folder.Trim('/', '\\');
        var key        = $"{safeFolder}/{Guid.NewGuid():N}{ext}";
        var url        = $"{_opt.PublicBaseUrl.TrimEnd('/')}{RequestPath}/{key}";
        return new StoredFile(url, key);
    }

    public async Task WriteAsync(string key, Stream content, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(RootPath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fs, ct);
        _log.LogInformation("Wrote uploaded bytes to {Key}", key);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(RootPath, key.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteAsync(string? urlOrKey, CancellationToken ct = default)
    {
        var key = ToKey(urlOrKey);
        if (key is null) return Task.CompletedTask;

        var fullPath = Path.Combine(RootPath, key.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            // Best-effort: a failed cleanup must never fail the calling operation.
            _log.LogWarning(ex, "Failed to delete stored file {Key}", key);
        }
        return Task.CompletedTask;
    }

    /// <summary>Strips any base URL + request-path prefix to recover the root-relative storage key.</summary>
    private string? ToKey(string? urlOrKey)
    {
        if (string.IsNullOrWhiteSpace(urlOrKey)) return null;
        var prefix = RequestPath.Trim('/') + "/";
        var idx = urlOrKey.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) return urlOrKey[(idx + prefix.Length)..].TrimStart('/');
        // Already a bare "folder/file" key?
        return urlOrKey.Contains('/') && !urlOrKey.Contains("://") ? urlOrKey.TrimStart('/') : null;
    }

    private static string ExtensionForContentType(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg"      => ".jpg",
        "image/png"       => ".png",
        "image/webp"      => ".webp",
        "image/gif"       => ".gif",
        "video/mp4"       => ".mp4",
        "video/quicktime" => ".mov",
        _                 => ".bin",
    };
}
