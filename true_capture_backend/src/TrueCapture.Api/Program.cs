using Serilog;
using TrueCapture.Api.Extensions;
using TrueCapture.Infrastructure.Extensions;
using TrueCapture.Modules.Messaging.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/true-capture-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddTrueCapture(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseFileStorage();   // serves uploaded files (avatars, ...) as static files at /media
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHealthChecks("/api/health");

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
    await app.Services.MigrateAndSeedAsync();

app.Run();

public partial class Program { }   // for WebApplicationFactory in integration tests
