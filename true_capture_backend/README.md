# True Capture — Backend

.NET 9 Web API following the modular monolith pattern in [`docs/specs/application_stracture.md`](docs/specs/application_stracture.md).

## Stack

- .NET 9 / ASP.NET Core
- PostgreSQL via Npgsql + EF Core 9
- Redis (distributed cache)
- JWT auth (15 min access / 30 day rotating refresh)
- Serilog, Swashbuckle, FluentValidation, BCrypt, Dapper

## Layout

```
true_capture_backend/
├── docs/specs/                      architecture spec
├── ops/docker-compose.yml           postgres, redis, mailhog
├── .github/workflows/ci.yml         build + tests
└── src/
    ├── TrueCapture.sln
    ├── TrueCapture.Core/            domain primitives, no NuGet deps
    ├── TrueCapture.Shared/          Result<T>, BaseEntity, BaseController, attributes
    ├── TrueCapture.Infrastructure/  AppDbContext, BaseService, EF interceptors, auth handlers
    ├── TrueCapture.Api/             host (Program.cs, middleware, DI composition)
    ├── TrueCapture.Modules.Identity/ auth, users, roles, permissions, refresh tokens
    ├── TrueCapture.Tests.Unit/      SQLite in-memory + xUnit + NSubstitute
    ├── TrueCapture.Tests.Integration/ Testcontainers Postgres + WebApplicationFactory
    └── TrueCapture.Tests.Arch/      NetArchTest rules
```

## Quickstart

```bash
# 1. Start infra
docker compose -f ops/docker-compose.yml up -d

# 2. Set a real signing key (>= 32 chars)
export Jwt__SigningKey="$(openssl rand -base64 48)"

# 3. Restore + run
dotnet restore src/TrueCapture.sln
dotnet run --project src/TrueCapture.Api
```

API listens at `http://localhost:5080`; Swagger at `/swagger`.

## EF migrations

```bash
dotnet ef migrations add 20260504_Identity_Initial \
  --project    src/TrueCapture.Infrastructure \
  --startup-project src/TrueCapture.Api \
  --context    AppDbContext \
  --output-dir Migrations
```

In `Development` with `RunMigrationsOnStartup: true`, the API auto-migrates and runs seeders on boot.

## Adding a module

Follow [§8 of the spec](docs/specs/application_stracture.md#8-adding-a-module--step-by-step). The Identity module is the canonical reference implementation.

## Testing

```bash
dotnet test src/TrueCapture.Tests.Arch        # structural rules
dotnet test src/TrueCapture.Tests.Unit        # SQLite, fast
dotnet test src/TrueCapture.Tests.Integration # Testcontainers Postgres (requires Docker)
```

## What's NOT implemented yet

The foundation, authn/authz, and Identity module are in place. Per the PRD, the following business modules still need to be added:

- User profiles + follow graph
- Posts (single photo, multi-photo carousel, video)
- Feed (followed-user posts ∪ admin posts)
- Fake-vs-Real (admin posts only)
- Engagement (like, comment, share, save, hashtag, mention)
- Media upload pipeline
- Notifications (FCM / APNS)
- Messaging (SignalR)
- Trust / AI detection bridge to the Python service
- CMS pages + Contact Us
- Admin moderation panel APIs
- Search

Add each as `TrueCapture.Modules.{Name}/` following the Identity module's structure.
