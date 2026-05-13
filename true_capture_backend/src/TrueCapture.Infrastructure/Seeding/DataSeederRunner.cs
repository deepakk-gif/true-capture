using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Seeding;

public sealed class DataSeederRunner(IServiceProvider sp, ILogger<DataSeederRunner> log)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<IDataSeeder>()
            .OrderBy(s => s.GetType().GetCustomAttributes(typeof(DataSeederOrderAttribute), inherit: false)
                .Cast<DataSeederOrderAttribute>().FirstOrDefault()?.Order ?? int.MaxValue)
            .ToList();

        foreach (var seeder in seeders)
        {
            log.LogInformation("Running seeder {Seeder}", seeder.GetType().Name);
            await seeder.SeedAsync(ct);
        }
    }
}

public static class DataSeederExtensions
{
    public static IServiceCollection AddDataSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class, IDataSeeder
    {
        services.AddScoped<IDataSeeder, TSeeder>();
        return services;
    }
}
