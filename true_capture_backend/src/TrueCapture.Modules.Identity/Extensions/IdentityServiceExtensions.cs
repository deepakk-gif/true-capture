using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Infrastructure.Seeding;
using TrueCapture.Modules.Identity.Infrastructure;
using TrueCapture.Modules.Identity.Seeds;
using TrueCapture.Modules.Identity.Services;

namespace TrueCapture.Modules.Identity.Extensions;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<JwtOptions>(cfg.GetSection("Jwt"));
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IEntityModelConfigurator, IdentityModelConfigurator>();
        services.AddDataSeeder<IdentitySystemSeeder>();
        return services;
    }
}
