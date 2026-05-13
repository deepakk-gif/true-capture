# Backend API Flow — `true_capture_backend` (.NET)

> Auto-maintained doc. Captures the call flow per module:
> Route → Controller → Service → DB. Update whenever a module changes.

## Architecture (shared)

- **Solution**: `TrueCapture.sln` — modular monolith.
  - `TrueCapture.Api` — composition root (`Program.cs` + `Extensions/AddTrueCapture`).
  - `TrueCapture.Shared` — `BaseController`, `Result<T>`, `IBaseService.ExecuteAsync(...)` (named-operation logging + optional transaction), `JwtClaims`, `RateLimitPolicies`, `RequireCaptchaAttribute`.
  - `TrueCapture.Infrastructure` — `AppDbContext` (EF Core), seeders, model configurators.
  - `TrueCapture.Modules.*` — feature modules (each owns its entities, services, controllers, model config, seeder).
- **Pipeline** (`Program.cs`): Serilog → CORS → Authentication → Authorization → RateLimiter → MapControllers + `/api/health`. Migrations + seeding run on dev startup when `RunMigrationsOnStartup=true`.
- **Module wiring**: each module exposes `AddXxxModule(IServiceCollection, IConfiguration)`. Identity registers `JwtOptions`, `ITokenService`, `IAuthService`, the model configurator, and a system data seeder.

---

## Identity / Auth Module — DONE

Base route: `/api/auth`. Controller: `TrueCapture.Modules.Identity/Controllers/AuthController.cs`.
All anonymous endpoints require captcha (`[RequireCaptcha]`) and use the `RateLimitPolicies.Auth` policy.

### Endpoints

| Method | Route | Auth | DTO in | Service call |
|---|---|---|---|---|
| POST | `/api/auth/register` | Anonymous + Captcha + RateLimit | `RegisterDto(Email, Username, Password)` | `AuthService.RegisterAsync` |
| POST | `/api/auth/login` | Anonymous + Captcha + RateLimit | `LoginDto(Email, Password)` | `AuthService.LoginAsync` |
| POST | `/api/auth/refresh` | Anonymous + RateLimit | `RefreshDto(RefreshToken)` | `AuthService.RefreshAsync` |
| POST | `/api/auth/logout` | `[Authorize]` | `RefreshDto(RefreshToken)` | `AuthService.LogoutAsync` |

All return `Result<AuthTokensDto>` (or `Result<bool>` for logout) wrapped by `BaseController.Ok(...)`.
`AuthTokensDto = (AccessToken, RefreshToken, AccessExpiresAtUtc)`.

### Files

| Concern | Path |
|---|---|
| Controller | `src/TrueCapture.Modules.Identity/Controllers/AuthController.cs` |
| Service | `src/TrueCapture.Modules.Identity/Services/{IAuthService,AuthService}.cs` |
| Token service | `src/TrueCapture.Modules.Identity/Services/{ITokenService,TokenService}.cs` |
| Entities | `src/TrueCapture.Modules.Identity/Entities/{User,Role,Permission,UserRole,RolePermission,RefreshToken}.cs` |
| EF model config | `src/TrueCapture.Modules.Identity/Infrastructure/IdentityModelConfigurator.cs` |
| System seeder | `src/TrueCapture.Modules.Identity/Seeds/IdentitySystemSeeder.cs` |
| DI | `src/TrueCapture.Modules.Identity/Extensions/IdentityServiceExtensions.cs#AddIdentityModule` |
| Config | `src/TrueCapture.Api/appsettings.json#Jwt` (`Issuer`, `Audience`, `SigningKey ≥ 32 chars`, `AccessMinutes`, `RefreshDays`) |

### Flows

