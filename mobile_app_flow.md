# Mobile App Flow — `true_capture_app` (Flutter)

> Auto-maintained doc. Captures the call flow for each completed module:
> Screen (UI) → ViewModel → Repository → ApiService (HTTP) → Backend.
> Update this file whenever a module changes.

## Architecture (shared)

- **State**: Riverpod (`flutter_riverpod`) — VMs exposed via `vm_provider.dart`.
- **Base**: every screen extends `BaseConsumerState<TWidget, TVM>`; every VM extends `BaseViewModel`. Loading/error UI is handled by `ScreenStateAware` + `ScreenState` (idle / apiProgress / error).
- **HTTP**: `services/api_service.dart` (Dio). `network/interceptors/auth_interceptor.dart` injects `Authorization: Bearer <token>` from `LocalStorageService` (`StorageKeys.accessTokenKey`).
- **Routing**: `core/router/app_router.dart` (`AppRouter.go` / `AppRouter.push` + `ScreenPath` constants).
- **Endpoints**: `core/constants/api_endpoints.dart`.
- **Auth state**: `presentation/providers/user_data_provider.dart#AuthStateNotifier` — `saveToken(access, refreshToken)` and `setUser(...)`.

---

## Auth Module — REWORKED (endpoint alignment + OTP / forgot-password / Google)

Routes: `routeSignIn` `/sign-in`, `routeSignUp` `/sign-up`, `routeForgotPassword` `/forgot-password`, `routeOtpVerify` `/otp-verify`, `routeResetPassword` `/reset-password` (new). After success, all flows land on `routeMain` `/main`.

### Endpoint constants (`core/constants/api_endpoints.dart`)
The legacy `/auth/sign-in`-style constants are gone. New canonical names match the backend:

| Constant | Path |
|---|---|
| `register` | `/api/auth/register` |
| `login` | `/api/auth/login` |
| `refresh` | `/api/auth/refresh` |
| `logout` | `/api/auth/logout` |
| `sendOtp` | `/api/auth/send-otp` |
| `verifyOtp` | `/api/auth/verify-otp` |
| `forgotPassword` | `/api/auth/forgot-password` |
| `resetPassword` | `/api/auth/reset-password` |
| `google` | `/api/auth/google` |
| `userProfile` | `/api/users/me` |

Also exports `enum OtpPurpose { verifyEmail(1), passwordReset(2) }` — the wire value must match backend `OtpPurpose` int conversion.

### Request DTOs (`network/dto/request/auth/`)
| File | Maps to backend DTO |
|---|---|
| `sign_in_request.dart` | `LoginDto(Email, Password)` |
| `sign_up_request.dart` | `RegisterDto(Email, Username, Password)` — `name`/`phone` removed |
| `otp_request.dart` | `OtpSendRequest(Email, Purpose)` + `VerifyOtpAndIssueDto(Email, Code, Purpose)` — was `otp` field; now `code` + `purpose` |
| `forgot_password_request.dart` | `ForgotPasswordDto(Email)` |
| `reset_password_request.dart` | `ResetPasswordDto(Email, Code, NewPassword)` |
| `google_sign_in_request.dart` | `GoogleSignInDto(IdToken)` — replaces the generic `social_login_request.dart` for Google |
| `refresh_token_request.dart` | `RefreshDto(RefreshToken)` — used for both `/refresh` and `/logout` |

### Response DTO (`network/dto/response/auth/auth_response.dart`)
- Reads camelCase fields (`accessToken`, `refreshToken`, `accessExpiresAtUtc`) emitted by ASP.NET Core's default `JsonSerializerDefaults.Web`. Snake-case fallback preserved for transition.
- Adds optional `accessExpiresAtUtc DateTime?` so callers can pre-emptively refresh before the token expires.

### Repository (`repositories/auth_repository.dart`)
All methods now hit the canonical paths via the matching DTOs:

