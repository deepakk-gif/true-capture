using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Social.Infrastructure;
using TrueCapture.Modules.Social.Services;

namespace TrueCapture.Modules.Social.Extensions;

public static class SocialServiceExtensions
{
    public static IServiceCollection AddSocialModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<CreatorScoreOptions>(cfg.GetSection(CreatorScoreOptions.SectionName));

        services.AddScoped<ISocialService,          SocialService>();
        services.AddScoped<IPostService,            PostService>();
        services.AddScoped<INotificationService,    NotificationService>();
        services.AddScoped<IEngagementService,      EngagementService>();
        services.AddScoped<IStoryService,           StoryService>();
        services.AddScoped<IMediaService,           MediaService>();
        services.AddScoped<IFeedService,            FeedService>();
        services.AddScoped<ICreatorScoreService,    CreatorScoreService>();
        services.AddScoped<IPostModerationService,  PostModerationService>();
        services.AddSingleton<IEntityModelConfigurator, SocialModelConfigurator>();
        return services;
    }
}