**Register** — `POST /api/auth/register`
→ `AuthController.Register`
→ `AuthService.RegisterAsync` (wrapped in `IBaseService.ExecuteAsync("Auth.Register", …, useTransaction: true)`):
  1. Normalize email (lowercase/trim) + username.
  2. Conflict check on `User.Email` and `User.Username` → `Result.Conflict` if taken.
  3. Insert `User` with `BCrypt.HashPassword(workFactor: 12)`, `EmailVerified=false`, `IsActive=true`.
  4. `LoadPermissionCodesAsync(userId)` (UserRole ⋈ RolePermission ⋈ Permission, distinct codes).
  5. `TokenService.Issue(user, perms)` → JWT (HS256, claims: `UserId`, `Email`, `Name`, `Role`, `Permissions`, `Features`) + opaque base64-url refresh token.
  6. Persist `RefreshToken { TokenHash = SHA256(refresh), ExpiresAtUtc = now + 30d }`.
  7. Return `AuthTokensDto`.

**Login** — `POST /api/auth/login`
→ `AuthController.Login` (passes `User-Agent` header + remote IP into the service)
→ `AuthService.LoginAsync` (transactional):
  1. Lookup user by normalized email; reject if missing or `!IsActive`.
  2. `BCrypt.Verify` password → `Result.Unauthorized` on mismatch.
  3. Update `LastLoginAtUtc`.
  4. Issue tokens (same as register), persist `RefreshToken` with `UserAgent` + `IpAddress`.
  5. Return `AuthTokensDto`.

**Refresh** — `POST /api/auth/refresh`
→ `AuthService.RefreshAsync` (transactional, **rotation**):
  1. Hash incoming refresh token; load matching `RefreshToken` (with `User`).
  2. Reject if missing, revoked, or expired (`!IsActive`).
  3. Mark stored token revoked (`RevokedAtUtc = now`).
  4. Issue new access + refresh tokens; set `stored.ReplacedByHash` to new hash; insert new `RefreshToken` row with `UserAgent`/`IpAddress`.
  5. Return new `AuthTokensDto`.

**Logout** — `POST /api/auth/logout` (`[Authorize]`, no transaction)
→ `AuthService.LogoutAsync`:
  1. Hash token; find row.
  2. If absent → succeed (idempotent).
  3. Else set `RevokedAtUtc = now`; save.

### Data model (Identity)

- `User` — Email, Username, PasswordHash, DisplayName?, AvatarUrl?, Bio?, EmailVerified, IsActive, IsAdmin, IsVerified, GoogleSubject?, LastLoginAtUtc?
- `Role` — Code, Name, Description?, IsSystem
- `Permission` / `RolePermission` / `UserRole` — RBAC join tables
- `RefreshToken` — UserId, TokenHash, ExpiresAtUtc, RevokedAtUtc?, ReplacedByHash?, UserAgent?, IpAddress?, computed `IsActive`

### Notes / TODO (mobile parity)

The mobile app (`api_endpoints.dart`) calls these auth endpoints, **none of which exist on the backend yet**:

| Mobile call | Status |
|---|---|
| `POST /auth/sign-in` | ❌ — backend exposes `/api/auth/login` |
| `POST /auth/sign-up` | ❌ — backend exposes `/api/auth/register` |
| `POST /auth/sign-out` | ❌ — backend exposes `/api/auth/logout` |
| `POST /auth/social-login` | ❌ — not implemented |
| `POST /auth/forgot-password` | ❌ — not implemented |
| `POST /auth/reset-password` | ❌ — not implemented |
| `POST /auth/send-otp` | ❌ — not implemented |
| `POST /auth/verify-otp` | ❌ — not implemented |
| `POST /auth/refresh-token` | ❌ — backend exposes `/api/auth/refresh` |

Either align the mobile `ApiEndpoints` to the existing `/api/auth/{register,login,refresh,logout}` routes, or add the missing endpoints + an `IOtpService` / password-reset / social-login flow on the backend.

---

## Pending Modules

- Captures, media upload, feed, notifications, search, etc. — not yet implemented; document each as it lands.
