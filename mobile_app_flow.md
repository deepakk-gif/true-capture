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

## Auth Module — DONE

Routes: `routeSignIn` `/sign-in`, `routeSignUp` `/sign-up`, `routeForgotPassword` `/forgot-password`, `routeOtpVerify` `/otp-verify`. After success, all flows land on `routeMain` `/main`.

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
