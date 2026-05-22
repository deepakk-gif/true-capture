using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Notifications.Infrastructure;
using TrueCapture.Modules.Notifications.Services;

namespace TrueCapture.Modules.Notifications.Extensions;

public static class NotificationsServiceExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddScoped<IAdminNotificationService,  AdminNotificationService>();
        services.AddScoped<IAdminUserMessagingService, AdminUserMessagingService>();
        services.AddSingleton<IEntityModelConfigurator, NotificationsModelConfigurator>();
        return services;
    }
}
