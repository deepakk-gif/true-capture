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

## Email Infrastructure (shared) — IN PROGRESS

Used by the Identity module for OTP delivery + password reset; will be reused by the Admin module for admin-to-user email send.

| Concern | Path |
|---|---|
| Abstraction | `src/TrueCapture.Shared/Services/IEmailSender.cs` — `record EmailMessage(ToEmail, Subject, BodyText, BodyHtml?)` + `Task SendAsync(...)` |
| Options | `src/TrueCapture.Infrastructure/Services/EmailOptions.cs` (binds to `appsettings.json#Email`) |
| SMTP impl | `src/TrueCapture.Infrastructure/Services/SmtpEmailSender.cs` (MailKit) — when `Host` is empty, logs the email body instead (dev fallback so OTPs surface in console) |
| Config | `Email:Host`, `Email:Port`, `Email:UseStartTls`, `Email:Username`, `Email:Password`, `Email:FromAddress`, `Email:FromName` |

DI registration is added in the Identity module's auth-extension wiring (next milestone) — `services.AddOptions<EmailOptions>().BindConfiguration("Email")` + `services.AddScoped<IEmailSender, SmtpEmailSender>()`.

---

## Identity / Auth Module — EXTENDED (OTP / forgot-password / Google)

Existing endpoints (`/api/auth/{register, login, refresh, logout}`) remain. Five new endpoints landed: `send-otp`, `verify-otp`, `forgot-password`, `reset-password`, `google`. All anonymous + captcha-gated + on the `RateLimitPolicies.Auth` policy except `google` (no captcha — Google's signed token is itself an attestation).

### Entities
- `src/TrueCapture.Modules.Identity/Entities/OtpCode.cs` — `OtpCode { UserId?, Email, CodeHash, Purpose: VerifyEmail|PasswordReset, ExpiresAtUtc, UsedAtUtc?, AttemptCount }` with `IsActive` computed
- `enum OtpPurpose { VerifyEmail = 1, PasswordReset = 2 }`
- EF mapping: `IdentityModelConfigurator.cs` — table `identity.OtpCode`; indexes on `(Email, Purpose)` and `UserId`; FK to `User` with `OnDelete(SetNull)`; `Purpose` stored as int.

### Service contract extensions (`Services/IAuthService.cs`)
New DTOs added: `ForgotPasswordDto(Email)`, `ResetPasswordDto(Email, Code, NewPassword)`, `VerifyOtpAndIssueDto(Email, Code, Purpose)`, `GoogleSignInDto(IdToken)`.
New methods on `IAuthService`: `VerifyOtpAndIssueAsync`, `ForgotPasswordAsync`, `ResetPasswordAsync`, `GoogleSignInAsync` — implementations land in the next milestone.

### OtpService (`Services/OtpService.cs` implementing `IOtpService`)

- `record OtpSendRequest(Email, Purpose)` / `OtpVerifyRequest(Email, Code, Purpose)` / `OtpVerifyResult(User?, Purpose)`
- `SendAsync` (transactional, op name `Otp.Send`):
  1. Lowercase + trim email.
  2. Rate-limit: rolling 60-min window, max 5 sends per (email, purpose) → `Result.Validation` on overflow.
  3. Look up `User` by email; for `PasswordReset` on unknown email, return `Success(true)` without dispatching (no enumeration leak).
  4. Generate 6-digit code via `RandomNumberGenerator`, SHA256-hash; persist `OtpCode` row with 10-minute expiry.
  5. Dispatch via `IEmailSender.SendAsync` with purpose-specific subject.
- `VerifyAsync` (transactional, op name `Otp.Verify`):
  1. Load most-recent unused row for `(email, purpose)`.
  2. Check expiry; reject if past `ExpiresAtUtc`.
  3. Enforce `MaxVerifyAttempts = 5` per row; over-limit returns `Unauthorized("Too many attempts.")`.
  4. Compare hashes via `FixedTimeEquals`; mismatch increments `AttemptCount`.
  5. On success: stamp `UsedAtUtc`; if `Purpose==VerifyEmail` and `User` is non-null, set `User.EmailVerified=true`.
  6. Return resolved `User?` so callers (forgot-password / verify-email) can branch.

### Planned endpoints (this work-stream)
| Method | Route | Auth | Service call |
|---|---|---|---|
| POST | `/api/auth/send-otp` | Anonymous + Captcha + RateLimit | `OtpService.SendAsync` + `IEmailSender.SendAsync` |
| POST | `/api/auth/verify-otp` | Anonymous + Captcha + RateLimit | `AuthService.VerifyOtpAndIssueAsync` (calls `OtpService.VerifyAsync`, then issues tokens) |
| POST | `/api/auth/forgot-password` | Anonymous + Captcha + RateLimit | `AuthService.ForgotPasswordAsync` (delegates to `OtpService.SendAsync` with `PasswordReset`) |
| POST | `/api/auth/reset-password` | Anonymous + Captcha + RateLimit | `AuthService.ResetPasswordAsync` (verifies OTP, updates `PasswordHash`, revokes all refresh tokens) |
| POST | `/api/auth/google` | Anonymous + RateLimit | `AuthService.GoogleSignInAsync` (validates ID token via `GoogleJsonWebSignature.ValidateAsync` against `Authentication:Google:ClientId`) |

### AuthService extensions (`Services/AuthService.cs`)
Existing flows (Register / Login / Refresh / Logout) unchanged. New flows:

- **`VerifyOtpAndIssueAsync`** (transactional, op `Auth.VerifyOtpAndIssue`): calls `IOtpService.VerifyAsync` → resolves `User` → issues new access + refresh tokens via `IssueAndPersistRefresh`; returns `Unauthorized` if OTP invalid or user inactive.
- **`ForgotPasswordAsync`**: delegates straight to `OtpService.SendAsync(email, PasswordReset)`. Always returns `Success` (no enumeration leak — handled in OtpService).
- **`ResetPasswordAsync`** (transactional, op `Auth.ResetPassword`):
  1. Verify OTP with `Purpose=PasswordReset` via `IOtpService`.
  2. Resolve `User` (from OTP row or by email).
  3. `PasswordHash = BCrypt.HashPassword(work=12)`.
  4. Revoke every `RefreshToken` where `RevokedAtUtc IS NULL AND ExpiresAtUtc > now` for that user.
- **`GoogleSignInAsync`** (transactional, op `Auth.GoogleSignIn`):
  1. Read `Authentication:Google:ClientId`; reject `Failure` if unset.
  2. `GoogleJsonWebSignature.ValidateAsync(idToken, { Audience = [ClientId] })` → catches `InvalidJwtException` as `Unauthorized`.
  3. Match by `GoogleSubject`, else by `Email`. If neither, create a new `User` with `EmailVerified=payload.EmailVerified`, `GoogleSubject=payload.Sub`, blank `PasswordHash`, auto-generated unique username from email prefix.
  4. If matched-by-email but `GoogleSubject` was empty, fill it in (linking).
  5. Issue tokens via `IssueAndPersistRefresh`.
- **Login regression**: now also rejects with reason "uses social provider" when `PasswordHash` is empty (Google-only account).
- **Refresh regression**: also rejects if `User.IsActive` is false (suspend / ban path).

`IssueAndPersistRefresh(user, perms, ua, ip)` is the shared helper (queues a new `RefreshToken` row, returns `IssuedTokens`). `GenerateUniqueUsernameAsync(email)` strips local-part and appends a numeric suffix on collision.

### Config (added)
- `Authentication:Google:ClientId` — Google OAuth client ID for ID-token audience validation.
- `Email:Host`, `Email:Port`, `Email:UseStartTls`, `Email:Username`, `Email:Password`, `Email:FromAddress`, `Email:FromName`.
- Placeholders for both live in `src/TrueCapture.Api/appsettings.json` (empty values); developers override locally via `appsettings.Development.json` or environment variables.

### Controller (`Controllers/AuthController.cs`)
Constructor now also depends on `IOtpService`. New action methods:
- `POST send-otp` → `OtpService.SendAsync` (body: `record SendOtpRequest(Email, Purpose)` — local to AuthController.cs)
- `POST verify-otp` → `AuthService.VerifyOtpAndIssueAsync` (body: `VerifyOtpAndIssueDto(Email, Code, Purpose)`)
- `POST forgot-password` → `AuthService.ForgotPasswordAsync` (body: `ForgotPasswordDto(Email)`)
- `POST reset-password` → `AuthService.ResetPasswordAsync` (body: `ResetPasswordDto(Email, Code, NewPassword)`)
- `POST google` → `AuthService.GoogleSignInAsync` (body: `GoogleSignInDto(IdToken)`)

### DI wiring (`Extensions/IdentityServiceExtensions.cs#AddIdentityModule`)
- `services.Configure<GoogleAuthOptions>(cfg.GetSection("Authentication:Google"))`
- `services.Configure<EmailOptions>(cfg.GetSection("Email"))`
- `services.AddScoped<IEmailSender, SmtpEmailSender>()`
- `services.AddScoped<IOtpService, OtpService>()`

### Migration note
The `OtpCode` entity is mapped but the migration file is generated by the developer once locally — run `dotnet ef migrations add AddOtpCode --project src/TrueCapture.Infrastructure --startup-project src/TrueCapture.Api`. Dev-mode startup applies it via `RunMigrationsOnStartup=true`.

### Test coverage (unit, SQLite-in-memory)
- `Tests.Unit/Modules/Identity/OtpServiceTests.cs` — happy-path send, enumeration-safe send for unknown email (PasswordReset), rate-limit on 6th send, wrong-code increments AttemptCount + Unauthorized, expired-code Unauthorized, correct-code marks `UsedAtUtc` and flips `EmailVerified` for `VerifyEmail`.
- `Tests.Unit/Modules/Identity/AuthServiceExtensionsTests.cs` — forgot-password no-leak / persists+sends; reset-password updates BCrypt hash and revokes every active refresh-token; bad OTP leaves password unchanged; Google sign-in returns Failure when `ClientId` is unconfigured; verify-otp-and-issue returns tokens and flips `EmailVerified`.

---

## Users Module — IN PROGRESS (admin users list)

`src/TrueCapture.Modules.Users/` (new project, narrowly references Identity to read the `User` entity).

### Endpoint
| Method | Route | Auth | Service |
|---|---|---|---|
| GET | `/api/admin/users` | `[AdminOnly]` (admin role policy) | `AdminUsersService.ListAsync` |

Query: `?search=&isActive=&isAdmin=&isVerified=&hasGoogle=&cursor=&limit=20`.
- `search` matches Email OR Username (case-insensitive substring).
- All booleans are tri-state (omit = either).
- `cursor` is opaque base64 of `"<createdAtUtcTicks>:<id>"`.
- `limit` clamped to `[1,100]`, default 20.

### Files
| Concern | Path |
|---|---|
| Project | `TrueCapture.Modules.Users/TrueCapture.Modules.Users.csproj` (added to `TrueCapture.sln` and referenced by `TrueCapture.Api`) |
| Controller | `Controllers/AdminUsersController.cs` |
| Service | `Services/{IAdminUsersService,AdminUsersService}.cs` |
| DTOs | `Models/AdminUserModels.cs` — `AdminUserListQuery`, `AdminUserListItem`, `AdminUserListResult` |
| DI | `Extensions/UsersServiceExtensions.cs#AddUsersModule` (called from `ApiServiceExtensions.AddTrueCapture`) |

### Flow
`GET /api/admin/users?...`
→ `[AdminOnly]` filter checks JWT `role=Admin` claim via the `admin` policy
→ `AdminUsersController.List([FromQuery] AdminUserListQuery)`
→ `AdminUsersService.ListAsync` (op name `AdminUsers.List`):
  1. Build `IQueryable<User>` (`AsNoTracking`); append `Where`s for each non-null filter.
  2. `Count` for `total`.
  3. Decode opaque cursor (`createdAtUtcTicks:id`); add cursor predicate for stable `(CreatedAtUtc DESC, Id DESC)` pagination.
  4. `Take(limit + 1)`; the extra row signals "more pages exist" and becomes the next cursor.
  5. Project into `AdminUserListItem` (15 fields; flattens `GoogleSubject` into a boolean `HasGoogle`).
  6. Return `AdminUserListResult { Items, NextCursor?, Total }`.

### Authorization (`TrueCapture.Shared/Authorization/AdminOnlyAttribute.cs`)
- New `[AdminOnly]` attribute = `[Authorize(Policy = "admin")]`.
- `PermissionPolicyProvider.GetPolicyAsync("admin")` now returns a policy requiring an authenticated user with `role=Admin` (the claim issued by `TokenService.Issue` when `User.IsAdmin = true`).

### DI wiring (`Api/Extensions/ApiServiceExtensions.cs`)
- `services.AddUsersModule(cfg)` invoked from `AddTrueCapture`.

## Pending Modules

- Captures, media upload, feed, notifications, search, etc. — not yet implemented; document each as it lands.
