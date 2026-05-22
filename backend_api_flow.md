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
- **JWT claims**: `AddJwtBearer` sets `MapInboundClaims = false` so claims stay verbatim (`sub`, `email`, `name`, `role`). Without it the handler rewrites `sub` → `ClaimTypes.NameIdentifier` and `BaseController.CurrentUserId` / `CurrentUser.UserId` (both read `"sub"`) resolve to `0`. `CurrentUserId` also falls back to `ClaimTypes.NameIdentifier` defensively.
- **Module wiring**: each module exposes `AddXxxModule(IServiceCollection, IConfiguration)`. Identity registers `JwtOptions`, `ITokenService`, `IAuthService`, the model configurator, and a system data seeder.

---

## Identity / Auth Module — DONE

Base route: `/api/auth`. Controller: `TrueCapture.Modules.Identity/Controllers/AuthController.cs`.
All anonymous endpoints require captcha (`[RequireCaptcha]`) and use the `RateLimitPolicies.Auth` policy.

### Endpoints

| Method | Route | Auth | DTO in | Service call |
|---|---|---|---|---|
| POST | `/api/auth/register` | Anonymous + Captcha + RateLimit | `RegisterDto(Email, Username, Password, FcmToken?, DeviceType?)` | `AuthService.RegisterAsync` |
| POST | `/api/auth/login` | Anonymous + Captcha + RateLimit | `LoginDto(Email, Password, FcmToken?, DeviceType?)` | `AuthService.LoginAsync` |
| POST | `/api/auth/refresh` | Anonymous + RateLimit | `RefreshDto(RefreshToken, FcmToken?, DeviceType?)` | `AuthService.RefreshAsync` |
| POST | `/api/auth/logout` | `[Authorize]` | `RefreshDto(RefreshToken, FcmToken?, DeviceType?)` | `AuthService.LogoutAsync(refreshToken, currentUserId, fcmToken?)` |

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

- `User` — Email, Username, PasswordHash, DisplayName?, AvatarUrl?, Bio?, EmailVerified, IsActive, IsAdmin, IsVerified, GoogleSubject?, LastLoginAtUtc?, Gender? (enum), AccountType (enum, default Public)
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
- Constructor also depends on `IHostEnvironment env` (for the dev fixed-code path below).
- `SendAsync` (transactional, op name `Otp.Send`):
  1. Lowercase + trim email.
  2. Rate-limit: rolling 60-min window, max 5 sends per (email, purpose) → `Result.Validation` on overflow.
  3. Look up `User` by email; for `PasswordReset` on unknown email, return `Success(true)` without dispatching (no enumeration leak).
  4. Generate the code: in **Development** (`env.IsDevelopment()`) it is the fixed const `DevFixedCode = "123456"` so localhost testing skips a real inbox; otherwise a 6-digit `RandomNumberGenerator` code. SHA256-hash it; persist `OtpCode` row with 10-minute expiry.
  5. Dispatch via `IEmailSender.SendAsync` with purpose-specific subject.
- `VerifyAsync` (transactional, op name `Otp.Verify`):
  0. **Development bypass**: when `env.IsDevelopment()` and the submitted code equals `DevFixedCode` (`"123456"`), the row hash check is skipped entirely — resolve the `User` by email, burn any pending `OtpCode` rows (`UsedAtUtc = now`), flip `EmailVerified` for `VerifyEmail`, and return `Success`. This makes localhost testing work even with a stale or missing OTP row (e.g. mobile auto-submit firing before send-otp, or rows created before the dev fixed-code path landed).
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

## Admin authorization model — IN PROGRESS (super-admin + per-user permissions)

There are now **three** ranks:

| Rank | Marker | Powers |
|---|---|---|
| Regular user | `User.IsAdmin = false` | Mobile sign-up creates these. Cannot use the admin panel (middleware 403). |
| Admin | `User.IsAdmin = true` + role `admin` | Can sign into the admin panel; specific powers come from per-user permissions granted by a super-admin. |
| Super-admin | `User.IsAdmin = true` + role `super-admin` | Holds every permission, including `Users.CreateAdmin` and `Users.AssignPermissions`. **Only super-admins can mint other admins.** |

### Permission resolution
A signed-in user's effective permission set is the **union** of:
1. **Role-derived** — `UserRole → RolePermission → Permission.Code`.
2. **Per-user grants** — new `UserPermission(UserId, PermissionId)` table (file: `Entities/UserPermission.cs`).

`AuthService.LoadPermissionCodesAsync(userId)` (file `Services/AuthService.cs`) returns `(fromRoles).Concat(fromUser).Distinct()`. The resulting code list becomes the `perms` JWT claim issued by `TokenService.Issue`, which `PermissionAuthorizationHandler` reads to satisfy any `[RequirePermission("X")]` attribute.

### `UserPermission` entity / table
- `UserPermission { UserId, PermissionId }` — BaseEntity + RowVersion.
- EF mapping in `IdentityModelConfigurator`:
  - Table `identity.UserPermission`
  - Unique index on `(UserId, PermissionId)`
  - FKs to `User` and `Permission` (default cascade).

Migration: `dotnet ef migrations add AddSuperAdminAndUserPermission --project src/TrueCapture.Infrastructure --startup-project src/TrueCapture.Api`.

