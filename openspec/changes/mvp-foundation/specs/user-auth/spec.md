## ADDED Requirements

### Requirement: Email + password registration
The system SHALL allow a new user to register with email, username, and password. The registration endpoint SHALL be `POST /api/auth/register`, require captcha and rate limiting, hash the password with BCrypt (work factor ≥ 12), and return a `Result<AuthTokensDto>` containing access token, refresh token, and access expiry. Email and username SHALL be normalized (lowercase + trim) before uniqueness checks.

#### Scenario: Successful registration
- **WHEN** a client POSTs `/api/auth/register` with a unique email, valid username, and password meeting the minimum policy
- **THEN** the system creates a `User` row with `EmailVerified=false`, `IsActive=true`, issues access + refresh tokens, persists the refresh-token hash, and returns `200 OK` with the tokens

#### Scenario: Duplicate email or username
- **WHEN** a client POSTs `/api/auth/register` with an email or username already in use
- **THEN** the system returns `409 Conflict` with a `Result.Conflict` payload and creates no row

#### Scenario: Missing captcha token
- **WHEN** a client POSTs `/api/auth/register` without a valid captcha header
- **THEN** the system rejects the request with `400 Bad Request` before invoking the service

#### Scenario: Rate limit exceeded
- **WHEN** a client exceeds the `RateLimitPolicies.Auth` policy from a single IP
- **THEN** subsequent requests within the window return `429 Too Many Requests`

### Requirement: Email + password sign-in
The system SHALL allow a registered user to sign in with email and password via `POST /api/auth/login`. The endpoint SHALL be captcha-gated and rate-limited, verify the password with BCrypt, reject inactive accounts, update `LastLoginAtUtc`, and return `Result<AuthTokensDto>`.

#### Scenario: Successful sign-in
- **WHEN** a client POSTs `/api/auth/login` with valid credentials for an active account
- **THEN** the system updates `LastLoginAtUtc`, issues fresh access + refresh tokens, persists the refresh-token hash with the request's User-Agent and IP, and returns `200 OK`

#### Scenario: Wrong password
- **WHEN** a client POSTs `/api/auth/login` with a known email but an incorrect password
- **THEN** the system returns `401 Unauthorized` with `Result.Unauthorized` and does **not** reveal whether the email exists

#### Scenario: Inactive account
- **WHEN** a client signs in for a user whose `IsActive=false`
- **THEN** the system returns `401 Unauthorized`

### Requirement: Email OTP verification and resend
The system SHALL support email-OTP verification for account activation and password reset via `POST /api/auth/send-otp` and `POST /api/auth/verify-otp`. OTP codes SHALL be 6 digits, single-use, expire in 10 minutes, and be rate-limited per email (max 5 sends per hour).

#### Scenario: Send OTP
- **WHEN** a client POSTs `/api/auth/send-otp` with an existing email
- **THEN** the system generates a 6-digit OTP, stores its hash with a 10-minute expiry, and dispatches it to the user's email via the configured SMTP provider

#### Scenario: Verify OTP successfully
- **WHEN** a client POSTs `/api/auth/verify-otp` with the correct email and OTP before expiry
- **THEN** the system marks the OTP used, sets `User.EmailVerified=true` if the OTP was for verification, and returns `Result<AuthTokensDto>` with fresh tokens

#### Scenario: Verify expired OTP
- **WHEN** a client submits an OTP after its 10-minute expiry
- **THEN** the system returns `400 Bad Request` and does not issue tokens

#### Scenario: Resend rate limit
- **WHEN** the same email requests a 6th OTP within one hour
- **THEN** the system returns `429 Too Many Requests`

### Requirement: Forgot password and reset
The system SHALL allow a user to request a password reset via `POST /api/auth/forgot-password` (sends OTP) and complete it via `POST /api/auth/reset-password` (consumes OTP + new password). The reset endpoint SHALL invalidate all existing refresh tokens for the user.

#### Scenario: Reset password
- **WHEN** a client POSTs `/api/auth/reset-password` with a valid OTP and a new password meeting the policy
- **THEN** the system updates `User.PasswordHash`, revokes every active `RefreshToken` for the user, and returns `200 OK`

#### Scenario: Unknown email on forgot-password
- **WHEN** a client POSTs `/api/auth/forgot-password` with an email that does not exist
- **THEN** the system returns `200 OK` (to avoid email enumeration) and sends no email

### Requirement: Google OAuth sign-in
The system SHALL allow sign-in with Google via `POST /api/auth/google` accepting a Google ID token. On first sign-in the system SHALL create a `User` linked by `GoogleSubject`; on subsequent sign-ins it SHALL match the existing user by `GoogleSubject` or by verified email.

#### Scenario: New Google user
- **WHEN** a client posts a verified Google ID token for an email not present in `users`
- **THEN** the system creates a `User` with `EmailVerified=true`, `GoogleSubject = <sub claim>`, no password hash, and returns `AuthTokensDto`

#### Scenario: Returning Google user
- **WHEN** a client posts a verified Google ID token whose `sub` matches an existing `User.GoogleSubject`
- **THEN** the system updates `LastLoginAtUtc` and returns `AuthTokensDto` without creating a duplicate user

#### Scenario: Invalid Google token
- **WHEN** a client posts a Google ID token that fails signature or audience validation
- **THEN** the system returns `401 Unauthorized`

### Requirement: Refresh-token rotation
The system SHALL rotate refresh tokens on every refresh via `POST /api/auth/refresh`. A used or revoked refresh token SHALL never be re-usable.

#### Scenario: Successful refresh
- **WHEN** a client POSTs `/api/auth/refresh` with an active, unexpired refresh token
- **THEN** the system marks the old token revoked, issues a new access + refresh token, sets the old row's `ReplacedByHash`, and returns the new `AuthTokensDto`

#### Scenario: Replay of revoked token
- **WHEN** a client posts a refresh token whose `RevokedAtUtc` is set
- **THEN** the system returns `401 Unauthorized` and does **not** issue new tokens

### Requirement: Logout
The system SHALL allow an authenticated user to revoke a refresh token via `POST /api/auth/logout`. The endpoint SHALL be idempotent.

#### Scenario: Logout success
- **WHEN** an authenticated client posts a refresh token it owns
- **THEN** the system sets `RevokedAtUtc=now` on the matching row and returns `200 OK`

#### Scenario: Logout with unknown token
- **WHEN** an authenticated client posts a refresh token that no longer exists
- **THEN** the system returns `200 OK` (idempotent — no error)

### Requirement: Mobile endpoint paths align to backend
The Flutter mobile app SHALL use the canonical backend paths (`/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`, `/api/auth/send-otp`, `/api/auth/verify-otp`, `/api/auth/forgot-password`, `/api/auth/reset-password`, `/api/auth/google`) in `core/constants/api_endpoints.dart`. Legacy `/auth/sign-in` style constants SHALL be removed.

#### Scenario: No legacy paths remain
- **WHEN** a developer greps `lib/` for `/auth/sign-in`, `/auth/sign-up`, `/auth/refresh-token`, or `/auth/sign-out`
- **THEN** there are no matches
