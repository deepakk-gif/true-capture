using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueCapture.Infrastructure.Authorization;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Infrastructure.Http;
using TrueCapture.Infrastructure.Seeding;
using TrueCapture.Infrastructure.Services;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration          cfg)
    {
        var connStr = cfg.GetConnectionString("AppDb")
            ?? throw new InvalidOperationException("ConnectionStrings:AppDb is not configured.");

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, opt) =>
        {
            opt.UseNpgsql(connStr);
            opt.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IBaseService, BaseService<AppDbContext>>();
        services.AddScoped<IErrorLogger,  ErrorLogger>();
        services.AddScoped<IAuditLogger,  AuditLogger>();
        services.AddScoped<ICacheService, CacheService>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, FeatureAuthorizationHandler>();

        services.AddScoped<DataSeederRunner>();
        services.AddSingleton<IThirdPartyApiLogger, LoggerThirdPartyApiLogger>();

        // Redis distributed cache (with in-memory fallback when not configured)
        var redis = cfg.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redis);
        else
            services.AddDistributedMemoryCache();

        return services;
    }
}