## Users Module — IN PROGRESS (admin users list + admin accounts)

New endpoints in the Users module:

| Method | Route | Auth | Service |
|---|---|---|---|
| GET  | `/api/admin/permissions` | `[AdminOnly]` + `[RequirePermission("Users.CreateAdmin")]` | `AdminAccountsService.ListPermissionsAsync` |
| POST | `/api/admin/users`       | `[AdminOnly]` + `[RequirePermission("Users.CreateAdmin")]` | `AdminAccountsService.CreateAdminAsync` |

Files: `Controllers/AdminAccountsController.cs`, `Services/{IAdminAccountsService,AdminAccountsService}.cs`, `Models/AdminAccountModels.cs` (`CreateAdminRequest`, `CreatedAdminResponse`, `PermissionDescriptor`). DI registered in `Extensions/UsersServiceExtensions.cs#AddUsersModule`.

Test coverage: `Tests.Unit/Modules/Users/AdminAccountsServiceTests.cs` — happy-path persists user + admin role + per-user permissions and never silently grants `Users.CreateAdmin`; unknown permission code → `Validation`; duplicate email → `Conflict`; short password → `Validation`; `ListPermissionsAsync` returns every seeded row.

`CreateAdminAsync` flow:
1. Normalize email/username; validate (email required, username ≥ 3 chars, password ≥ 8 chars).
2. Reject duplicate email/username with `Conflict`.
3. Resolve every `PermissionCode` to a row; unknown codes → `Validation` (whole request fails).
4. Insert `User` with `IsAdmin=true`, `EmailVerified=true`, BCrypt password (work=12), optional `DisplayName`.
5. Attach the `admin` role (so the JWT `role` claim is `Admin`).
6. Insert one `UserPermission` row per requested permission code.
7. Return `CreatedAdminResponse` with the granted permission codes.



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

## Notifications / FCM — IN PROGRESS (push notifications)

End-to-end Firebase Cloud Messaging integration. Mobile sends `fcm_token` + `device_type` on every token-issuing auth call; backend persists per-device rows and auto-subscribes them to the `"all"` topic; admins can broadcast by topic, by user filter, or by explicit user IDs.

### Config (`appsettings.json#Firebase`)
| Key | Purpose |
|---|---|
| `Firebase:ServiceAccountJsonPath` | Path to the Firebase Admin service-account JSON (relative to `ContentRootPath`). Defaults to `config/firebase-service-account.json`. |
| `Firebase:ProjectId` | Firebase project ID (optional — read from service account if blank). |
| `Firebase:DefaultTopic` | Default topic name new devices subscribe to. Defaults to `all`. |

A blank `{}` placeholder lives at `src/TrueCapture.Api/config/firebase-service-account.json` (copied to output via `CopyToOutputDirectory=PreserveNewest`). When the file is empty or the JSON contains no credentials, the FCM sender falls back to log-only mode (mirrors `SmtpEmailSender` when SMTP host is unset).

### NuGet
- `FirebaseAdmin` added to `TrueCapture.Infrastructure.csproj`. Lives in Infrastructure (same layer as MailKit for SMTP).

### Module structure
| Concern | Path |
|---|---|
| Module project | `src/TrueCapture.Modules.Notifications/` (sibling to `TrueCapture.Modules.Users`) referenced by `TrueCapture.Api.csproj` |
| Abstraction | `src/TrueCapture.Shared/Services/IFcmSender.cs` — exposes `SendToTokenAsync`, `SendToTokensAsync` (multicast ≤ 500), `SendToTopicAsync`, `SubscribeAsync`, `UnsubscribeAsync`. Returns `FcmMulticastResult(SuccessCount, FailureCount, InvalidTokens[])` so callers can prune unregistered tokens. Payload is `record NotificationPayload(Title, Body, Data?)`. |
| Impl | `src/TrueCapture.Infrastructure/Services/FirebaseFcmSender.cs` — singleton, `Lazy<FirebaseMessaging?>` first-use init reads `Firebase:ServiceAccountJsonPath`. Treats missing file, empty file, or `{}` placeholder (no `"service_account"` discriminator) as "not configured" and logs payloads instead of dispatching. Multicasts via `SendEachForMulticastAsync`; surfaces `Unregistered`/`InvalidArgument` tokens for caller-side pruning. |
| Options | `src/TrueCapture.Infrastructure/Services/FirebaseOptions.cs` — binds `appsettings.json#Firebase` (`ServiceAccountJsonPath`, `ProjectId`, `DefaultTopic`). |
| Entity | `src/TrueCapture.Modules.Identity/Entities/UserDevice.cs` (lives in Identity beside `User`) |
| Device service | `src/TrueCapture.Modules.Identity/Services/{IUserDeviceService,UserDeviceService}.cs` — three methods: `RegisterAsync(userId, fcmToken, deviceType)` (op `UserDevice.Register`, transactional) upserts the row (reassigns if the token already belonged to a different user) and best-effort subscribes to `Firebase:DefaultTopic`; `RemoveAsync(userId, fcmToken)` (op `UserDevice.Remove`) unsubscribes and deletes the row; `PruneInvalidAsync(invalidTokens)` bulk-deletes rows whose tokens FCM reported as `Unregistered`/`InvalidArgument`. All topic IO is wrapped in try/catch — FCM outage never fails the parent auth flow. |
| Admin controller | `src/TrueCapture.Modules.Notifications/Controllers/AdminNotificationsController.cs` |
| Admin service | `src/TrueCapture.Modules.Notifications/Services/{IAdminNotificationService,AdminNotificationService}.cs` |
| DI | `src/TrueCapture.Modules.Notifications/Extensions/NotificationsServiceExtensions.cs#AddNotificationsModule` (called from `ApiServiceExtensions.AddTrueCapture`) |

