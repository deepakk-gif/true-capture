# File Storage

Uploaded files (today: user avatars) go through the `IFileStorage` abstraction.
The shipped implementation writes to local disk; production should swap in an
S3-backed implementation. **No caller changes are needed** — controllers and
services depend only on `IFileStorage`.

## The abstraction

`TrueCapture.Shared/Services/IFileStorage.cs`:

```csharp
Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType,
                           string folder, CancellationToken ct = default);
Task DeleteAsync(string? urlOrKey, CancellationToken ct = default);

record StoredFile(string Url, string Key);
```

- `SaveAsync` persists the stream and returns the public `Url` (what gets stored
  on `User.AvatarUrl`) plus a `Key` for later deletion.
- `DeleteAsync` is best-effort — callers use it for cleanup and must not let it
  fail the surrounding request.

## Configuration (`appsettings.json#Storage`)

```jsonc
"Storage": {
  "Provider":      "Local",          // "Local" | "S3"
  "PublicBaseUrl": "",               // absolute base prepended to every URL; empty = root-relative
  "Local": {
    "RootPath":    "storage",        // disk folder (relative to ContentRoot, or absolute)
    "RequestPath": "/media"          // URL prefix the folder is served under
  },
  "S3": {
    "BucketName":    "",
    "Region":        "",
    "AccessKey":     "",
    "SecretKey":     "",
    "ServiceUrl":    "",             // set for S3-compatible stores (MinIO, Cloudflare R2); empty = AWS
    "PublicBaseUrl": ""              // CDN / bucket public URL objects are served from
  }
}
```

## Local provider (default)

`TrueCapture.Infrastructure/Services/LocalFileStorage.cs`:

- Writes to `{ContentRoot}/storage/{folder}/{guid}.{ext}`.
- `Program.cs` calls `app.UseFileStorage()` which serves `RootPath` as static
  files at `RequestPath` (`/media`). So an avatar saved as
  `storage/avatars/ab12.jpg` is reachable at `GET /media/avatars/ab12.jpg`.
- With `PublicBaseUrl` empty, `StoredFile.Url` is **root-relative**
  (`/media/avatars/ab12.jpg`); the mobile app (`AppConfig.resolveUrl`) and the
  admin panel (`resolveMediaUrl`) prepend the API base. Set `PublicBaseUrl` to
  an absolute origin if you prefer absolute URLs.
- The `storage/` folder is created on startup. Files there are **not** durable
  across container rebuilds — fine for dev, not for production. Add `storage/`
  to `.gitignore` / exclude it from deploy artifacts.

## Switching to S3 (production)

### 1. Add the AWS SDK to `TrueCapture.Infrastructure.csproj`

```xml
<PackageReference Include="AWSSDK.S3" Version="3.7.*" />
```

### 2. Add `S3FileStorage` next to `LocalFileStorage`

```csharp
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Services;

/// <summary>S3 (or S3-compatible) implementation of <see cref="IFileStorage"/>.</summary>
public sealed class S3FileStorage : IFileStorage
{
    private readonly S3StorageOptions _opt;
    private readonly IAmazonS3        _client;

    public S3FileStorage(IOptions<StorageOptions> options)
    {
        _opt = options.Value.S3;

        var config = new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(_opt.ServiceUrl))
        {
            config.ServiceURL    = _opt.ServiceUrl;   // MinIO / R2 / etc.
            config.ForcePathStyle = true;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_opt.Region);
        }

        _client = new AmazonS3Client(_opt.AccessKey, _opt.SecretKey, config);
    }

    public async Task<StoredFile> SaveAsync(
        Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName  = _opt.BucketName,
            Key         = key,
            InputStream = content,
            ContentType = contentType,
            // Bucket-policy-based public read is preferred over per-object ACLs.
        }, ct);

        var baseUrl = string.IsNullOrWhiteSpace(_opt.PublicBaseUrl)
            ? $"https://{_opt.BucketName}.s3.{_opt.Region}.amazonaws.com"
            : _opt.PublicBaseUrl.TrimEnd('/');

        return new StoredFile($"{baseUrl}/{key}", key);
    }

    public async Task DeleteAsync(string? urlOrKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(urlOrKey)) return;

        // Recover the bare key whether we were handed a key or a full URL.
        var key = urlOrKey.Contains("://")
            ? new Uri(urlOrKey).AbsolutePath.TrimStart('/')
            : urlOrKey.TrimStart('/');

        try
        {
            await _client.DeleteObjectAsync(_opt.BucketName, key, ct);
        }
        catch
        {
            // Best-effort — a failed cleanup must not fail the calling request.
        }
    }
}
```

### 3. Pick the provider in `InfrastructureExtensions.AddInfrastructure`

```csharp
services.Configure<StorageOptions>(cfg.GetSection(StorageOptions.SectionName));

var provider = cfg[$"{StorageOptions.SectionName}:Provider"];
if (string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase))
    services.AddSingleton<IFileStorage, S3FileStorage>();
else
    services.AddSingleton<IFileStorage, LocalFileStorage>();
```

### 4. Configure the bucket

- Set `Storage:Provider` to `S3` and fill the `Storage:S3` block (keep real
  secrets in environment variables / a secret store, not `appsettings.json`).
- The bucket must allow public read of uploaded objects (a bucket policy is
  cleaner than per-object ACLs), or front it with a CDN and set
  `Storage:S3:PublicBaseUrl` to the CDN origin.
- `UseFileStorage()` becomes a no-op for the S3 provider — S3/the CDN serves the
  files, the API does not.

No other code changes: `UserProfileService`, the controllers, the mobile app,
and the admin panel already work against whatever URL `SaveAsync` returns.
