using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Messaging.Infrastructure;
using TrueCapture.Modules.Messaging.Services;

namespace TrueCapture.Modules.Messaging.Extensions;

public static class MessagingServiceExtensions
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IMessageService,      MessageService>();
        services.AddScoped<IChatNotifier,        ChatNotifier>();
        services.AddSingleton<IEntityModelConfigurator, MessagingModelConfigurator>();
        return services;
    }
}
