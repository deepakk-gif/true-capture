namespace TrueCapture.Infrastructure.Services;

/// <summary>Binds the <c>Storage</c> section of appsettings.json.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// "Local" (disk + static files) or "S3". Only "Local" ships an implementation
    /// today; see <c>docs/file-storage.md</c> for the S3 swap.
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Absolute base URL prepended to every stored file URL. Leave empty to return
    /// root-relative URLs ("/media/..."), which clients resolve against their API base.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "";

    public LocalStorageOptions Local { get; set; } = new();
    public S3StorageOptions    S3    { get; set; } = new();
}

public sealed class LocalStorageOptions
{
    /// <summary>Filesystem root for uploads. Relative paths resolve against ContentRootPath.</summary>
    public string RootPath { get; set; } = "storage";

    /// <summary>URL path prefix the root is served under. Must match the Program.cs static-file wiring.</summary>
    public string RequestPath { get; set; } = "/media";
}

public sealed class S3StorageOptions
{
    public string BucketName    { get; set; } = "";
    public string Region        { get; set; } = "";
    public string AccessKey     { get; set; } = "";
    public string SecretKey     { get; set; } = "";

    /// <summary>Override endpoint for S3-compatible stores (MinIO, R2, ...). Empty = real AWS S3.</summary>
    public string ServiceUrl    { get; set; } = "";

    /// <summary>Public/CDN base URL objects are served from. Empty = derive from bucket + region.</summary>
    public string PublicBaseUrl { get; set; } = "";
}