| Method | Endpoint | Returns |
|---|---|---|
| `signIn(SignInRequest)` | `POST /api/auth/login` | `AuthResponse` |
| `signUp(SignUpRequest)` | `POST /api/auth/register` | `AuthResponse` |
| `sendOtp(OtpSendRequest)` | `POST /api/auth/send-otp` | `void` |
| `verifyOtp(OtpVerifyRequest)` | `POST /api/auth/verify-otp` | `AuthResponse` |
| `forgotPassword(ForgotPasswordRequest)` | `POST /api/auth/forgot-password` | `void` |
| `resetPassword(ResetPasswordRequest)` | `POST /api/auth/reset-password` | `void` |
| `googleSignIn(GoogleSignInRequest)` | `POST /api/auth/google` | `AuthResponse` |
| `refresh(RefreshTokenRequest)` | `POST /api/auth/refresh` | `AuthResponse` |
| `signOut(RefreshTokenRequest)` | `POST /api/auth/logout` | `void` |
| `getProfile()` | `GET /api/users/me` | `UserResponse` |

### Screens + ViewModels (rebuilt for the new contract)
| Screen | ViewModel | Flow |
|---|---|---|
| `auth/sign_up/sign_up_screen.dart` | `sign_up_view_model.dart` | Fields: **Username** (was Full name), Email, Password → `signUp(username, email, password)` → `AuthRepository.signUp` → `POST /api/auth/register` → push `routeOtpVerify` with `{ email, purpose: verifyEmail }`. |
| `auth/otp/otp_verify_screen.dart` | `otp_view_model.dart` | Takes `email` + `purpose` from route extras. On 6-digit code completion: `verifyEmail` → `repository.verifyOtp` → `AuthStateNotifier.saveToken+setUser` → `go(routeMain)`. `passwordReset` → push `routeResetPassword` with `{ email, code }` (no /verify-otp call — `/reset-password` consumes the OTP itself). |
| `auth/forgot_password/forgot_password_screen.dart` | `forgot_password_view_model.dart` | Email → `AuthRepository.forgotPassword(ForgotPasswordRequest)` → push `routeOtpVerify` with `{ email, purpose: passwordReset }`. |
| `auth/reset_password/reset_password_screen.dart` (NEW) | `reset_password_view_model.dart` (NEW) | Takes `email` + `code` from extras. Form: new password + confirm → `AuthRepository.resetPassword(ResetPasswordRequest)` → `go(routeSignIn)`. |
| `auth/sign_in/sign_in_screen.dart` | `sign_in_viewmodel.dart` | Email + password → `AuthRepository.signIn` → save tokens → main. Social Google button → `AuthMixin.signInWithSocial` (Google flow — see task `#20`). |

### Router (`core/router/app_router.dart`)
- New constant `ScreenPath.routeResetPassword = '/reset-password'`.
- `routeOtpVerify` now reads `{ email, purpose }` from `state.extra`.
- `routeResetPassword` reads `{ email, code }` from `state.extra`.

### Provider wiring (`presentation/providers/vm_provider.dart`)
- New `resetPasswordViewModelProvider` constructed with `authRepo`.

### Google sign-in flow
1. `SignInScreen` Google button → `SignInViewModel.signInWithProvider(context, SocialUserType.google)`.
2. `AuthMixin.signInWithSocial(socialType, authRepository, authStateNotifier, context, onSuccess, onError)` (`mixin/auth_mixin.dart`) — orchestrates the full flow.
3. `SocialLoginService.instance.signIn(SocialUserType.google)` (`services/social_login_service.dart`) — runs `google_sign_in` 6.x: `GoogleSignIn(['email','profile','openid']).signIn()` → `account.authentication.idToken` → returns `SocialLoginResult { provider, idToken, email, name }`.
4. `AuthRepository.googleSignIn(GoogleSignInRequest(idToken))` → `POST /api/auth/google`.
5. `AuthStateNotifier.saveToken(access, refresh) + setUser(user)`.
6. `AppRouter.go(routeMain)`.

Apple / Facebook are wired in `SocialLoginService` but currently short-circuit because the backend exposes only Google.

### Refresh-token interceptor (`network/interceptors/refresh_interceptor.dart`)
Wired in `services/api_service.dart` immediately after `LoggingInterceptor`. On any `401 Unauthorized` that is **not** an `/api/auth/*` call and has not yet been retried:

1. Coalesces concurrent 401s through a single in-flight `_refreshFuture`.
2. Reads `StorageKeys.refreshTokenKey` from secure storage; bails on missing.
3. Uses a **bare Dio** (no interceptor chain) to `POST /api/auth/refresh` with `{ refreshToken }`, avoiding recursion.
4. On success persists new `accessToken` / `refreshToken` via `LocalStorageService` and replays the original request with `Authorization: Bearer <new>` and a `tc_retried_after_refresh` extra flag to guarantee single-retry.
5. On any failure passes the original 401 through to `ErrorInterceptor`.