### Data model
- `UserDevice` (`src/TrueCapture.Modules.Identity/Entities/UserDevice.cs`) — `UserId` (long FK), `FcmToken` (unique, ≤ 512 chars), `DeviceType?` ("ios"|"android"|"web", ≤ 16 chars), `LastUsedAtUtc`, plus inherited `BaseEntity` columns. Has `User` reference navigation.
- `User.UserDevices` collection navigation added on the existing `User` entity (`Entities/User.cs`) alongside `UserRoles` and `RefreshTokens`.
- EF mapping (`IdentityModelConfigurator.cs`): table `identity.UserDevice`, unique index on `FcmToken`, index on `UserId`, FK to `User` with `OnDelete(Cascade)`, `RowVersion` as concurrency token.
- Migration: `src/TrueCapture.Infrastructure/Migrations/20260514065601_AddUserDevices.cs` (hand-written matching the `dotnet ef` output for the snapshotted model) — creates `identity.UserDevice` plus the unique `FcmToken` index and the non-unique `UserId` index. Applied automatically by dev startup when `RunMigrationsOnStartup=true`.

### Auth flow changes
Token-issuing endpoints (`/api/auth/{register, login, refresh, verify-otp, google}`) gained optional `fcm_token` + `device_type` fields (see DTOs in `Services/IAuthService.cs`: `RegisterDto`, `LoginDto`, `RefreshDto`, `VerifyOtpAndIssueDto`, `GoogleSignInDto`). `AuthService` (`Services/AuthService.cs`) now also depends on `IUserDeviceService` via constructor injection. After `IssueAndPersistRefresh`, `AuthService` calls `IUserDeviceService.RegisterAsync(userId, fcmToken, deviceType)`. `RegisterAsync` upserts the `UserDevice` row (reassigns to current user if the token previously belonged to someone else) and best-effort subscribes the token to the configured `Firebase:DefaultTopic` (default `"all"`) via `IFcmSender.SubscribeAsync`. Logout accepts an optional `fcm_token` (carried on `RefreshDto`); when set, `AuthService.LogoutAsync(refreshToken, currentUserId, fcmToken?)` calls `IUserDeviceService.RemoveAsync(userId, fcmToken)` so the row is removed and unsubscribed in addition to revoking the refresh token.

### Admin endpoints
Base route `/api/admin/notifications`, all gated by `[AdminOnly]`. Controller: `Controllers/AdminNotificationsController.cs`. DTOs live in `Models/AdminNotificationModels.cs`. Service: `Services/{IAdminNotificationService,AdminNotificationService}.cs` — depends on `AppDbContext`, `IBaseService`, `IFcmSender`, and `IUserDeviceService` (the last is reused from the Identity module for token pruning).

| Method | Route | DTO in | Service |
|---|---|---|---|
| POST | `/send-topic`    | `SendTopicDto(Topic, Title, Body, Data?)` | `AdminNotificationService.SendToTopicAsync` (op `AdminNotifications.SendToTopic`) → `IFcmSender.SendToTopicAsync` |
| POST | `/send-users`    | `SendUsersDto(UserIds[], Title, Body, Data?)` | `AdminNotificationService.SendToUsersAsync` (op `AdminNotifications.SendToUsers`) — selects `UserDevice.FcmToken` where `UserId IN (...)`, batches by 500, multicasts via `IFcmSender.SendToTokensAsync`, then calls `IUserDeviceService.PruneInvalidAsync` for any `Unregistered` / `InvalidArgument` tokens FCM reports back |
| POST | `/send-filtered` | `SendFilteredDto(Search?, IsActive?, IsAdmin?, IsVerified?, HasGoogle?, Title, Body, Data?)` | `AdminNotificationService.SendToFilteredAsync` (op `AdminNotifications.SendToFiltered`) — same filter predicate set as `AdminUsersService.ListAsync`, joined against `UserDevice` to harvest tokens, then same multicast + prune fan-out as `send-users` |

All return `Result<SendNotificationResultDto>` with `{ SentCount, FailedCount, InvalidTokensPruned, TargetedDeviceCount }`.

### Notifications module DI (`Extensions/NotificationsServiceExtensions.cs#AddNotificationsModule`)
- `services.AddScoped<IAdminNotificationService, AdminNotificationService>()`

`ApiServiceExtensions.AddTrueCapture` calls `services.AddNotificationsModule(cfg)` next to `AddUsersModule`. The Notifications module narrowly references `TrueCapture.Modules.Identity` for the `UserDevice` entity + `IUserDeviceService` interface (same pattern as `TrueCapture.Modules.Users → Identity`).

