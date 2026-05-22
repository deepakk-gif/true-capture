using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Infrastructure.Extensions;
using TrueCapture.Infrastructure.Seeding;
using TrueCapture.Modules.Identity.Extensions;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Modules.Messaging.Extensions;
using TrueCapture.Modules.Notifications.Extensions;
using TrueCapture.Modules.Social.Extensions;
using TrueCapture.Modules.Users.Extensions;
using TrueCapture.Shared.Constants;

namespace TrueCapture.Api.Extensions;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddTrueCapture(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddInfrastructure(cfg);
        services.AddIdentityModule(cfg);
        services.AddUsersModule(cfg);
        services.AddNotificationsModule(cfg);
        services.AddSocialModule(cfg);
        services.AddMessagingModule(cfg);

        services.AddControllers();
        services.AddSignalR();
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo { Title = "True Capture API", Version = "v1" });
            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name        = "Authorization",
                Description = "JWT Authorization header. Example: \"Bearer {token}\"",
                In          = ParameterLocation.Header,
                Type        = SecuritySchemeType.ApiKey,
                Scheme      = "Bearer",
            });
            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                }] = []
            });
        });

        // JWT authentication
        var jwt = cfg.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                // Keep claims verbatim — without this the handler rewrites `sub`
                // to ClaimTypes.NameIdentifier, so `User.FindFirst("sub")` (used by
                // CurrentUserId / CurrentUser) returns null and resolves to user 0.
                opt.MapInboundClaims = false;
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwt.Issuer,
                    ValidAudience            = jwt.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jwt.SigningKey)
                            ? new string('x', 32)
                            : jwt.SigningKey)),
                    NameClaimType            = JwtClaims.Name,
                    RoleClaimType            = JwtClaims.Role,
                };

                // SignalR (WebSockets) can't send an Authorization header — accept the
                // JWT from the `access_token` query string for the chat hub.
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var token = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) &&
                            ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            ctx.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();

        // Rate limiting (spec §7)
        services.AddRateLimiter(opt =>
        {
            opt.AddFixedWindowLimiter(RateLimitPolicies.Auth,   c =>
            {
                c.Window      = TimeSpan.FromMinutes(1);
                c.PermitLimit = 5;
                c.QueueLimit  = 0;
            });
            opt.AddFixedWindowLimiter(RateLimitPolicies.Public, c =>
            {
                c.Window      = TimeSpan.FromMinutes(1);
                c.PermitLimit = 100;
                c.QueueLimit  = 0;
            });
            opt.AddFixedWindowLimiter(RateLimitPolicies.Upload, c =>
            {
                c.Window      = TimeSpan.FromMinutes(1);
                c.PermitLimit = 30;
                c.QueueLimit  = 0;
            });
            opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        services.AddCors(opt =>
        {
            opt.AddDefaultPolicy(p => p
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed(_ => true));
        });

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("appdb");

        return services;
    }

    public static async Task MigrateAndSeedAsync(this IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);

        var runner = scope.ServiceProvider.GetRequiredService<DataSeederRunner>();
        await runner.RunAsync(ct);
    }
}
