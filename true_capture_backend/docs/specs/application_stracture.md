# Generic API Architecture — Scalable Modular Monolith

> **Stack:** .NET 8 · Angular 18 · SQL Server · Redis  
> **Pattern:** Modular Monolith → extract to microservices only when a specific module needs independent scaling  
> **Audience:** Any new SaaS, internal tool, admin panel, or B2B platform

---

## Table of Contents

1. [Philosophy & Guiding Principles](#1-philosophy--guiding-principles)
2. [Solution Layout](#2-solution-layout)
3. [Project Responsibilities](#3-project-responsibilities)
4. [Core Patterns](#4-core-patterns)
   - 4.1 Result\<T\> — No Exceptions for Business Logic
   - 4.2 BaseService / ExecuteAsync — Every Write Goes Through Here
   - 4.3 BaseEntity & Soft Delete
   - 4.4 BaseController — Consistent HTTP Mapping
5. [Database Strategy](#5-database-strategy)
6. [Authentication & Authorization](#6-authentication--authorization)
7. [Cross-Cutting Concerns](#7-cross-cutting-concerns)
8. [Adding a Module — Step-by-Step](#8-adding-a-module--step-by-step)
9. [Frontend Architecture (Angular)](#9-frontend-architecture-angular)
10. [Testing Pyramid](#10-testing-pyramid)
11. [CI/CD Skeleton](#11-cicd-skeleton)
12. [Project Bootstrap Checklist](#12-project-bootstrap-checklist)
13. [Decision Log](#13-decision-log)

---

## 1. Philosophy & Guiding Principles

| Principle | What it means in practice |
|-----------|--------------------------|
| **Boring technology wins** | Pick the most mainstream tool for the job. Creativity belongs in the product, not the stack. |
| **Explicit over clever** | A 20-line method that anyone can read beats a 5-line abstraction that requires mental archaeology. |
| **One obvious place for everything** | A new developer should never ask "where does this live?" — the structure answers that. |
| **Fail fast, fail loudly** | Return a typed `Result<T>` from every service method. Never swallow exceptions. Never return `null` to signal failure. |
| **The monolith is the right default** | Start as a single deployable. Add database-level boundaries. Extract a service only when a specific module needs its own release cadence or scaling profile. |
| **Tests are not optional** | A feature is not done until the unit tests and at least one integration test exist. Architecture tests enforce structural rules automatically. |

---

## 2. Solution Layout

```
MySolution/
├── docs/
│   ├── ARCHITECTURE.md          ← this file
│   └── decisions/               ADRs — one file per non-obvious decision
│
├── ops/
│   ├── docker-compose.yml       SQL Server, Redis, MailHog (local only)
│   └── .github/workflows/
│       ├── ci.yml
│       └── deploy.yml
│
└── src/
    ├── MySolution.sln
    │
    ├── MySolution.Core/          Domain contracts (enums, value objects, events)
    ├── MySolution.Shared/        Cross-cutting framework (Result, BaseEntity, interfaces)
    ├── MySolution.Infrastructure/ EF contexts, migrations, DI wiring, third-party adapters
    ├── MySolution.Api/           Host project (Program.cs, middleware, DI composition)
    │
    ├── MySolution.Modules.Identity/    Auth, users, roles, permissions
    ├── MySolution.Modules.{Domain1}/   First business domain
    ├── MySolution.Modules.{Domain2}/   Second business domain
    │   └── ...
    │
    ├── MySolution.Tests.Unit/
    ├── MySolution.Tests.Integration/
    └── MySolution.Tests.Arch/
```

### Dependency Graph (enforce via arch tests — strictly one-way)

```
MySolution.Api
    ├── MySolution.Infrastructure
    └── MySolution.Modules.*
            └── MySolution.Shared
                    └── MySolution.Core
```

**Modules never reference each other.** Cross-module contracts are defined as interfaces in `Shared` and injected at the `Api` level.

---

## 3. Project Responsibilities

### `MySolution.Core`
- Enums, value objects, domain events
- Zero NuGet dependencies — pure C# only
- Example contents: `OrderStatus.cs`, `Money.cs`, `DomainEvent.cs`

### `MySolution.Shared`
- `BaseEntity.cs`, `TenantEntity.cs`
- `Result.cs`, `Result<T>.cs`, `ResultExtensions.cs`
- `BaseController.cs`
- `IBaseService.cs`, `IErrorLogger.cs`, `IDataSeeder.cs`
- `RequirePermissionAttribute.cs`, `RequireFeatureAttribute.cs`, `RequireCaptchaAttribute.cs`
- `PagedResult<T>.cs`
- Shared message constants (`Errors.cs`, `Constants.cs`)

### `MySolution.Infrastructure`
- `AppDbContext.cs` (single DB) or multiple DbContexts for larger apps
- EF migrations
- `BaseService<TDbContext>.cs` — the `ExecuteAsync` implementation
- `ErrorLogger.cs`, `AuditLogger.cs`
- Redis cache helper
- Third-party API base client
- `IEntityModelConfigurator` — allows modules to plug EF config without coupling to Infrastructure
- Startup extensions: `AddInfrastructure(this IServiceCollection services, IConfiguration cfg)`

### `MySolution.Api`
- `Program.cs`
- Middleware pipeline (`UseXxx` calls)
- DI composition: calls `services.AddXxxModule()` for every module
- Health check endpoint
- Swagger/OpenAPI setup
- Background job registration (Hangfire, Quartz, etc.)

### `MySolution.Modules.{Domain}`
```
MySolution.Modules.Orders/
├── Controllers/
│   └── OrdersController.cs
├── Extensions/
│   └── OrdersServiceExtensions.cs       registers services, seeder, model configurator
├── Infrastructure/
│   └── OrdersModelConfigurator.cs       IEntityModelConfigurator implementation
├── Seeds/
│   └── OrdersSystemSeeder.cs
└── Services/
    ├── IOrderService.cs                 interface + request/response records
    └── OrderService.cs                 implementation
```

### `MySolution.Tests.Unit`
- One test class per service
- SQLite in-memory EF (no Docker required for unit tests)
- NSubstitute for mocking dependencies

### `MySolution.Tests.Integration`
- Testcontainers (real SQL Server in Docker)
- `WebApplicationFactory` for endpoint tests
- Tests run against real DB — never `UseInMemoryDatabase`

### `MySolution.Tests.Arch`
- NetArchTest rules
- Enforces: dependency graph, naming conventions, `ExecuteAsync` usage, authorization attributes

---

## 4. Core Patterns

### 4.1 Result\<T\> — No Exceptions for Business Logic

Every service method returns `Result<T>`. Exceptions are for unexpected failures only.

```csharp
// MySolution.Shared/Services/Result.cs

public class Result
{
    public bool IsSuccess { get; protected init; }
    public HttpStatusCode StatusCode { get; protected init; }
    public IReadOnlyList<string> Errors { get; protected init; } = [];

    public static Result Success()                             => new() { IsSuccess = true,  StatusCode = HttpStatusCode.OK };
    public static Result NotFound(string msg)                  => new() { IsSuccess = false, StatusCode = HttpStatusCode.NotFound,              Errors = [msg] };
    public static Result Conflict(string msg)                  => new() { IsSuccess = false, StatusCode = HttpStatusCode.Conflict,              Errors = [msg] };
    public static Result Validation(IEnumerable<string> errs)  => new() { IsSuccess = false, StatusCode = HttpStatusCode.UnprocessableEntity,   Errors = [..errs] };
    public static Result Forbidden(string? msg = null)         => new() { IsSuccess = false, StatusCode = HttpStatusCode.Forbidden,             Errors = [msg ?? "Access denied."] };
    public static Result Failure(string msg)                   => new() { IsSuccess = false, StatusCode = HttpStatusCode.InternalServerError,   Errors = [msg] };
}

public sealed class Result<T> : Result
{
    public T? Value { get; private init; }

    public static Result<T> Success(T value)                   => new() { IsSuccess = true,  StatusCode = HttpStatusCode.OK,                   Value = value };
    public new static Result<T> NotFound(string msg)           => new() { IsSuccess = false, StatusCode = HttpStatusCode.NotFound,              Errors = [msg] };
    public new static Result<T> Conflict(string msg)           => new() { IsSuccess = false, StatusCode = HttpStatusCode.Conflict,              Errors = [msg] };
    public new static Result<T> Validation(IEnumerable<string> errs) => new() { IsSuccess = false, StatusCode = HttpStatusCode.UnprocessableEntity, Errors = [..errs] };
    public new static Result<T> Forbidden(string? msg = null)  => new() { IsSuccess = false, StatusCode = HttpStatusCode.Forbidden,             Errors = [msg ?? "Access denied."] };
    public new static Result<T> Failure(string msg)            => new() { IsSuccess = false, StatusCode = HttpStatusCode.InternalServerError,   Errors = [msg] };
}
```

```csharp
// MySolution.Shared/Services/ResultExtensions.cs

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess) return new OkResult();
        return result.StatusCode switch
        {
            HttpStatusCode.NotFound            => new NotFoundObjectResult(new { errors = result.Errors }),
            HttpStatusCode.Conflict            => new ConflictObjectResult(new { errors = result.Errors }),
            HttpStatusCode.UnprocessableEntity => new UnprocessableEntityObjectResult(new { errors = result.Errors }),
            HttpStatusCode.Forbidden           => new ForbidResult(),
            _                                  => new ObjectResult(new { errors = result.Errors }) { StatusCode = (int)result.StatusCode },
        };
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess) return new OkObjectResult(result.Value);
        return ((Result)result).ToActionResult();
    }
}
```

**Usage in a service:**
```csharp
public async Task<Result<long>> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct)
{
    if (await _db.Orders.AnyAsync(o => o.Reference == dto.Reference, ct))
        return Result<long>.Conflict($"Order reference '{dto.Reference}' already exists.");

    // ... create order ...
    return Result<long>.Success(order.Id);
}
```

---

### 4.2 BaseService / ExecuteAsync — Every Write Goes Through Here

```csharp
// MySolution.Infrastructure/Services/BaseService.cs

public class BaseService<TDbContext>(TDbContext db, IErrorLogger errorLogger) : IBaseService
    where TDbContext : DbContext
{
    protected readonly TDbContext _db = db;

    public async Task<Result<T>> ExecuteAsync<T>(
        string operationName,
        Func<Task<Result<T>>> operation,
        CancellationToken ct,
        bool useTransaction = false)
    {
        IDbContextTransaction? tx = null;
        try
        {
            if (useTransaction)
                tx = await _db.Database.BeginTransactionAsync(ct);

            var result = await operation();

            if (tx is not null)
            {
                if (result.IsSuccess) await tx.CommitAsync(ct);
                else                  await tx.RollbackAsync(ct);
            }
            return result;
        }
        catch (OperationCanceledException)
        {
            await RollbackAsync(tx);
            return Result<T>.Failure("Operation was cancelled.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await RollbackAsync(tx);
            return Result<T>.Conflict($"Concurrency conflict in '{operationName}'.");
        }
        catch (ValidationException ex)
        {
            await RollbackAsync(tx);
            return Result<T>.Validation(ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (Exception ex)
        {
            await RollbackAsync(tx);
            await errorLogger.LogAsync(operationName, ex);
            return Result<T>.Failure($"Unexpected error in '{operationName}'.");
        }
        finally { if (tx is not null) await tx.DisposeAsync(); }
    }

    private static async Task RollbackAsync(IDbContextTransaction? tx)
    {
        if (tx is null) return;
        try { await tx.RollbackAsync(); } catch { /* best-effort */ }
    }
}
```

**Rule:** The arch test `SaveChangesAsync_MustBeInsideExecuteAsync` fails the build if `_db.SaveChangesAsync()` appears outside `ExecuteAsync`. No exceptions.

**Service usage template:**
```csharp
public class OrderService(AppDbContext db, IBaseService baseService) : IOrderService
{
    public async Task<Result<long>> CreateAsync(CreateOrderDto dto, CancellationToken ct)
    {
        return await baseService.ExecuteAsync("Orders.Create", async () =>
        {
            var order = new Order { Reference = dto.Reference, ... };
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(order.Id);
        }, ct, useTransaction: true);
    }
}
```

---

### 4.3 BaseEntity & Soft Delete

```csharp
// MySolution.Shared/Data/BaseEntity.cs

public abstract class BaseEntity
{
    public long     Id               { get; set; }
    public DateTime CreatedAtUtc     { get; set; }
    public long?    CreatedByUserId  { get; set; }
    public DateTime? UpdatedAtUtc    { get; set; }
    public long?    UpdatedByUserId  { get; set; }
    public bool     IsDeleted        { get; set; }
    public DateTime? DeletedAtUtc    { get; set; }
    public long?    DeletedByUserId  { get; set; }
    public byte[]   RowVersion       { get; set; } = [];
}
```

For multi-tenant projects, add:
```csharp
// MySolution.Shared/Data/TenantEntity.cs
public abstract class TenantEntity : BaseEntity
{
    public long TenantId { get; set; }   // or ShopId, OrganizationId, etc.
}
```

**EF global filter (applied in `OnModelCreating` for every entity that extends `TenantEntity`):**
```csharp
// For every tenant entity automatically:
modelBuilder.Entity<T>()
    .HasQueryFilter(e => e.TenantId == _tenantContext.TenantId && !e.IsDeleted);
```

**EF config template for any entity:**
```csharp
b.ToTable("Order", schema: "orders");
b.HasKey(e => e.Id);
b.Property(e => e.RowVersion).IsConcurrencyToken();   // IsConcurrencyToken, NOT IsRowVersion — keeps SQLite tests working
b.Property(e => e.Status).HasConversion<string>().HasMaxLength(30).IsRequired();  // all enums stored as strings
b.HasIndex(e => e.TenantId);
b.HasIndex(e => new { e.TenantId, e.Reference }).IsUnique();
```

---

### 4.4 BaseController — Consistent HTTP Mapping

```csharp
// MySolution.Shared/Controllers/BaseController.cs

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected long CurrentUserId =>
        long.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : 0;

    protected long CurrentTenantId =>
        long.TryParse(User.FindFirst("tenant_id")?.Value, out var id) ? id : 0;

    // Auto-maps Result<T>.StatusCode → HTTP status code
    protected IActionResult Ok<T>(Result<T> result) => result.ToActionResult();
    protected IActionResult Ok(Result result)        => result.ToActionResult();
}
```

**Controller template:**
```csharp
[Route("api/orders")]
[Authorize]
public class OrdersController(IOrderService svc) : BaseController
{
    [HttpGet]
    [RequirePermission("Orders.View")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
        => Ok(await svc.ListAsync(page, size, ct));

    [HttpGet("{id:long}")]
    [RequirePermission("Orders.View")]
    public async Task<IActionResult> Get(long id, CancellationToken ct = default)
        => Ok(await svc.GetAsync(id, ct));

    [HttpPost]
    [RequirePermission("Orders.Create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderDto dto,
        CancellationToken ct = default)
        => Ok(await svc.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [RequirePermission("Orders.Edit")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateOrderDto dto,
        CancellationToken ct = default)
        => Ok(await svc.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:long}")]
    [RequirePermission("Orders.Delete")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
        => Ok(await svc.DeleteAsync(id, ct));
}
```

---

## 5. Database Strategy

### Single-DB (Default — Start Here)

One `AppDbContext`, one database, all modules share it. Every module registers its EF entities via `IEntityModelConfigurator`.

```
AppDb
├── schema: identity   (users, roles, permissions, tokens)
├── schema: orders     (orders, order_lines)
├── schema: catalog    (products, categories)
├── schema: billing    (invoices, payments)
└── schema: audit      (audit_log, error_log)
```

### Separate Log DB (Extract When Writes Get Noisy)

Move `AuditLog`, `ErrorLog`, `ThirdPartyApiLog` to a dedicated `LogDb` early. Logging writes should never contend with business writes.

### Multi-Tenant Strategy

Choose one per project:

| Strategy | When to use |
|----------|------------|
| **Row-level** (column `TenantId` + global query filter) | < 1,000 tenants, standard tier, simplest to operate |
| **Schema-per-tenant** (`SET search_path`) | Regulatory isolation without separate servers |
| **Database-per-tenant** (runtime connection string resolution) | Enterprise tier, strict data isolation requirement, highest cost |

For row-level isolation — the default:

```csharp
// Infrastructure/MultiTenant/ITenantContext.cs
public interface ITenantContext
{
    long TenantId { get; }
}

// Infrastructure/MultiTenant/RequestTenantContext.cs
public class RequestTenantContext : ITenantContext
{
    private long _tenantId;
    public long TenantId => _tenantId;
    public void Set(long id) => _tenantId = id;
}

// Middleware sets it on every request from the JWT claim:
app.Use(async (ctx, next) =>
{
    var tenantCtx = ctx.RequestServices.GetRequiredService<RequestTenantContext>();
    if (long.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var id))
        tenantCtx.Set(id);
    await next();
});
```

### EF Core vs Dapper Decision Matrix

| Use | When |
|-----|------|
| **EF Core** | CRUD, simple lists, anything that needs tenant-filter, most writes |
| **Dapper** | Reports and dashboards (3+ joined tables, aggregates, window functions), stored procedures, bulk operations > 100 rows, reads from a separate read DB |

Dapper inside a transaction (share EF's connection):
```csharp
// Inside ExecuteAsync with useTransaction: true
var results = await _db.Database.GetDbConnection()
    .QueryAsync<ReportRow>(sql, parameters, transaction: _db.Database.CurrentTransaction!.GetDbTransaction());
```

---

## 6. Authentication & Authorization

### JWT Structure

```json
{
  "sub":         "42",
  "tenant_id":   "7",
  "name":        "Jane Smith",
  "email":       "jane@acme.com",
  "perms":       "Orders.View,Orders.Create,Catalog.View",
  "feats":       "Billing.Recurring,Reports.Advanced",
  "role":        "Manager",
  "iat":         1714900000,
  "exp":         1714900900
}
```

| Claim | Purpose |
|-------|---------|
| `sub` | User ID |
| `tenant_id` | Tenant/organisation ID — used to set `ITenantContext` |
| `perms` | Comma-separated permission codes — evaluated by `[RequirePermission]` |
| `feats` | Feature flags active for this tenant's plan — evaluated by `[RequireFeature]` |

**Token lifetimes:**
- Access token: **15 minutes**
- Refresh token: **30 days**, rotating on use, stored server-side (DB or Redis)

### Permission Attribute

```csharp
// MySolution.Shared/Authorization/RequirePermissionAttribute.cs

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirement
{
    public const string PolicyPrefix = "Permission:";
    public string PermissionCode { get; }

    public RequirePermissionAttribute(string code) : base(PolicyPrefix + code)
        => PermissionCode = code;
}
```

Dynamic policy registration in `Program.cs`:
```csharp
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("default", p => p.RequireAuthenticatedUser());
});

// Intercept any policy starting with "Permission:" and evaluate against JWT 'perms' claim
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

### Feature Gate Attribute

```csharp
[RequireFeature("Reports.Advanced")]
public async Task<IActionResult> AdvancedReport(...) { ... }
```

Evaluated against the `feats` JWT claim. Returns `402 Payment Required` when the feature is not in the tenant's plan.

### Password Security

```csharp
// Always BCrypt, cost factor 12 (minimum)
var hash   = BCrypt.Net.BCrypt.HashPassword(plainText, workFactor: 12);
var valid  = BCrypt.Net.BCrypt.Verify(plainText, hash);
```

Never MD5, SHA-256(password), or anything reversible.

### CAPTCHA on Public Endpoints

Every endpoint that accepts unauthenticated input (login, register, forgot-password, OTP) must carry `[RequireCaptcha]`. The arch test `PublicAuthEndpoints_RequireCaptcha` enforces this.

```csharp
[HttpPost("login")]
[AllowAnonymous]
[RequireCaptcha]                  // ← mandatory on every public auth surface
public async Task<IActionResult> Login([FromBody] LoginRequest req, ...) { ... }
```

---

## 7. Cross-Cutting Concerns

### Error Logging

```csharp
// MySolution.Shared/Services/IErrorLogger.cs
public interface IErrorLogger
{
    Task LogAsync(string operationName, Exception ex, CancellationToken ct = default);
}

// Implementation writes to LogDb.ErrorLog (never to AppDb — separate connection string)
```

### Audit Logging

Two mechanisms:

**1. Interceptor-based (automatic, entity-level):**
```csharp
// AuditSaveChangesInterceptor — intercepts every SaveChanges
// Writes Create/Update/Delete events to AuditLog for entities marked [Auditable]
[Auditable]
public class Order : TenantEntity { ... }
```

**2. Explicit semantic events:**
```csharp
await _auditLogger.LogAsync("Order.Cancelled", entityId: order.Id, detail: $"Reason: {reason}");
```

### Third-Party API Logging

```csharp
// MySolution.Infrastructure/Http/ThirdPartyApiClientBase.cs
public abstract class ThirdPartyApiClientBase(
    IHttpClientFactory factory,
    IThirdPartyApiLogger logger,
    string serviceName)
{
    protected async Task<TResponse?> PostAsync<TResponse>(
        string path, object body, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // ... make request ...
            await logger.LogAsync(serviceName, path, requestBody, responseBody, statusCode, sw.Elapsed);
            return response;
        }
        catch (Exception ex)
        {
            await logger.LogAsync(serviceName, path, requestBody, ex.Message, 0, sw.Elapsed);
            throw;
        }
    }
}
```

**Never** use raw `HttpClient` for external service calls. All calls must log to `ThirdPartyApiLog`.

### Validation

Use FluentValidation. Validators are auto-discovered and registered by convention.

```csharp
public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new OrderLineDtoValidator());
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();
```

`ValidationException` is caught inside `ExecuteAsync` and mapped to `Result.Validation()` (422).

### Caching

```csharp
// Inject ICacheService, not IDistributedCache directly
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

Use Redis in production. Fall back to in-memory for local development (configured via `appsettings.Development.json`).

### Rate Limiting

```csharp
// Program.cs
builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("auth",   c => { c.Window = TimeSpan.FromMinutes(1); c.PermitLimit = 5; });
    opt.AddFixedWindowLimiter("public", c => { c.Window = TimeSpan.FromMinutes(1); c.PermitLimit = 100; });
});

// Controller
[HttpPost("login")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> Login(...) { ... }
```

### Sequence / Document Numbers

Never use `MAX(id) + 1` or application-level incrementing.

```csharp
public interface ISequenceService
{
    Task<string> NextAsync(string sequenceCode, CancellationToken ct = default);
}

// Uses a stored procedure with UPDLOCK + HOLDLOCK for concurrency safety
// Returns formatted strings like "ORD-2024-00042"
```

---

## 8. Adding a Module — Step-by-Step

### Step 1 — Create the project & folder structure

```
MySolution.Modules.Orders/
├── Controllers/OrdersController.cs
├── Extensions/OrdersServiceExtensions.cs
├── Infrastructure/OrdersModelConfigurator.cs
├── Seeds/OrdersSystemSeeder.cs
└── Services/
    ├── IOrderService.cs
    └── OrderService.cs
```

Add project reference to `MySolution.Api.csproj` and `MySolution.Tests.Unit.csproj`.

### Step 2 — Define entities and enums

```csharp
// Always use enums for status fields
public enum OrderStatus { Draft, Confirmed, Shipped, Delivered, Cancelled }

public class Order : TenantEntity           // or BaseEntity for non-tenant apps
{
    public string      Reference { get; set; } = "";
    public OrderStatus Status    { get; set; } = OrderStatus.Draft;
    public long        CustomerId{ get; set; }
    public decimal     Total     { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

public class OrderLine : TenantEntity
{
    public long    OrderId     { get; set; }
    public long    ProductId   { get; set; }
    public decimal Qty         { get; set; }
    public decimal UnitPrice   { get; set; }
    public Order   Order       { get; set; } = null!;
}
```

### Step 3 — EF model configurator

```csharp
internal sealed class OrdersModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder b)
    {
        b.Entity<Order>(e =>
        {
            e.ToTable("Order", schema: "orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Reference).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Total).HasPrecision(18, 4);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.TenantId, x.Reference }).IsUnique();
            e.HasMany(x => x.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId);
        });

        b.Entity<OrderLine>(e =>
        {
            e.ToTable("OrderLine", schema: "orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Qty).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.OrderId);
        });
    }
}
```

### Step 4 — Service interface + DTOs

```csharp
// Keep DTOs in the service file or a sibling DTOs file — no separate "DTO project"
public record CreateOrderDto(long CustomerId, string Reference, List<CreateOrderLineDto> Lines);
public record CreateOrderLineDto(long ProductId, decimal Qty, decimal UnitPrice);
public record OrderSummaryDto(long Id, string Reference, OrderStatus Status, decimal Total, DateTime CreatedAtUtc);

public interface IOrderService
{
    Task<Result<long>>                                              CreateAsync(CreateOrderDto dto, CancellationToken ct = default);
    Task<Result<OrderSummaryDto>>                                   GetAsync(long id, CancellationToken ct = default);
    Task<(IReadOnlyList<OrderSummaryDto> Items, int Total)>         ListAsync(int page, int size, CancellationToken ct = default);
    Task<Result<bool>>                                              ConfirmAsync(long id, CancellationToken ct = default);
    Task<Result<bool>>                                              CancelAsync(long id, string reason, CancellationToken ct = default);
}
```

### Step 5 — Service implementation

```csharp
public class OrderService(AppDbContext db, IBaseService baseService, ISequenceService seq) : IOrderService
{
    public async Task<Result<long>> CreateAsync(CreateOrderDto dto, CancellationToken ct)
    {
        return await baseService.ExecuteAsync("Orders.Create", async () =>
        {
            if (await db.Orders.AnyAsync(o => o.TenantId == db.TenantId && o.Reference == dto.Reference, ct))
                return Result<long>.Conflict($"Reference '{dto.Reference}' already exists.");

            var order = new Order
            {
                Reference  = dto.Reference,
                CustomerId = dto.CustomerId,
                Status     = OrderStatus.Draft,
                Total      = dto.Lines.Sum(l => l.Qty * l.UnitPrice),
                Lines      = dto.Lines.Select(l => new OrderLine { ProductId = l.ProductId, Qty = l.Qty, UnitPrice = l.UnitPrice }).ToList(),
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(order.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ConfirmAsync(long id, CancellationToken ct)
    {
        return await baseService.ExecuteAsync("Orders.Confirm", async () =>
        {
            var order = await db.Orders.FindAsync([id], ct);
            if (order is null)             return Result<bool>.NotFound($"Order {id} not found.");
            if (order.Status != OrderStatus.Draft) return Result<bool>.Conflict("Only Draft orders can be confirmed.");

            order.Status = OrderStatus.Confirmed;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }
}
```

### Step 6 — Seeder

```csharp
[DataSeederOrder(50)]   // lower = runs earlier
public class OrdersSystemSeeder(AppDbContext db) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct)
    {
        var perms = new[]
        {
            ("Orders.View",   "Orders", "View orders"),
            ("Orders.Create", "Orders", "Create orders"),
            ("Orders.Edit",   "Orders", "Edit orders"),
            ("Orders.Delete", "Orders", "Delete orders"),
        };

        foreach (var (code, module, desc) in perms)
        {
            if (!await db.Permissions.AnyAsync(p => p.Code == code, ct))
                db.Permissions.Add(new Permission { Code = code, Module = module, Description = desc });
        }

        await db.SaveChangesAsync(ct);
    }
}
```

### Step 7 — Module extension method

```csharp
public static class OrdersServiceExtensions
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddDataSeeder<OrdersSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, OrdersModelConfigurator>();
        return services;
    }
}
```

### Step 8 — Register in Api

```csharp
// MySolution.Api/Extensions/ApiServiceExtensions.cs
services.AddOrdersModule();
```

### Step 9 — Migration

```bash
dotnet ef migrations add 20240601_Orders_Initial \
  --project src/MySolution.Infrastructure \
  --startup-project src/MySolution.Api \
  --context AppDbContext \
  --output-dir Migrations
```

### Step 10 — Write tests

```
MySolution.Tests.Unit/Modules/Orders/
├── OrderServiceTests.cs
└── OrdersArchTests.cs
```

Minimum test coverage: happy path + every `Result.*` failure branch per method.

---

## 9. Frontend Architecture (Angular)

### Component Rules — Non-Negotiable

```typescript
@Component({
  selector: 'app-orders',
  standalone: true,                                    // always standalone
  changeDetection: ChangeDetectionStrategy.OnPush,     // always OnPush
  imports: [/* explicit — no shared NgModules */],
  template: `...`
})
export class OrdersComponent {
  // ✅ State via signals
  protected readonly orders  = signal<OrderRow[]>([]);
  protected readonly loading = signal(false);
  protected readonly total   = computed(() => this.orders().length);

  // ✅ Dependencies via inject()
  private readonly http = inject(HttpClient);

  // ✅ Inputs via input() not @Input()
  readonly customerId = input.required<number>();

  // ✅ Outputs via output() not @Output() EventEmitter
  readonly selected = output<OrderRow>();
}
```

### HTTP Interceptor Stack

Register in `app.config.ts` in this order:

```typescript
provideHttpClient(withInterceptors([
  apiBaseInterceptor,    // 1. prepend API base URL from environment
  authInterceptor,       // 2. inject Authorization: Bearer token; auto-refresh on 401
  tenantInterceptor,     // 3. inject X-Tenant-Id header
  loadingInterceptor,    // 4. global loading spinner counter
  errorInterceptor,      // 5. toast on 5xx; pass 4xx to callers
]))
```

### Routing Pattern

```typescript
// app.routes.ts
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'orders',
        loadComponent: () => import('./features/orders/list/orders.component').then(m => m.OrdersComponent),
        canActivate: [permissionGuard('Orders.View')],
      },
      {
        path: 'orders/:id',
        loadComponent: () => import('./features/orders/detail/order-detail.component').then(m => m.OrderDetailComponent),
        canActivate: [permissionGuard('Orders.View')],
      },
    ],
  },
  {
    path: '',
    loadComponent: () => import('./layout/auth-layout/auth-layout.component').then(m => m.AuthLayoutComponent),
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
    ],
  },
  { path: '**', redirectTo: '' },
];
```

### Shared Constants Pattern

```typescript
// Never inline strings. All constants in one of these files:

// api-endpoints.ts
export const Api = {
  orders: {
    list:    '/api/orders',
    get:     (id: number) => `/api/orders/${id}`,
    create:  '/api/orders',
    confirm: (id: number) => `/api/orders/${id}/confirm`,
  },
} as const;

// permissions.ts
export const Perms = {
  orders: {
    view:   'Orders.View',
    create: 'Orders.Create',
    edit:   'Orders.Edit',
    delete: 'Orders.Delete',
  },
} as const;

// routes.ts
export const Routes = {
  orders: { list: 'orders', detail: (id: number) => `orders/${id}` },
  login:  'login',
} as const;
```

### Auth Service (Signal-Based)

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _user        = signal<AuthUser | null>(this.loadStoredUser());
  private readonly _accessToken = signal<string | null>(localStorage.getItem('access_token'));

  readonly user        = this._user.asReadonly();
  readonly accessToken = this._accessToken.asReadonly();
  readonly isLoggedIn  = computed(() => this._user() !== null);

  hasPermission(code: string): boolean {
    return this._user()?.permissionCodes.includes(code) ?? false;
  }

  hasFeature(code: string): boolean {
    return this._user()?.featureCodes.includes(code) ?? false;
  }
}
```

### Structural Directives

```html
<!-- Hides button if user lacks the permission -->
<button *hasPermission="'Orders.Create'" (click)="create()">New Order</button>

<!-- Hides section if tenant's plan doesn't include the feature -->
<section *hasFeature="'Reports.Advanced'">...</section>
```

---

## 10. Testing Pyramid

### Unit Tests (fast, no Docker)

```csharp
public class OrderServiceTests : IDisposable
{
    private readonly AppDbContext   _db;
    private readonly IBaseService   _base;
    private readonly OrderService   _sut;
    private readonly SqliteConnection _conn;

    public OrderServiceTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        _db   = BuildDbContext(_conn);
        _db.Database.EnsureCreated();

        _base = new BaseService<AppDbContext>(_db, Substitute.For<IErrorLogger>());
        _sut  = new OrderService(_db, _base, Substitute.For<ISequenceService>());
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsPersistentId()
    {
        var result = await _sut.CreateAsync(ValidDto(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
        (await _db.Orders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_DuplicateReference_ReturnsConflict()
    {
        await _sut.CreateAsync(ValidDto("REF-001"), CancellationToken.None);
        var result = await _sut.CreateAsync(ValidDto("REF-001"), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    public void Dispose() { _db.Dispose(); _conn.Dispose(); }
}
```

**SQLite gotchas to avoid:**
- Use `IsConcurrencyToken()` not `IsRowVersion()` in EF config
- Don't call `SumAsync(decimal)` — use `ToListAsync()` + `.Sum()` client-side
- Avoid DB functions not supported by SQLite (`DATEDIFF`, `GETUTCDATE`, etc.)

### Integration Tests (Testcontainers — real SQL Server)

```csharp
public class OrdersControllerTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    [Fact]
    public async Task POST_orders_WithValidBody_Returns201()
    {
        var client = fixture.CreateAuthenticatedClient(permissions: ["Orders.Create"]);
        var resp   = await client.PostAsJsonAsync("/api/orders", new { Reference = "ORD-001", ... });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_orders_WithoutPermission_Returns403()
    {
        var client = fixture.CreateAuthenticatedClient(permissions: []);   // no permission
        var resp   = await client.PostAsJsonAsync("/api/orders", new { Reference = "ORD-002", ... });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

### Architecture Tests

```csharp
[Fact]
public void SaveChangesAsync_MustBeInsideExecuteAsync()
{
    // No service class may call SaveChangesAsync directly
    var serviceTypes = Types.InAssemblies(ModuleAssemblies)
        .That().HaveNameEndingWith("Service")
        .GetTypes();

    foreach (var t in serviceTypes)
    {
        var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        // inspect IL or source — SaveChangesAsync must only appear inside lambdas passed to ExecuteAsync
    }
}

[Fact]
public void Controllers_MustExtendBaseController()
{
    Types.InAssemblies(ModuleAssemblies)
        .That().HaveNameEndingWith("Controller")
        .Should().Inherit(typeof(BaseController))
        .BecauseOf("BaseController provides consistent Result<T> → HTTP mapping");
}

[Fact]
public void Modules_MustNotReference_OtherModules()
{
    foreach (var moduleAssembly in ModuleAssemblies)
    {
        Types.InAssembly(moduleAssembly)
            .Should()
            .NotHaveDependencyOnAny(OtherModuleNamespaces(moduleAssembly))
            .BecauseOf("modules communicate through Shared interfaces only");
    }
}
```

---

## 11. CI/CD Skeleton

### `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: "YourStr0ngPassword!"
          ACCEPT_EULA: "Y"
        ports: ["1433:1433"]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "8.x" }
      - run: dotnet restore src/MySolution.sln
      - run: dotnet build src/MySolution.sln --no-restore -c Release
      - run: dotnet test src/MySolution.Tests.Arch       --no-build -c Release
      - run: dotnet test src/MySolution.Tests.Unit       --no-build -c Release
      - run: dotnet test src/MySolution.Tests.Integration --no-build -c Release
        env:
          ConnectionStrings__AppDb: "Server=localhost,1433;Database=TestDb;User=sa;Password=YourStr0ngPassword!;TrustServerCertificate=true"

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: "20" }
      - run: cd src/web && npm ci
      - run: cd src/web && npx ng build --configuration production
      - run: cd src/web && npm test -- --watchAll=false
```

### `.github/workflows/deploy.yml`

```yaml
name: Deploy

on:
  workflow_dispatch:
    inputs:
      environment:
        type: choice
        options: [staging, production]

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: ${{ inputs.environment }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "8.x" }
      - run: dotnet publish src/MySolution.Api -c Release -o ./publish
      - name: SSH deploy
        run: |
          # copy artifact, stop service, deploy, migrate, restart
          ssh ${{ secrets.SSH_USER }}@${{ secrets.SSH_HOST }} "..."
      - name: Smoke test
        run: curl -f https://${{ secrets.API_HOST }}/api/health
```

---

## 12. Project Bootstrap Checklist

Use this when starting any new project with this architecture.

### Solution Setup
- [ ] Create solution with the project layout from § 2
- [ ] Configure one-way dependency rules (verified by arch test)
- [ ] Set up `docker-compose.yml` (SQL Server, Redis, MailHog)
- [ ] Configure `appsettings.Development.json` (local DB, Redis, feature flags)
- [ ] Configure `appsettings.Production.json` (empty — all secrets via env vars)

### Backend Foundation
- [ ] Copy `BaseEntity`, `TenantEntity` (if multi-tenant)
- [ ] Copy `Result<T>`, `ResultExtensions`
- [ ] Copy `BaseService<TDbContext>`, `IBaseService`
- [ ] Copy `BaseController`
- [ ] Copy `RequirePermissionAttribute`, `RequireFeatureAttribute`, `RequireCaptchaAttribute`
- [ ] Set up `AppDbContext` with `AuditSaveChangesInterceptor`
- [ ] Set up `IEntityModelConfigurator` plugin pattern
- [ ] Set up `IErrorLogger` → `ErrorLog` table in LogDb (or same DB if small project)
- [ ] Set up `ISequenceService` + `usp_AllocateSequenceNumber` stored procedure
- [ ] Set up `IDataSeeder` + ordered seeder runner
- [ ] Configure JWT authentication (15 min access / 30 day refresh)
- [ ] Configure `PermissionPolicyProvider` + `PermissionAuthorizationHandler`
- [ ] Configure FluentValidation auto-validation
- [ ] Configure Serilog (file + structured JSON sink for production)
- [ ] Configure rate limiting (auth: 5/min, public: 100/min)
- [ ] Configure health check endpoint (`/api/health`)
- [ ] Configure Hangfire or Quartz (if background jobs needed)
- [ ] Configure CORS (dev: `*`; production: explicit whitelist)
- [ ] Configure Swagger/OpenAPI with JWT bearer auth

### Identity Module (always first module)
- [ ] `User`, `Role`, `Permission`, `RolePermission`, `RefreshToken` entities
- [ ] Register/Login/ForgotPassword/ResetPassword endpoints (all `[AllowAnonymous, RequireCaptcha]`)
- [ ] Token refresh endpoint
- [ ] Seed default admin user + admin role with all permissions
- [ ] Invite flow (optional but recommended from day 1)

### Architecture Tests (wire up immediately)
- [ ] `SaveChangesAsync_MustBeInsideExecuteAsync`
- [ ] `Controllers_MustExtendBaseController`
- [ ] `Modules_MustNotReference_OtherModules`
- [ ] `PublicAuthEndpoints_RequireCaptcha`
- [ ] `StatusFields_MustBeEnums` (not `string`)

### Frontend Foundation
- [ ] Angular project with `standalone: true` as default
- [ ] Configure PrimeNG theme with custom brand colours
- [ ] Set up 5-interceptor HTTP stack (apiBase, auth, tenant, loading, error)
- [ ] `AuthService` with signal-based state
- [ ] `authGuard`, `permissionGuard`
- [ ] `*hasPermission`, `*hasFeature` directives
- [ ] `AppShellComponent` (sidebar + topbar)
- [ ] `AuthLayoutComponent` (centred card)
- [ ] Shared constants files: `api-endpoints.ts`, `permissions.ts`, `routes.ts`, `messages.ts`
- [ ] Shared components: `<app-data-table>`, `<app-page-header>`, `<app-confirm-dialog>`
- [ ] Login + ForgotPassword + ResetPassword pages

### CI/CD
- [ ] `ci.yml` — build + all tests on every push
- [ ] `deploy.yml` — manual deploy to staging
- [ ] Branch protection: require `ci` to pass before merge to `main`
- [ ] Coverage gate: ≥ 75% overall; service layer ≥ 80%

---

## 13. Decision Log

Record every non-obvious architectural choice here. Future developers deserve to know why, not just what.

| Date | Decision | Alternatives Considered | Reason |
|------|----------|------------------------|--------|
| — | Modular monolith over microservices | Microservices, serverless | Operational complexity of microservices is not justified until a specific module needs independent scaling. Start simple, extract when proven necessary. |
| — | `Result<T>` over exceptions | Throw domain exceptions, `OneOf<T,E>` | Exceptions for control flow are invisible to callers and break return-type contracts. `Result<T>` makes failure explicit in the signature. |
| — | EF Core + Dapper hybrid | EF only, Dapper only, raw SQL only | EF handles CRUD and tenant filters correctly. Dapper handles complex reports and aggregates without fighting the ORM. |
| — | SQLite in-memory for unit tests | EF in-memory provider, Testcontainers for all tests | EF in-memory provider diverges from SQL Server semantics. Testcontainers is too slow for unit test feedback loops. SQLite is a good middle ground with known limitations. |
| — | Permissions in JWT `perms` claim (comma-separated) | DB lookup per request, Redis cache per request | Eliminates a DB/cache round-trip on every request. Acceptable staleness: token TTL (15 min). On role change, force token refresh. |
| — | `IsConcurrencyToken()` not `IsRowVersion()` | `IsRowVersion()` | `IsRowVersion()` is SQL Server-specific. `IsConcurrencyToken()` works on both SQL Server and SQLite, keeping unit tests valid. |

---