### DI wiring (`IdentityServiceExtensions.AddIdentityModule`)
- `services.Configure<FirebaseOptions>(cfg.GetSection("Firebase"))`
- `services.AddSingleton<IFcmSender, FirebaseFcmSender>()` — singleton; `FirebaseApp` is process-global, lazy-init guarded by `Lazy<FirebaseMessaging?>`.
- `services.AddScoped<IUserDeviceService, UserDeviceService>()`

`ApiServiceExtensions.AddTrueCapture` will gain `services.AddNotificationsModule(cfg)` next to `AddUsersModule` once the Notifications module project lands.

## File Storage (shared) — DONE

Pluggable blob storage. The shipped provider writes to local disk and serves files as static files; production swaps in an S3 provider with no caller changes — see `docs/file-storage.md`.

| Concern | Path |
|---|---|
| Abstraction | `src/TrueCapture.Shared/Services/IFileStorage.cs` — `record StoredFile(Url, Key)` + `SaveAsync(stream, fileName, contentType, folder, ct)` / `DeleteAsync(urlOrKey, ct)` |
| Options | `src/TrueCapture.Infrastructure/Services/StorageOptions.cs` — binds `appsettings.json#Storage` (`Provider`, `PublicBaseUrl`, `Local{RootPath,RequestPath}`, `S3{...}`) |
| Local impl | `src/TrueCapture.Infrastructure/Services/LocalFileStorage.cs` — writes under `{ContentRoot}/storage`, returns URL `{PublicBaseUrl}{RequestPath}/{folder}/{guid}.{ext}` (root-relative when `PublicBaseUrl` is empty); randomised filenames; best-effort `DeleteAsync` |
| Static-file wiring | `src/TrueCapture.Infrastructure/Extensions/StorageStartupExtensions.cs#UseFileStorage` — serves the local root at `RequestPath` (`/media`); no-op for non-Local providers. Called in `Program.cs` after `UseSerilogRequestLogging` |
| DI | `InfrastructureExtensions.AddInfrastructure` — `Configure<StorageOptions>` + `AddSingleton<IFileStorage, LocalFileStorage>` |

Config (`appsettings.json#Storage`): `Provider` (`Local`), `PublicBaseUrl`, `Local:RootPath` (`storage`), `Local:RequestPath` (`/media`), plus an `S3` placeholder block.

---

## Users Module — EXTENDED (self profile + avatar + admin user management)

### Self profile — `UsersController` (`Controllers/UsersController.cs`)
Base route `/api/users`, all `[Authorize]` (any signed-in user). Service: `IUserProfileService` / `UserProfileService`.

| Method | Route | DTO in | Service call |
|---|---|---|---|
| GET    | `/api/users/me`        | — | `UserProfileService.GetAsync(CurrentUserId)` |
| PUT    | `/api/users/me`        | `UpdateProfileRequest(DisplayName?, Bio?, Gender?, AccountType?)` | `UserProfileService.UpdateAsync(CurrentUserId, req)` |
| POST   | `/api/users/me/avatar` | multipart `file` (`IFormFile`), `RateLimitPolicies.Upload` | `UserProfileService.SetAvatarAsync(CurrentUserId, AvatarUpload)` |
| DELETE | `/api/users/me/avatar` | — | `UserProfileService.RemoveAvatarAsync(CurrentUserId)` |

All return `Result<UserProfileResponse>`:
`(Id, Email, Username, DisplayName, AvatarUrl, Bio, JoinedAtUtc, FollowersCount, FollowingCount, PostsCount, IsSuspended, IsBlueTick, AccountType, Gender, EmailVerified, IsAdmin)`.
- `JoinedAtUtc` = `User.CreatedAtUtc`; `IsSuspended` = `!User.IsActive`; `IsBlueTick` = `User.IsVerified`.
- `FollowersCount` / `FollowingCount` / `PostsCount` are read straight from the denormalized counter columns on `User` (see below) — no per-request `COUNT(*)`.
- `AccountType` is a lowercase string (`"public"` / `"private"`); `Gender` is `"male"` / `"female"` / `"other"` / null. Enums map to strings in the DTO — no global `JsonStringEnumConverter`.

**New `User` columns** (`Entities/User.cs`): `Gender? Gender` (enum `Male=1,Female=2,Other=3`, nullable) and `AccountType AccountType` (enum `Public=1,Private=2`, default `Public`). EF mapping in `IdentityModelConfigurator` mirrors the `OtpPurpose` pattern — `HasConversion<int>()`; `AccountType` also `HasDefaultValue(AccountType.Public)`. Migration: `20260521102909_AddUserProfileFields` (nullable `Gender int`, non-null `AccountType int default 1`).

**Denormalized social counters** (`Entities/User.cs`): `FollowersCount`, `FollowingCount`, `PostsCount` (`int`, default `0`). Maintained transactionally — never recomputed on read:
- `SocialService.FollowAsync` / `UnfollowAsync` / `AcceptRequestAsync` shift `FollowersCount` + `FollowingCount` by ±1 **only for accepted edges** (a pending follow request does not count until accepted) via `AdjustFollowCountsAsync` → EF `ExecuteUpdateAsync` (`SET col = col ± 1`), so concurrent follows of the same user don't collide on the row-version token.
- `PostService.CreateAsync` / `RemoveAsync` shift `PostsCount` by ±1 the same way.
- Each adjustment runs inside the same transaction as the follow/post mutation. Migration: `20260521142221_AddUserCounters` adds the three columns and backfills existing rows from the `Follow` / `Post` tables.

