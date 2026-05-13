using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TrueCapture.Infrastructure.Data;

namespace TrueCapture.Tests.Integration;

public sealed class WebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("true_capture_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        // Migrate schema once at fixture startup.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _pg.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = _pg.GetConnectionString(),
                ["ConnectionStrings:Redis"] = "",   // fall back to in-memory
                ["Jwt:SigningKey"]          = new string('t', 64),
                ["RunMigrationsOnStartup"]  = "false",
            });
        });
    }

    public HttpClient CreateAuthenticatedClient(string[]? permissions = null)
    {
        // For real assertions you'd issue a JWT here using the same TokenService.
        // Stub: the test should call /api/auth/login first or attach a manually-signed token.
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
        return client;
    }
}
