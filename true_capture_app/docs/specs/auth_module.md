# Auth Module Spec — Mobile App

Source PRD: `prd.md` §6.1 + Module 1 + §10
Target surface: `true_capture_app/` (Flutter)
Status: Phase 1 / MVP

---

## 1. Scope

The Auth module owns everything required to take a user from the splash screen to a signed-in `Home` session, plus everything required to keep that session alive.

**In scope**
- Email + password sign up
- Email + password sign in
- Google sign in (single social provider for MVP)
- Email OTP verification (account verification + password reset)
- Forgot / reset password
- JWT access token + refresh token handling
- Sign out (local + remote)
- Session bootstrap on cold start (splash decision)

**Out of scope (for MVP)**
- Facebook / Apple sign in (scaffolded but disabled)
- Phone-number / SMS OTP
- Biometric unlock to re-enter app (UI scaffold exists; activation deferred)
- Two-factor auth beyond email OTP

---

## 2. User-facing Flows

### 2.1 Cold start
1. App opens → `SplashScreen` runs `SplashViewmodel.setupBeforeStart`.
2. Read `access_token` from secure storage.
   - **No token** → `intro` (first install) or `sign_in` (intro already seen).
   - **Token present** → call `GET /user/profile` to validate.
     - 200 → cache user → `main`.
     - 401 / network → `sign_in`.

### 2.2 Sign up (email)
1. User enters name, email, password (min 8 chars).
2. `POST /auth/sign-up` → backend creates account in **unverified** state and dispatches OTP email.
3. App pushes `otp_verify` with `email` extra.
4. On successful OTP → user is verified, tokens issued, navigate to `main`.

### 2.3 Sign in (email)
1. User enters email + password.
2. `POST /auth/sign-in` →
   - 200 → store tokens, cache user, navigate to `main`.
   - 401 → inline form error ("Invalid email or password").
   - 403 unverified → push `otp_verify` with `email` (resend OTP automatically).

### 2.4 Sign in (Google)
1. User taps Google button.
2. `SocialLoginService.signIn(SocialUserType.google)` returns id token + email + name.
3. `POST /auth/social-login` with `provider=google`.
4. Backend creates the user if missing, returns tokens. Navigate to `main`.

### 2.5 Forgot password
1. User enters email on `forgot_password`.
2. `POST /auth/forgot-password` → backend dispatches OTP.
3. Push `otp_verify` with `email` and a flag indicating reset flow.
4. On verified OTP → push `reset_password` (TODO — see §9.3).
5. `POST /auth/reset-password` with new password → return to `sign_in`.

### 2.6 OTP verify
- 6-digit numeric code.
- `POST /auth/verify-otp` returns full `AuthResponse` for sign-up flow, or a short-lived reset token for forgot-password flow.
- Resend allowed after a 30s cooldown (UI counter; backend enforces too).

### 2.7 Sign out
1. User taps Sign out in `tab_settings`.
2. Call `POST /auth/sign-out` (best-effort; ignore failure).
3. Clear `access_token`, `refresh_token`, `user_id` from secure storage.
4. Reset `AuthStateNotifier` state.
5. Navigate to `sign_in`.

### 2.8 Session refresh
- `AuthInterceptor` attaches `Authorization: Bearer <access_token>` to every request.
- On 401 from any endpoint:
  1. Pause request.
  2. Call `POST /auth/refresh-token` with `refresh_token`.
  3. On success → store new tokens → retry original request.
  4. On failure → clear session → emit "force sign-out" event → navigate to `sign_in`.
- Refresh is single-flight (concurrent 401s share one refresh future).

---

## 3. Screens & Routes

| Screen | Path constant | File |
|---|---|---|
| Splash | `ScreenPath.routeSplash` | `presentation/screens/splash/splash_screen.dart` |
| Intro | `ScreenPath.routeIntro` | `presentation/screens/intro/intro_screen.dart` |
| Sign in | `ScreenPath.routeSignIn` | `presentation/screens/auth/sign_in/sign_in_screen.dart` |
| Sign up | `ScreenPath.routeSignUp` | `presentation/screens/auth/sign_up/sign_up_screen.dart` |
| Forgot password | `ScreenPath.routeForgotPassword` | `presentation/screens/auth/forgot_password/forgot_password_screen.dart` |
| OTP verify | `ScreenPath.routeOtpVerify` | `presentation/screens/auth/otp/otp_verify_screen.dart` |
| Main (post-auth) | `ScreenPath.routeMain` | `presentation/screens/main/main_screen.dart` |

Navigation uses `AppRouter.go` for terminal transitions (splash → intro/main, OTP → main) and `AppRouter.push` when the user can step back (sign_in → sign_up, sign_in → forgot_password).

---

## 4. State & Providers

| Provider | Type | Purpose |
|---|---|---|
| `localStorageServiceProvider` | `Provider<LocalStorageService>` | Secure storage wrapper |
| `authStateNotifierProvider` | `StateNotifierProvider<AuthStateNotifier, UserResponse?>` | Current user + token write/clear |
| `authRepo` | `Provider<AuthRepository>` | API calls |
| `splashVm` / `signInViewModelProvider` / `signUpViewModelProvider` / `forgotPasswordViewModelProvider` / `otpViewModelProvider` | `Provider.autoDispose` | One ViewModel per screen |

`AuthStateNotifier`:
- `saveToken(access, {refresh})` — writes to secure storage.
- `setUser(UserResponse)` — caches profile + writes `user_id`.
- `clear()` — wipes tokens + user state. Called by sign-out and forced refresh failure.