`UserProfileService` (`Services/UserProfileService.cs`, implements `IUserProfileService`) — depends on `AppDbContext`, `IBaseService`, `IFileStorage`. All methods take an explicit `userId` so they serve both the self endpoints and the admin avatar endpoints:
- `GetAsync` (op `UserProfile.Get`): loads `User`, `NotFound` if missing.
- `UpdateAsync` (op `UserProfile.Update`, transactional): trims + length-validates (`DisplayName` ≤ 80, `Bio` ≤ 500), full-replace of those columns (blank → null). Parses `Gender` (blank → cleared; unknown → `Validation`) and `AccountType` (blank → unchanged; unknown → `Validation`) via the case-insensitive `ParseEnum<TEnum>` helper.
- `SetAvatarAsync` (op `UserProfile.SetAvatar`, transactional): `AvatarRules.Validate` (≤ 5 MB, JPEG/PNG/WebP) → `IFileStorage.SaveAsync(folder "avatars")` → set `User.AvatarUrl` → best-effort `DeleteAsync` of the previous file.
- `RemoveAvatarAsync` (op `UserProfile.RemoveAvatar`, transactional): clears `AvatarUrl`, best-effort deletes the old file.
- `AvatarUpload(Stream, FileName, ContentType, Length)` — controller-agnostic upload record, built from `IFormFile` in the controller.
- `AvatarRules` (`Services/AvatarRules.cs`) — shared `MaxBytes` (5 MB), allowed content types, `Folder` ("avatars"), `Validate(contentType, length)`.

### Admin user management — `AdminUsersController` (`Controllers/AdminUsersController.cs`)
Existing `GET /api/admin/users` (list) unchanged. New endpoints — all `[AdminOnly]`; mutations also `[RequirePermission("Users.Manage")]`. Controller now also depends on `IUserProfileService` (for the avatar endpoints).

| Method | Route | Auth | DTO in | Service call |
|---|---|---|---|---|
| GET    | `/api/admin/users/{id}`        | `[AdminOnly]` | — | `AdminUsersService.GetDetailAsync(id)` |
| PUT    | `/api/admin/users/{id}`        | + `Users.Manage` | `AdminUpdateUserRequest(DisplayName?, Bio?)` | `AdminUsersService.UpdateAsync(id, req)` |
| POST   | `/api/admin/users/{id}/status` | + `Users.Manage` | `SetUserStatusRequest(IsActive)` | `AdminUsersService.SetStatusAsync(id, isActive)` |
| POST   | `/api/admin/users/{id}/avatar` | + `Users.Manage` | multipart `file`, `RateLimitPolicies.Upload` | `UserProfileService.SetAvatarAsync(id, AvatarUpload)` |
| DELETE | `/api/admin/users/{id}/avatar` | + `Users.Manage` | — | `UserProfileService.RemoveAvatarAsync(id)` |

Detail/update/status return `Result<AdminUserDetail>` — `(Id, Email, Username, DisplayName, AvatarUrl, Bio, EmailVerified, IsActive, IsAdmin, IsVerified, HasGoogle, LastLoginAtUtc, CreatedAtUtc)`. Avatar endpoints return `Result<UserProfileResponse>` (shared service). `SetStatus` rejects with `Validation` when `id == CurrentUserId` (an admin cannot suspend themselves).

`AdminUsersService` (`Services/AdminUsersService.cs`) gained `GetDetailAsync` (op `AdminUsers.GetDetail`), `UpdateAsync` (op `AdminUsers.Update`, transactional), `SetStatusAsync` (op `AdminUsers.SetStatus`, transactional) + a `MapDetail` projector.

DI (`Extensions/UsersServiceExtensions.cs#AddUsersModule`): added `services.AddScoped<IUserProfileService, UserProfileService>()`.

No DB migration — `User.AvatarUrl/DisplayName/Bio` already exist; no new permission — reuses the seeded `Users.Manage`.

## Social Module — DONE (search, follow graph, profiles, posts)

New project `TrueCapture.Modules.Social` (references Shared, Infrastructure, Identity; registered via `AddSocialModule` in `ApiServiceExtensions`). One `SocialModelConfigurator` (`IEntityModelConfigurator`).

### Entities
- `Follow` (`Entities/Follow.cs`) — `FollowerId`, `FolloweeId`, `Status` (`FollowStatus` enum `Accepted=1, Pending=2`). Table `social.Follow`; unique index `(FollowerId, FolloweeId)`; FKs to `User` (cascade).
- `Post` (`Entities/Post.cs`) — `AuthorId`, `ImageUrl`, `Caption?` (≤2200). Table `social.Post`; index `(AuthorId, Id)`; FK to `User` (cascade).

