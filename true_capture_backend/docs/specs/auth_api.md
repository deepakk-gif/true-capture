# Spec — Auth API Gaps (Firebase Google sign-in, Email OTP, Password Reset)

Companion to [`application_stracture.md`](application_stracture.md). Extends the `TrueCapture.Modules.Identity` module with the auth endpoints PRD §6.1 / Module 1 still requires.

## Context

PRD Module 1 (Authentication) requires:

- Email + password sign up / sign in — **already built** (`AuthController.Register`, `Login`, `Refresh`, `Logout`)
- Google sign in — **missing**
- Email OTP for verification + password reset — **missing**
- JWT access + refresh token — **already built**

This spec covers only the missing pieces. The existing Identity module is the reference implementation; we extend without restructuring.

**Decisions:**
- Mobile (Flutter) and Web (Next.js) both use Firebase Auth client-side. Backend receives a Firebase **ID token** at one endpoint and verifies it.
- Email OTP and password-reset emails go through our own API via SMTP. Dev → MailHog (already in `ops/docker-compose.yml`); prod swaps SMTP provider via config.
- OTP: 6-digit numeric, 10-min expiry, max 5 verification attempts.

---

## Endpoints to add

All under `AuthController` (`/api/auth/*`). All return `Result<T>` mapped via `ToActionResult()`. All public endpoints use the `auth` rate-limit policy (5/min) and `[RequireCaptcha]`.

