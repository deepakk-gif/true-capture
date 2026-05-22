using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using TrueCapture.Infrastructure.Services;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Extensions;

public static class StorageStartupExtensions
{
    /// <summary>
    /// Serves locally-stored uploads as static files at the configured request path
    /// (default <c>/media</c>). No-op when the storage provider is not "Local"
    /// (an S3 provider serves files itself / via CDN).
    /// </summary>
    public static WebApplication UseFileStorage(this WebApplication app)
    {
        var opt = app.Services.GetRequiredService<IOptions<StorageOptions>>().Value;
        if (!string.Equals(opt.Provider, "Local", StringComparison.OrdinalIgnoreCase))
            return app;

        var storage = (LocalFileStorage)app.Services.GetRequiredService<IFileStorage>();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(storage.RootPath),
            RequestPath  = new PathString(storage.RequestPath),
        });
        return app;
    }
}