### Endpoints — all `[Authorize]`, return `Result<T>`
| Method | Route | Service | Notes |
|---|---|---|---|
| GET    | `/api/users/search?q=&limit=` | `SocialService.SearchAsync` | username/displayName substring, excl. self; items carry ≤2 `mutualFollowers` + count + `followState` |
| GET    | `/api/users/{id}` | `SocialService.GetProfileAsync` | `UserProfileView` — counts, `followState`, `followsMe`, `isMe`, `canViewContent` |
| POST   | `/api/users/{id}/follow` | `SocialService.FollowAsync` | public → `Accepted`; private → `Pending`; FCM push to `{id}` |
| DELETE | `/api/users/{id}/follow` | `SocialService.UnfollowAsync` | unfollow / cancel request |
| GET    | `/api/users/{id}/followers` `following` `posts` | `SocialService.*` | 403 when `!canViewContent` (private + not following) |
| GET    | `/api/follow/requests` | `SocialService.GetFollowRequestsAsync` | incoming pending |
| POST   | `/api/follow/requests/{requesterId}/accept` `reject` | `SocialService.Accept/RejectRequestAsync` | accept pushes the requester |
| POST   | `/api/posts` | `PostService.CreateAsync` | multipart `file` + `caption`; image via `IFileStorage` folder `posts`; `RateLimitPolicies.Upload` |
| DELETE | `/api/posts/{id}` | `PostService.DeleteAsync` | author-only |
| GET    | `/api/admin/users/{id}/posts` | `PostService.GetByUserAsync` | `[AdminOnly]` — privacy bypassed |
| DELETE | `/api/admin/posts/{id}` | `PostService.AdminDeleteAsync` | `[AdminOnly]` + `[RequirePermission("Posts.Moderate")]` |

Controllers: `SocialController` (search/profile/follow/lists/requests/user-posts), `PostsController` (`/api/posts`), `AdminPostsController` (admin post moderation). Services: `SocialService`, `PostService` — wrap `IBaseService.ExecuteAsync`. `canViewContent` = self OR public OR accepted-follower is the single privacy gate. Mutual followers = accepted-follows whose follower the viewer also follows. Cursor pagination is keyset by descending id (`Services/Paging.cs`).

Follow-event pushes go through the new `IUserDeviceService.PushToUserAsync(userId, NotificationPayload)` (Identity) — best-effort, queries the user's `UserDevice` tokens → `IFcmSender.SendToTokensAsync` → prunes invalid. Data payload `type` = `follow_request` / `new_follower` / `follow_accepted`.

`UserProfileService`, `SocialService.GetProfileAsync`, and `AdminUsersService` read follower/following/post counts straight from the denormalized `User.FollowersCount` / `FollowingCount` / `PostsCount` columns (maintained by `SocialService` / `PostService` — see the Users module section); `AdminUserDetail` also exposes `AccountType` + the three counts.

## Notifications Module — EXTENDED (in-app notices + per-user admin messaging)

- New entity `AppNotice` (`Entities/AppNotice.cs`) — `RecipientUserId`, `Title`, `Body`, `IsRead`. Table `notifications.AppNotice` via the new `NotificationsModelConfigurator`.
- `INoticeService` / `NoticeService` — the user's in-app inbox. Endpoints (`NoticeController`, `[Authorize]`): `GET /api/notices?cursor=`, `GET /api/notices/unread-count`, `POST /api/notices/{id}/read`.
- `IAdminUserMessagingService` / `AdminUserMessagingService` — per-user admin messaging via `AdminUserMessagingController` (`[Route("api/admin/users")]`, `[AdminOnly]` + `[RequirePermission("Users.Manage")]`):
  - `POST /api/admin/users/{id}/notify` — FCM push (`SendUserNotificationDto`).
  - `POST /api/admin/users/{id}/notice` — creates an `AppNotice` row (`SendUserNoticeDto`).
  - `POST /api/admin/users/{id}/email` — emails the user via `IEmailSender` (`SendUserEmailDto`).
- DI in `AddNotificationsModule`: `INoticeService`, `IAdminUserMessagingService`, `NotificationsModelConfigurator`.

### Migration & permissions
One migration `20260521112647_AddSocialAndNotices` (tables `Follow`, `Post`, `AppNotice`). No new permission codes — follow/post/notice user actions need only `[Authorize]`; admin messaging reuses `Users.Manage`; admin post deletion reuses the seeded `Posts.Moderate`.

## Create Post Module — BACKEND DONE (media pipeline, posts, feed, engagement, moderation)

Adds two post types — **Normal** (Home tab) and **Fake vs Real** (dedicated tab) — built
on a signed-URL media pipeline. All entities live in `TrueCapture.Modules.Social`
(schema `social`).

### Data model
- `MediaAsset` (`Entities/MediaAsset.cs`) — one uploaded photo/video. `OwnerId`,
  `Kind {Photo,Video}`, `Status {Pending,Ready,Failed}`, `StorageKey`, `Url`,
  `ThumbnailUrl`, `MimeType`, `ByteSize`, `DurationSeconds?`, `Width?`/`Height?`,
  `CaptureMetadata` (jsonb), `ErrorCode?`.