---

## 5. Storage Keys (`StorageKeys`)

| Key | Purpose | Lifetime |
|---|---|---|
| `access_token` | JWT access | overwritten on every login/refresh |
| `refresh_token` | JWT refresh | overwritten on every login/refresh |
| `user_id` | Active user id | cleared on sign-out |
| `check_first_intro_done_value` | Skip intro on next cold start | persistent |
| `check_first_sign_up_done` | Tracks first successful sign-up | persistent |

Storage is `flutter_secure_storage` (Keychain on iOS, EncryptedSharedPreferences on Android).

---

## 6. API Contracts

All endpoints under `AppConfig.baseUrl`. All requests/responses JSON. Error envelope: `{ "message": "...", "code": "..." }`.

### 6.1 `POST /auth/sign-up`
Request:
```json
{ "name": "string", "email": "string", "password": "string", "phone": "string?" }
```
Response 200: `AuthResponse` (tokens may be empty if backend requires OTP first).

### 6.2 `POST /auth/sign-in`
Request:
```json
{ "email": "string", "password": "string", "fcm_token": "string?" }
```
Response 200: `AuthResponse`.
Response 401: invalid credentials. Response 403: `{ "code": "unverified" }`.

### 6.3 `POST /auth/social-login`
Request:
```json
{ "provider": "google", "id_token": "string", "email": "string?", "name": "string?", "fcm_token": "string?" }
```
Response 200: `AuthResponse`.

### 6.4 `POST /auth/send-otp`
Request: `{ "email": "string" }`. Response 204.

### 6.5 `POST /auth/verify-otp`
Request: `{ "email": "string", "otp": "string" }`. Response 200: `AuthResponse`.

### 6.6 `POST /auth/forgot-password`
Request: `{ "email": "string" }`. Response 204.

### 6.7 `POST /auth/reset-password`
Request: `{ "email": "string", "otp": "string", "new_password": "string" }`. Response 204.

### 6.8 `POST /auth/refresh-token`
Request: `{ "refresh_token": "string" }`. Response 200: `AuthResponse` (with rotated refresh token).

### 6.9 `POST /auth/sign-out`
Request: empty. Header `Authorization: Bearer <access>`. Response 204. Best-effort — failure does not block local sign-out.

### 6.10 Shared models
- `AuthResponse` → `{ access_token, refresh_token, user: UserResponse? }`
- `UserResponse` → `{ id, email, name?, phone?, avatar_url?, profile_status? }`

Request DTOs live in `lib/network/dto/request/auth/`. Response DTOs in `lib/network/dto/response/auth/`.

---

## 7. Validation Rules (client-side)

| Field | Rule |
|---|---|
| Email | non-empty, basic regex `^[^@\s]+@[^@\s]+\.[^@\s]+$` |
| Password (sign in) | non-empty |
| Password (sign up / reset) | min 8 characters |
| Name (sign up) | non-empty, max 60 |
| OTP | exactly 6 digits |

Server is the source of truth; client checks are UX-only.

---

## 8. Security

| Concern | Treatment |
|---|---|
| Token storage | `flutter_secure_storage` (encrypted) |
| Token in transit | HTTPS only (`AppConfig.baseUrl` enforces `https`) |
| Token in logs | `LoggingInterceptor` must redact `Authorization` headers and `*_token` fields (TODO — currently logs full URI/body) |
| OTP brute-force | Rate-limited server-side per PRD §10; client shows generic "Invalid code" message |
| Sign-in brute-force | Server rate-limit. Client surfaces generic error, never enumerates accounts |
| Sign out | Always wipe local tokens even if remote sign-out fails |
| Cold-start token leak | Tokens never read into widget tree; only via `LocalStorageService` inside ViewModels / interceptors |

---

## 9. Open Items / TODOs

1. **Refresh-token interceptor** is not yet implemented. `AuthInterceptor` attaches the token but does not handle 401 → refresh → retry. Next iteration must add this to `lib/network/interceptors/`.
2. **Google sign-in wiring**: `SocialLoginService.signIn` is a stub. Wire `google_sign_in` package, return real `idToken`.
3. **Reset-password screen** does not yet exist. Add `presentation/screens/auth/reset_password/` and a `routeResetPassword` constant.
4. **Logging redaction**: scrub `Authorization`, `access_token`, `refresh_token`, `password`, `otp` from `LoggingInterceptor`.
5. **OTP resend cooldown UI**: 30s timer + disabled "Resend" button.
6. **Forced sign-out event bus**: when refresh fails mid-request, all in-flight VMs need to bail out. Consider a global `Stream<AuthEvent>` exposed by `AuthStateNotifier`.

---

## 10. Acceptance Criteria

- [ ] First-launch user sees intro → sign-in.
- [ ] Returning signed-in user lands on `main` within 1.5 s on warm start.
- [ ] Sign-up flow: form → OTP → main, with tokens persisted.
- [ ] Sign-in flow: 200 → main; 401 → inline error; 403 → OTP screen.
- [ ] Google sign-in: tap → provider sheet → main (when `SocialLoginService` is wired).
- [ ] Forgot password: email → OTP → reset → sign-in.
- [ ] Sign out: clears tokens, returns to sign-in, and a subsequent app restart goes back to intro/sign-in (not `main`).
- [ ] Tokens never appear in console logs.
- [ ] No crash on flaky network during splash — fall back to sign-in.
