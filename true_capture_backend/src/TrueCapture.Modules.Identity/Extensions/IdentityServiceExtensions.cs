using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Infrastructure.Seeding;
using TrueCapture.Infrastructure.Services;
using TrueCapture.Modules.Identity.Infrastructure;
using TrueCapture.Modules.Identity.Seeds;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Extensions;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<JwtOptions>(cfg.GetSection("Jwt"));
        services.Configure<GoogleAuthOptions>(cfg.GetSection("Authentication:Google"));
        services.Configure<EmailOptions>(cfg.GetSection("Email"));
        services.Configure<FirebaseOptions>(cfg.GetSection("Firebase"));

        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IFcmSender, FirebaseFcmSender>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IUserDeviceService, UserDeviceService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IEntityModelConfigurator, IdentityModelConfigurator>();
        services.AddDataSeeder<IdentitySystemSeeder>();
        return services;
    }
}