- `Post` (rewritten) — `Type {Normal,FakeVsReal}`, `Kind {Photo,Carousel,Video}`,
  `IsAdminPost`, `Status {Live,PendingReview,Removed}`, `RemovalReason?`, `Caption?`,
  `CoverUrl` (first-media thumbnail), `ShareId` (slug for `domain/post/{shareId}`),
  counters `View/Likes/Comments/Shares/TrueVotes/FalseVotes`.
- Join/edge tables: `PostMedia(PostId,MediaAssetId,Position)`,
  `PostReference(PostId,Url,Position)` (Fake-vs-Real reference links),
  `PostMention(PostId,MentionedUserId)`, `PostVote(PostId,UserId,Value)` unique,
  `PostView(PostId,ViewerId)` unique, `PostSave(UserId,PostId)` unique,
  `PostShare(UserId,PostId)`, `PostReport(PostId,ReporterId,Reason,OtherText?,Status)`,
  `CommentLike(CommentId,UserId)` unique.
- `Comment` gains `ParentCommentId?` (1-level replies) and `IsRemoved`.
- `User` (Identity) gains `CanPostFakeVsReal`, `CreatorScore`,
  `FvrCandidateNotifiedAtUtc` — `IsVerified` (blue tick) is set together with
  `CanPostFakeVsReal` when an admin grants access.

### Routes & flow

All routes are `[Authorize]` unless noted. Flow: Controller → Service (`IBaseService.ExecuteAsync`
+ `Result<T>`) → `AppDbContext` (schema `social`).

**Media pipeline** — `MediaController` (`api/media`) → `IMediaService` / `MediaService`:
| Method | Route | Service | Notes |
|---|---|---|---|
| POST | `/api/media/uploads` | `RequestUploadAsync` | body `RequestUploadDto{mimeType,byteSize,kind}`; MIME allow-list (jpeg/png/webp, mp4/quicktime — GIF/audio rejected); 25 MB photo / 200 MB video → `413`; reserves slot + `MediaAsset(status=pending)`; returns `UploadTicket{uploadId,putUrl,expiresAtUtc}` |
| PUT  | `/api/media/blob/{uploadId}` | `StoreBlobAsync` | raw body bytes → `IFileStorage.WriteAsync`; `RateLimitPolicies.Upload`; `[DisableRequestSizeLimit]` |
| POST | `/api/media/finalize` | `FinalizeAsync` | body `FinalizeUploadDto{uploadId,captureMetadata}`; verifies bytes, sets `status=ready` (+ photo thumbnail); HLS transcoding deferred |

`IFileStorage` gained `ReserveSlot` / `WriteAsync` / `ExistsAsync` (Shared + `LocalFileStorage`).

**Posts** — `PostsController` → `IPostService` / `PostService` + `IEngagementService` + `IPostModerationService`:
| Method | Route | Service | Notes |
|---|---|---|---|
| POST | `/api/posts` | `PostService.CreateAsync` | body `CreatePostRequest{type,mediaAssetIds,caption,references}`; validates media ready+owned, derives `Kind`, rejects mixed photo/video; Fake-vs-Real needs `User.CanPostFakeVsReal` + caption + ≥1 reference; parses `@mentions` (public/followed only); `RateLimitPolicies.Upload` |
| GET | `/api/posts/{id}` | `EngagementService.GetPostAsync` | full `PostDto`; records a distinct `PostView` + bumps `ViewCount`; Fake-vs-Real bypasses the privacy gate |
| DELETE | `/api/posts/{id}` | `PostService.DeleteAsync` | author-only; FK-cascades all dependents |
| POST | `/api/posts/{id}/like` `save` `share` | `Engagement.Toggle*/ShareAsync` | like/save toggle; share records `PostShare` + returns `domain/post/{shareId}` |
| POST | `/api/posts/{id}/vote` | `Engagement.VoteAsync` | body `VoteRequest{value}`; Fake-vs-Real only; one vote/user; maintains `True/FalseVotesCount` |
| POST | `/api/posts/{id}/report` | `PostModeration.ReportPostAsync` | body `ReportPostRequest{reason,otherText}` |
| GET/POST | `/api/posts/{id}/comments` | `Engagement.GetComments/AddCommentAsync` | `AddCommentRequest{text,parentCommentId}`; reply-to-reply rejected (1 level) |
| GET | `/api/comments/{id}/replies` | `Engagement.GetRepliesAsync` | |
| POST | `/api/comments/{id}/like` | `Engagement.ToggleCommentLikeAsync` | |
| DELETE | `/api/comments/{id}` | `Engagement.DeleteCommentAsync` | author or post owner; soft delete (`IsRemoved`) |
| GET | `/api/users/me/saves` | `Engagement.GetSavedAsync` | the caller's bookmarks |
| GET | `/api/feed?channel=&cursor=` | `FeedService.GetAsync` | `FeedController`; `channel=home` (Normal) or `fake_vs_real`; keyset cursor |

