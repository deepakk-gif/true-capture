using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueCapture.Modules.Users.Services;

namespace TrueCapture.Modules.Users.Extensions;

public static class UsersServiceExtensions
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddScoped<IAdminUsersService, AdminUsersService>();
        return services;
    }
}