Interceptor order inside `ApiService.initialize()`:
1. `AuthInterceptor` — attaches the current bearer.
2. `LoggingInterceptor` — request/response logs.
3. `RefreshInterceptor` — 401 recovery.
4. `ErrorInterceptor` — terminal error mapping.

### Original DONE section (kept for history)

### Files

| Layer | Path |
|---|---|
| Screens | `lib/presentation/screens/auth/{sign_in,sign_up,forgot_password,otp}/*_screen.dart` |
| ViewModels | `…/auth/{sign_in,sign_up,forgot_password,otp}/*_view_model.dart` |
| Repository | `lib/repositories/auth_repository.dart` |
| Mixin (social) | `lib/mixin/auth_mixin.dart` |
| Request DTOs | `lib/network/dto/request/auth/{sign_in,sign_up,otp,social_login}_request.dart` |
| Response DTOs | `lib/network/dto/response/auth/{auth,user}_response.dart` |
| Endpoints | `lib/core/constants/api_endpoints.dart` (`signIn`, `signUp`, `signOut`, `socialLogin`, `forgotPassword`, `resetPassword`, `sendOtp`, `verifyOtp`, `refreshToken`) |
| Token injection | `lib/network/interceptors/auth_interceptor.dart` |

### Flows

**Sign In** — `SignInScreen` form (email + password, social buttons, forgot link)
→ `SignInViewModel.signIn` (wraps `executeWithLoading`)
→ `AuthRepository.signIn(SignInRequest)` → `POST /auth/sign-in`
→ `AuthResponse { accessToken, refreshToken, user? }`
→ `AuthStateNotifier.saveToken(...)` + `setUser(...)`
→ `AppRouter.go(routeMain)`.

**Social Login** (Google / Facebook / Apple) — `SignInScreen` icons
→ `SignInViewModel.signInWithProvider`
→ `AuthMixin.signInWithSocial` (currently a stub that logs and calls `onSuccess`)
→ `AppRouter.go(routeMain)`.
> Backend integration: `AuthRepository.socialLogin(SocialLoginRequest) → POST /auth/social-login` is wired but not yet called from the mixin.

**Sign Up** — `SignUpScreen` form (name + email + password)
→ `SignUpViewModel.signUp`
→ `AuthRepository.signUp(SignUpRequest)` → `POST /auth/sign-up`
→ `AppRouter.push(routeOtpVerify, extra: { email })`.

**Forgot Password** — `ForgotPasswordScreen` (email)
→ `ForgotPasswordViewModel.sendResetLink`
→ `AuthRepository.forgotPassword(email)` → `POST /auth/forgot-password`
→ `AppRouter.push(routeOtpVerify, extra: { email })`.

**OTP Verify** — `OtpVerifyScreen` (email passed via route extras, OTP input)
→ `OtpViewModel.verify`
→ `AuthRepository.verifyOtp(OtpVerifyRequest)` → `POST /auth/verify-otp`
→ `AuthResponse` → `AuthStateNotifier.saveToken` + `setUser`
→ `AppRouter.go(routeMain)`.

**OTP Resend** — same screen
→ `OtpViewModel.resend(email)`
→ `AuthRepository.sendOtp(OtpSendRequest)` → `POST /auth/send-otp`.

**Sign Out** (wired in repo, not yet exposed as a UI flow)
→ `AuthRepository.signOut()` → `POST /auth/sign-out`.

### Notes / TODO

- `AuthMixin.signInWithSocial` is a stub; wire it to `AuthRepository.socialLogin` and persist the returned tokens before calling `onSuccess`.
- `refresh-token` endpoint is declared but the refresh interceptor is not yet implemented in `network/interceptors/`.
- `reset-password` endpoint exists but no screen/VM consumes it yet — currently OTP verify auto-logs-in instead of routing to a new-password screen.

---

## Pending Modules

- `splash` / `intro` — bootstrap and onboarding (screens exist; flow not yet documented here).
- `main` — post-auth shell (screens exist; flow not yet documented here).
- Profile, notifications, FCM token registration (`/notifications/register-token`), avatar upload — endpoints declared, no flow yet.