| Verb | Route | DTO in | DTO out | Auth | Captcha | Notes |
|---|---|---|---|---|---|---|
| `POST` | `/api/auth/firebase` | `FirebaseSignInDto(IdToken)` | `AuthTokensDto` | Anonymous | Yes | Verifies Firebase ID token, links to existing user by email or creates new user; returns our JWT + refresh token |
| `POST` | `/api/auth/email/send-verification` | `SendEmailOtpDto(Email)` | `Result` (empty) | Anonymous | Yes | Generates OTP, stores in Redis, emails it. Always returns 200 (don't leak account existence) |
| `POST` | `/api/auth/email/verify` | `VerifyEmailOtpDto(Email, Otp)` | `Result` (empty) | Anonymous | Yes | Validates OTP, sets `User.EmailVerified = true`. Increments attempt counter |
| `POST` | `/api/auth/forgot-password` | `ForgotPasswordDto(Email)` | `Result` (empty) | Anonymous | Yes | Generates reset OTP under separate keyspace, emails it. Always returns 200 |
| `POST` | `/api/auth/reset-password` | `ResetPasswordDto(Email, Otp, NewPassword)` | `Result` (empty) | Anonymous | Yes | Validates OTP, hashes new password, **revokes all active refresh tokens for the user** |

CAPTCHA is on `/firebase` too because it can create accounts. (`[RequireCaptcha]` is currently a marker; enforcement middleware is a separate task.)

---

## New code

### Module: `TrueCapture.Modules.Identity`

**DTOs** (`Dtos/`):
- `FirebaseSignInDto.cs` — `record FirebaseSignInDto(string IdToken)` + FluentValidation validator (non-empty)
- `SendEmailOtpDto.cs` — `record SendEmailOtpDto(string Email)` + validator (email format)
- `VerifyEmailOtpDto.cs` — `record VerifyEmailOtpDto(string Email, string Otp)` + validator (email + 6-digit numeric)
- `ForgotPasswordDto.cs` — `record ForgotPasswordDto(string Email)` + validator
- `ResetPasswordDto.cs` — `record ResetPasswordDto(string Email, string Otp, string NewPassword)` + validator (password ≥ 8 chars, mirrors `RegisterDto`)

**Services**:

- `Services/IFirebaseTokenVerifier.cs` + `FirebaseTokenVerifier.cs`
  - `Task<Result<FirebaseUserInfo>> VerifyAsync(string idToken, CancellationToken ct)`
  - `record FirebaseUserInfo(string Subject, string Email, bool EmailVerified, string? Name, string? PictureUrl, string Provider)`
  - Implementation uses **`FirebaseAdmin` NuGet package** (`FirebaseApp.Create(...)` once in DI, `FirebaseAuth.DefaultInstance.VerifyIdTokenAsync`). Singleton.
  - Reads service-account JSON path from `Firebase:CredentialsPath`; project ID from `Firebase:ProjectId`.

- `Services/IOtpService.cs` + `OtpService.cs` (Redis-backed via existing `ICacheService`)
  - `Task<string> GenerateAsync(OtpPurpose purpose, string email, CancellationToken ct)` — returns the 6-digit code; stores `{ codeHash: SHA256(code), attempts: 0 }` under key `otp:{purpose}:{email}` with 10-min TTL.
  - `Task<Result<bool>> VerifyAsync(OtpPurpose purpose, string email, string code, CancellationToken ct)` — checks hash, increments attempts, returns `Validation` after 5 attempts or on mismatch, deletes key on success.
  - `enum OtpPurpose { EmailVerification, PasswordReset }` — separate keyspaces so a verification OTP can't reset a password.

- `Services/IEmailTemplateRenderer.cs` + `EmailTemplateRenderer.cs` — two simple in-code templates (`EmailVerificationOtp(code)`, `PasswordResetOtp(code)`). Plain HTML strings; no templating engine yet.

- **Extend `IAuthService` / `AuthService`** with:
  - `Task<Result<AuthTokensDto>> SignInWithFirebaseAsync(string idToken, string? userAgent, string? ip, CancellationToken ct)`
    - Verifies via `IFirebaseTokenVerifier`
    - If `User` exists by `Email` → link `GoogleSubject` if null, update `LastLoginAtUtc`, issue tokens
    - If not → create `User` with `EmailVerified = firebaseInfo.EmailVerified`, `GoogleSubject = firebaseInfo.Subject`, random unguessable `PasswordHash` (so password login is disabled), assign `user` role, issue tokens
  - `Task<Result> SendEmailVerificationAsync(string email, CancellationToken ct)` — looks up user (return `Success` even if not found to prevent enumeration), generates OTP, sends email
  - `Task<Result> VerifyEmailAsync(string email, string code, CancellationToken ct)` — verifies OTP, sets `EmailVerified = true`
  - `Task<Result> ForgotPasswordAsync(string email, CancellationToken ct)` — like SendEmailVerification but `PasswordReset` purpose
  - `Task<Result> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct)` — verifies OTP, updates `PasswordHash`, **revokes all active `RefreshToken`s for the user** (sets `RevokedAtUtc = utcNow` for rows where `UserId = user.Id AND RevokedAtUtc IS NULL`)

  All write paths use the existing `BaseService.ExecuteAsync` with `useTransaction: true`.

**Controller** (`Controllers/AuthController.cs`):
- Append five new actions matching the table above. Reuse existing `GetUserAgent()` / `GetIpAddress()` helpers.

### Infrastructure additions

- `TrueCapture.Infrastructure/Email/IEmailSender.cs` + `SmtpEmailSender.cs`
  - `Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken ct)`
  - Implementation uses **`MailKit` NuGet package** (`SmtpClient`). Reads `Smtp:{Host,Port,UseTls,Username,Password,FromAddress,FromName}`.
- `TrueCapture.Infrastructure/Email/SmtpOptions.cs` — bound to `Smtp` config section
- `InfrastructureExtensions.AddInfrastructure`: register `services.Configure<SmtpOptions>(cfg.GetSection("Smtp"))` and `services.AddSingleton<IEmailSender, SmtpEmailSender>()`

### Module DI (`IdentityServiceExtensions.cs`)

```csharp
services.Configure<FirebaseOptions>(cfg.GetSection("Firebase"));
services.AddSingleton<IFirebaseTokenVerifier, FirebaseTokenVerifier>();
services.AddScoped<IOtpService, OtpService>();
services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
```

---

## Configuration changes

`appsettings.json` (template, blank values):
```json
"Firebase": {
  "ProjectId": "",
  "CredentialsPath": ""
},
"Smtp": {
  "Host": "",
  "Port": 587,
  "UseTls": true,
  "Username": "",
  "Password": "",
  "FromAddress": "",
  "FromName": "True Capture"
},
"Otp": {
  "Length": 6,
  "TtlMinutes": 10,
  "MaxAttempts": 5
}
```

`appsettings.Development.json`:
```json
"Smtp": {
  "Host": "localhost", "Port": 1025, "UseTls": false,
  "FromAddress": "no-reply@truecapture.local"
},
"Firebase": {
  "ProjectId": "true-capture-dev",
  "CredentialsPath": "./firebase-dev-sa.json"
}
```
(MailHog already exposes 1025 from `ops/docker-compose.yml`.)

`.gitignore` — add `firebase-*.json` so service-account keys don't leak.

---

## Packages to add

- `src/TrueCapture.Infrastructure/TrueCapture.Infrastructure.csproj` → `MailKit` (4.x)
- `src/TrueCapture.Modules.Identity/TrueCapture.Modules.Identity.csproj` → `FirebaseAdmin` (3.x)

---

## Critical files

**Reference (don't restructure):**
- `src/TrueCapture.Modules.Identity/Services/AuthService.cs` — extend, don't replace
- `src/TrueCapture.Modules.Identity/Services/TokenService.cs` — reuse `Issue()` and `HashRefreshToken()` as-is
- `src/TrueCapture.Modules.Identity/Entities/User.cs` — `GoogleSubject` and `EmailVerified` already present
- `src/TrueCapture.Modules.Identity/Entities/RefreshToken.cs` — for the bulk revoke on password reset
- `src/TrueCapture.Shared/Services/Result.cs` — return-type convention
- `src/TrueCapture.Infrastructure/Services/BaseService.cs` — `ExecuteAsync` wrapping
- `src/TrueCapture.Shared/Services/ICacheService.cs` — Redis abstraction for OTP storage

**To create:**
- `src/TrueCapture.Modules.Identity/Dtos/{FirebaseSignInDto,SendEmailOtpDto,VerifyEmailOtpDto,ForgotPasswordDto,ResetPasswordDto}.cs`
- `src/TrueCapture.Modules.Identity/Services/{IFirebaseTokenVerifier,FirebaseTokenVerifier,IOtpService,OtpService,IEmailTemplateRenderer,EmailTemplateRenderer}.cs`
- `src/TrueCapture.Modules.Identity/Configuration/FirebaseOptions.cs`
- `src/TrueCapture.Infrastructure/Email/{IEmailSender,SmtpEmailSender,SmtpOptions}.cs`

**To modify:**
- `src/TrueCapture.Modules.Identity/Controllers/AuthController.cs` — 5 new actions
- `src/TrueCapture.Modules.Identity/Services/IAuthService.cs` + `AuthService.cs` — 5 new methods
- `src/TrueCapture.Modules.Identity/Extensions/IdentityServiceExtensions.cs` — DI registrations
- `src/TrueCapture.Infrastructure/Extensions/InfrastructureExtensions.cs` — register `IEmailSender`
- `src/TrueCapture.Api/appsettings.json` + `appsettings.Development.json`
- Both `.csproj` files (NuGet refs)
- `.gitignore`

**No EF migration required** — `User.GoogleSubject`, `User.EmailVerified`, and `RefreshToken` already exist in the schema.

---

## Security notes (load-bearing)

- **No account-existence enumeration:** `send-verification` and `forgot-password` always return 200 regardless of whether the user exists.
- **OTP storage:** store SHA-256 hash of the code, not the code itself (parallels how refresh tokens are already stored).
- **Attempt limit:** 5 failed verifications wipes the OTP and returns generic `Validation`. Re-issue requires a new send.
- **Password reset side effect:** revoke all active refresh tokens for that user — otherwise an attacker holding a stolen refresh token retains access after the reset.
- **Firebase verification:** must validate `aud == ProjectId`, `iss == https://securetoken.google.com/{ProjectId}`, signature against Google's rotating public keys, and `auth_time` not in the future. The `FirebaseAdmin` SDK does all of this; do not roll our own JWT validation.
- **Firebase email trust:** if `firebaseInfo.EmailVerified == true`, mark our `User.EmailVerified = true` on first sign-in (Firebase already verified it).
- **Rate limiting:** all five endpoints use the existing `auth` policy (5/min). Per-email tightening is out of scope.

---

## Tests

**Unit (`TrueCapture.Tests.Unit/Modules/Identity/`):**
- `OtpServiceTests.cs` — generate/verify happy path, wrong-code attempt counter, lockout after 5 attempts, expiry
- `AuthServiceFirebaseTests.cs` — new-user creation, link-existing-by-email, with `IFirebaseTokenVerifier` mocked via NSubstitute
- `AuthServicePasswordResetTests.cs` — reset succeeds, all refresh tokens revoked, wrong OTP returns Validation

**Integration (`TrueCapture.Tests.Integration/Modules/Identity/`):**
- `EmailVerificationFlowTests.cs` — POST send → assert email captured by an `InMemoryEmailSender` test double → POST verify → assert `User.EmailVerified == true`
- `PasswordResetFlowTests.cs` — full forgot → reset → verify old refresh token rejected, new login works
- Register `InMemoryEmailSender` in `WebAppFixture` overriding the real `IEmailSender`. Real Firebase verification is mocked; integration tests assert controller wiring + DTO validation, not Firebase itself.

**Architecture (`TrueCapture.Tests.Arch`):**
- The existing `PublicAuthEndpoints_RequireCaptcha` rule will catch any new public endpoint missing `[RequireCaptcha]` — confirm it covers the new actions.

---

## Verification (end-to-end)

1. `docker compose -f ops/docker-compose.yml up -d` — Postgres, Redis, MailHog (UI 8025, SMTP 1025)
2. Drop a Firebase service-account JSON at `src/TrueCapture.Api/firebase-dev-sa.json`
3. `dotnet test src/TrueCapture.Tests.Unit src/TrueCapture.Tests.Integration src/TrueCapture.Tests.Arch`
4. `dotnet run --project src/TrueCapture.Api`
5. Manual via Swagger (`/swagger`) or curl:
   - `POST /api/auth/register` (existing) → got tokens
   - `POST /api/auth/email/send-verification` → check MailHog UI at `http://localhost:8025` for the OTP
   - `POST /api/auth/email/verify` with the OTP → 200; `SELECT "EmailVerified" FROM identity."User" WHERE "Email"=...` → `true`
   - `POST /api/auth/forgot-password` → MailHog shows reset OTP → `POST /api/auth/reset-password` → 200; old refresh token now rejected by `/auth/refresh`; `/auth/login` with new password works
   - `POST /api/auth/firebase` with a real Firebase ID token from a Flutter test client → returns our JWT + refresh; subsequent `/auth/refresh` works

---

## Out of scope

- CAPTCHA enforcement middleware (attribute is a marker today; build later)
- Per-email/IP rate limiting beyond the existing 5/min `auth` window
- Two-factor auth
- Magic links (chose 6-digit OTP)
- Web-side server OAuth redirect flow (web also uses Firebase)
- Admin user auto-seeding (separate task)