**Admin** — `AdminPostsController` (`[AdminOnly]`):
| Method | Route | Service | Permission |
|---|---|---|---|
| POST | `/api/admin/posts` | `PostService.AdminCreateAsync` | `Posts.Create` — publishes a Fake-vs-Real post (`is_admin_post`) |
| GET | `/api/admin/users/{id}/posts` | `PostService.GetByUserAsync` | `[AdminOnly]` |
| DELETE | `/api/admin/posts/{id}` | `PostService.AdminDeleteAsync` | `Posts.Moderate` |
| GET | `/api/admin/post-reports?status=` | `PostModeration.ListReportsAsync` | `Posts.Moderate` |
| PATCH | `/api/admin/post-reports/{id}` | `PostModeration.ResolveReportAsync` | `Posts.Moderate` — `dismiss\|removePost\|withholdAccount\|sendNotice` |
| GET | `/api/admin/fvr-candidates` | `PostModeration.ListFvrCandidatesAsync` | `Posts.Moderate` |
| POST | `/api/admin/users/{id}/fake-vs-real-access` | `PostModeration.GrantFvrAccessAsync` | `Posts.Moderate` — sets `CanPostFakeVsReal` + `IsVerified` |

**Creator-score milestone** — `CreatorScoreService` (config `CreatorScoreOptions` — section `CreatorScore`):
recomputes `User.CreatorScore = FollowerWeight·followers + LikeWeight·likesReceived +
UpvoteWeight·fvrUpVotes` after each like / vote; the first time it crosses `Threshold` for a
user without access it stamps `FvrCandidateNotifiedAtUtc` and pushes an FCM alert to the
`admins` topic (best-effort, via `IFcmSender`).

DI in `AddSocialModule`: `IMediaService`, `IFeedService`, `ICreatorScoreService`,
`IPostModerationService` + `CreatorScoreOptions`. Migration `20260522060437_AddCreatePostModule`
adds the 10 new tables, the `Post`/`Comment`/`User` columns, and drops `Post.ImageUrl`.

## Messaging Module — BACKEND DONE (1-to-1 chat, SignalR realtime)

New project `TrueCapture.Modules.Messaging` (schema `messaging`; references Shared,
Infrastructure, Identity). PRD Module 8. Wired via `AddMessagingModule` in
`ApiServiceExtensions`; `AddSignalR()` + `app.MapHub<ChatHub>("/hubs/chat")`.

### Data model
- `Conversation` — denormalized `LastMessageAtUtc` / `LastMessagePreview` /
  `LastMessageSenderId`.
- `ConversationParticipant(ConversationId, UserId, IsPinned, PinnedAtUtc?,
  LastReadMessageId?, UnreadCount)` — unique `(ConversationId,UserId)`; per-user
  pin + read state.
- `Message(ConversationId, SenderId, Type {Text,Image,Video}, Text?, MediaUrl?,
  ThumbnailUrl?, MediaWidth?/Height?, ReplyToMessageId?)` — soft-deletable.
- `MessageReaction(MessageId, UserId, Emoji)` — unique `(MessageId,UserId)`,
  one reaction per user (replace / clear).
- `MessagingModelConfigurator`; migration `AddMessagingModule`.

### Realtime — SignalR `ChatHub` (`/hubs/chat`)
Push-only typed hub (`Hub<IChatClient>`). Each connection joins group
`user:{userId}` on connect. JWT over WebSocket: `JwtBearerEvents.OnMessageReceived`
reads `?access_token=` for `/hubs` paths. Server→client events (`IChatClient`):
`ReceiveMessage`, `MessageRead`, `ReactionUpdated`. `IChatNotifier` / `ChatNotifier`
fans events out over SignalR (`IHubContext<ChatHub,IChatClient>`) **and** FCM data
pushes (`{type:'message',conversationId,messageId,senderName}`) to the recipient's
`UserDevice` tokens — best-effort.

### Routes & flow (`[Authorize]`; Controller → Service → `AppDbContext`)
| Method | Route | Service |
|---|---|---|
| GET  | `/api/conversations?cursor=` | `ConversationService.ListAsync` (recency cursor) |
| POST | `/api/conversations` `{userId}` | `GetOrCreateDirectAsync` |
| GET  | `/api/conversations/{id}/messages?cursor=` | `MessageService.ListAsync` (newest-first keyset) |
| POST | `/api/conversations/{id}/messages` | `SendAsync` — `{type,text?,mediaUrl?,thumbnailUrl?,mediaWidth?,mediaHeight?,replyToMessageId?}`; persists, bumps conversation + recipient `UnreadCount`, broadcasts + FCM |
| POST | `/api/conversations/{id}/read` `{lastMessageId}` | `MarkReadAsync` — zeroes unread, emits `MessageRead` |
| POST | `/api/conversations/{id}/pin` `{pinned}` | `PinAsync` — rejects a 4th pin |
| POST | `/api/messages/{id}/react` `{emoji}` | `MessageService.ReactAsync` (empty emoji clears) |
| DELETE | `/api/messages/{id}` | `DeleteAsync` — sender-only soft delete |

Media reuses the existing signed-URL pipeline (`/api/media/*`) — the client
uploads image/video (compressed, ≤10 MB) and a video thumbnail, then sends the
message with the resulting URLs. DTOs in `Models/MessagingModels.cs`
(`ConversationDto`, `MessageDto`, `ReactionDto`, `MessageReplyDto`, …).

## Pending Modules

- Captures, full media transcoding (HLS) — not yet implemented; document each as it lands.
