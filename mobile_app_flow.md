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
- **Motion**: widget-level animation uses the `flutter_animate` package via the `.animate()` API. Timing/curves come from `core/constants/app_motion.dart#AppMotion` (`fast`/`normal`/`slow`/`stagger` durations; `enter`/`exit`/`standard` curves) — never hard-coded. The `flutter-widget-animation` Claude skill (`.claude/skills/`) governs how/when motion is applied: restrained entrances, no replay-on-rebuild traps. Route transitions stay separate in `core/router/app_router.dart` (`AnimationType`). Reference demo: `intro_screen.dart#_IntroPage` (staggered fade + slide).

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
| `sign_in_request.dart` | `LoginDto(Email, Password, FcmToken?, DeviceType?)` — optional `fcmToken` + `deviceType` for FCM registration |
| `sign_up_request.dart` | `RegisterDto(Email, Username, Password, FcmToken?, DeviceType?)` — same optionals |
| `otp_request.dart` | `OtpSendRequest(Email, Purpose)` + `VerifyOtpAndIssueDto(Email, Code, Purpose, FcmToken?, DeviceType?)` |
| `forgot_password_request.dart` | `ForgotPasswordDto(Email)` |
| `reset_password_request.dart` | `ResetPasswordDto(Email, Code, NewPassword)` |
| `google_sign_in_request.dart` | `GoogleSignInDto(IdToken, FcmToken?, DeviceType?)` |
| `refresh_token_request.dart` | `RefreshDto(RefreshToken, FcmToken?, DeviceType?)` — used for both `/refresh` and `/logout`. On logout, `fcmToken` triggers backend removal of that `UserDevice` row + topic unsubscribe. |

All FCM-related fields use camelCase keys (`fcmToken`, `deviceType`) to match the backend's default `JsonSerializerDefaults.Web` policy.

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
| `auth/otp/otp_verify_screen.dart` | `otp_view_model.dart` | Takes `email` + `purpose` from route extras. On 6-digit code completion: `verifyEmail` → `repository.verifyOtp` → `AuthStateNotifier.saveToken+setUser` → `go(routeMain)`. `passwordReset` → push `routeResetPassword` with `{ email, code }` (no /verify-otp call — `/reset-password` consumes the OTP itself). When `AppConfig.isLocal`, passes `initialValue: AppConfig.localTestOtp` (`'123456'`) to `CustomOtpField` — see local-env testing OTP below. |
| `auth/forgot_password/forgot_password_screen.dart` | `forgot_password_view_model.dart` | Email → `AuthRepository.forgotPassword(ForgotPasswordRequest)` → push `routeOtpVerify` with `{ email, purpose: passwordReset }`. |
| `auth/reset_password/reset_password_screen.dart` (NEW) | `reset_password_view_model.dart` (NEW) | Takes `email` + `code` from extras. Form: new password + confirm → `AuthRepository.resetPassword(ResetPasswordRequest)` → `go(routeSignIn)`. |
| `auth/sign_in/sign_in_screen.dart` | `sign_in_viewmodel.dart` | Email + password → `AuthRepository.signIn` → save tokens → main. Social Google button → `AuthMixin.signInWithSocial` (Google flow — see task `#20`). |

### Local-env testing OTP (`config/app_config.dart` + `common_widgets/custom_otp_field.dart`)
For localhost testing the backend's Development environment issues a fixed OTP instead of a random one, so no real inbox is needed.

- `AppConfig.localTestOtp = '123456'` — must match `OtpService.DevFixedCode` on the backend.
- `AppConfig.isLocal` — `true` when `environment == Environment.local`.
- `CustomOtpField` has an optional `initialValue` param that **only pre-fills the cells** (visual hint). `OtpVerifyScreen` passes `AppConfig.isLocal ? AppConfig.localTestOtp : null`.
- Auto-submission is driven by the **screen**, not the field: `_OtpVerifyScreenState.onModelReady` calls `OtpViewModel.verify(...)` once with `localTestOtp` when `AppConfig.isLocal`. `onModelReady` fires exactly once (post first frame) from the screen's stable state.
- **Why screen-level, not field-level**: the field previously auto-submitted from its own `initState` post-frame callback. `ScreenStateAware` wraps the form in a `Stack` for `apiProgress` and not for `content`; that structural change recreates `CustomOtpField`'s `State`, so its `initState` re-ran on every `verify()` state transition → an infinite `verify-otp` loop that tripped the `Auth` rate limit (`429`). Driving the one-shot from `onModelReady` (stable screen state) fixes it.

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
4. `AuthRepository.googleSignIn(GoogleSignInRequest(idToken, fcmToken, deviceType))` → `POST /api/auth/google`. The Google view-model passes the cached FCM token + device type through `AuthMixin.signInWithSocial(storage: …)` so the backend registers the device + subscribes it to `"all"` in the same call.
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

**Sign Out** — `TabProfile` "Sign out" tile (`presentation/screens/main/tabs/tab_profile/tab_profile.dart`)
→ reads `StorageKeys.refreshTokenKey` + `StorageKeys.fcmTokenKey` from secure storage
→ `AuthRepository.signOut(RefreshTokenRequest(refreshToken, fcmToken))` → `POST /api/auth/logout` (best-effort try/catch)
→ `SocialLoginService.signOut()` (Google sign-out)
→ deletes `StorageKeys.fcmTokenKey`
→ `AuthStateNotifier.clear()` → `AppRouter.go(routeSignIn)`.

Backend-side, the `fcmToken` on the logout payload triggers `IUserDeviceService.RemoveAsync(userId, fcmToken)` which unsubscribes the token from `"all"` and deletes the matching `UserDevice` row.

### Notes / TODO

- `AuthMixin.signInWithSocial` is a stub; wire it to `AuthRepository.socialLogin` and persist the returned tokens before calling `onSuccess`.
- `refresh-token` endpoint is declared but the refresh interceptor is not yet implemented in `network/interceptors/`.
- `reset-password` endpoint exists but no screen/VM consumes it yet — currently OTP verify auto-logs-in instead of routing to a new-password screen.

---

## Splash / bootstrap routing (`presentation/screens/splash/splash_viewmodel.dart`)

`SplashViewmodel.setupBeforeStart` decides the first screen on cold start:

1. No `accessTokenKey` → `routeIntro`.
2. `pendingVerifyEmailKey` set → the user registered but never finished OTP
   verification. The OTP screen is only meant to be reached **in-session**
   (pushed right after `/register`). On a relaunch the half-finished
   registration is treated as **abandoned**: `AuthStateNotifier.clear()` wipes
   the unverified token + pending key, and the user is sent to `routeSignIn`.
   (Previously this re-routed straight to `routeOtpVerify`, trapping the user
   on the OTP screen on every restart.)
3. **Fast path** — `AuthStateNotifier.hasValidAccessToken()` true (stored access
   token + a persisted `accessExpiresAtKey` still in the future, minus a 30s skew) →
   `loadCachedUser()` restores the cached profile into state → `routeMain`, with **no
   blocking network call** (works offline). A best-effort `getProfile()` then refreshes
   the profile in the background; failure keeps the cached copy.
4. **Slow path** — token present but expiry missing/expired → `getProfile()` (the
   refresh interceptor rotates an expired access token on a 401) → `setUser` →
   `routeMain`; on failure → `routeSignIn`.

### Session persistence

The auth session is kept across app restarts via `flutter_secure_storage`
(`LocalStorageService` — encrypted: Android Keystore / iOS Keychain):

- `AuthStateNotifier.saveToken(access, {refreshToken, accessExpiresAtUtc})` persists the
  token, refresh token and **expiry** (`accessExpiresAtKey`, ISO-8601 UTC). All login
  paths (sign-in, sign-up, OTP verify, Google) pass `response.accessExpiresAtUtc`.
- `RefreshInterceptor._doRefresh` rewrites `accessExpiresAtKey` after a silent token
  rotation, so the local validity check stays accurate.
- `setUser(...)` caches the full `UserResponse` as JSON (`userProfileKey`);
  `loadCachedUser()` restores it on launch without a network call.
- `hasValidAccessToken()` is the local validity check; `clear()` wipes token, refresh
  token, expiry, cached profile, user id and pending-verify key.

---

## Notifications / FCM — IN PROGRESS

Adds push-notification support. Plumbing already present: `firebase_core ^3.4.0` + `firebase_messaging ^15.1.1` in `pubspec.yaml`; `lib/firebase_options.dart` generated; `android/app/google-services.json` + `ios/Runner/GoogleService-Info.plist` in place.

### Local storage (`presentation/providers/local_storage_provider.dart`)
| Key | Purpose |
|---|---|
| `StorageKeys.fcmTokenKey` (`'fcm_token'`) | Cached FCM device token. Written by `FirebaseService` on init + `onTokenRefresh`. Read by every auth view-model when assembling the request. |
| `StorageKeys.deviceTypeKey` (`'device_type'`) | Cached device platform ("ios" / "android"). Static for the install; computed once on first init from `Platform.isIOS/isAndroid`. |

### Firebase bootstrap (`services/firebase_service.dart`)
`FirebaseService.instance.initialize()` now does:
1. `_requestPermission()` — `FirebaseMessaging.instance.requestPermission(alert/badge/sound)`.
2. `_getToken(storage)` — fetches via `FirebaseMessaging.instance.getToken()` and writes to `StorageKeys.fcmTokenKey` in secure storage.
3. Writes `StorageKeys.deviceTypeKey` from `FirebaseService.currentDeviceType()` (`Platform.isIOS → "ios"`, `Platform.isAndroid → "android"`, else `"web"`).
4. Wires foreground / background / terminated handlers + `onTokenRefresh` listener that re-writes the latest token.

Static helpers: `FirebaseService.currentDeviceType()` and `FirebaseService.cachedToken(storage)` — used by every auth view-model when building requests so we don't re-hit `FirebaseMessaging` on the hot path.

### View-model injections (`presentation/providers/vm_provider.dart`)
`SignInViewModel` now takes `LocalStorageService` in addition to `AuthRepository` + `AuthStateNotifier`, so it can `FirebaseService.cachedToken(storage)` when building the request. `SignUpViewModel` and `OtpViewModel` already had storage injected (`pendingVerifyEmailKey` flow) and now reuse it for the FCM lookup. `AuthMixin.signInWithSocial` gains a `storage` parameter for the Google flow.

### Boot wiring (`lib/main.dart`)
After `ApiService.instance.initialize()`, `main()` runs `await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform)` followed by `await FirebaseService.instance.initialize()`. Both are wrapped in a fail-soft `try/catch` so a misconfigured Firebase (e.g. iOS APNs key not uploaded yet) doesn't block app launch — the worst case is `fcm_token` being absent on subsequent auth requests, which the backend treats as optional.

### Planned changes
- All auth request DTOs (`sign_in_request`, `sign_up_request`, `otp_request#OtpVerifyRequest`, `refresh_token_request`, `social_login_request`) gain optional `fcmToken` + `deviceType` fields, serialized as camelCase (`fcmToken` / `deviceType`) to match the backend's default `JsonSerializerDefaults.Web` policy.
- View-models for sign-in / sign-up / OTP verify / Google social / logout read the cached values from secure storage and pass them through the repository → backend on every token-issuing call. Backend persists each (`user`, `fcm_token`) row in `identity.UserDevice` and auto-subscribes it to the `"all"` topic.

## Main Shell — 5-tab bottom navigation

`MainScreen` (`presentation/screens/main/main_screen.dart`) is the post-auth shell:
an `IndexedStack` of 5 tabs driven by `MainViewModel.currentIndex`, with
`CustomBottomNavigation` (fixed-type `BottomNavigationBar`).

| # | Tab | Widget (`presentation/screens/main/tabs/`) | Status |
|---|---|---|---|
| 1 | Home | `tab_home/tab_home.dart` | story tray + Normal-post feed (see Create Post Module) |
| 2 | Fake vs Real | `tab_fake_vs_real/tab_fake_vs_real.dart` | Fake-vs-Real feed (see Create Post Module) |
| 3 | Create Post | `tab_create_post/tab_create_post.dart` | post composer (see Create Post Module) |
| 4 | Message | `tab_message/tab_message.dart` | placeholder |
| 5 | Profile | `tab_profile/tab_profile.dart` | header + Theme + Sign out |

`TabProfile` absorbed the former `TabSettings` (theme toggle + sign-out tile);
`tab_settings/` was removed. Note this differs from PRD §6, which lists 4 tabs
with Messaging as a top-bar icon — Message is a dedicated tab per request.

## Error handling — auth surfaces

`BaseViewModel.executeWithLoading` takes an `errorState` param: auth view models
pass `ScreenState.content` so a failed submission keeps the form visible.
`BaseConsumerState` listens to its view model and surfaces new errors via a
snackbar (skipped for full-page `ScreenState.error`). Social-login failures in
`AuthMixin.signInWithSocial` are forwarded through `onError` → `setError`.

## Profile Module — DONE (edit profile + avatar upload)

Route: `routeEditProfile` `/edit-profile` (pushed from the Profile tab). Backed by `UsersController` (`/api/users/me`) on the backend.

### Endpoint constants (`core/constants/api_endpoints.dart`)
Already declared: `userProfile` / `updateProfile` = `/api/users/me`, `uploadAvatar` = `/api/users/me/avatar`.

### DTOs
- Request: `network/dto/request/user/update_profile_request.dart` — `UpdateProfileRequest(displayName?, bio?, gender?, accountType?)` → backend `UpdateProfileRequest`. `gender` is `"male"/"female"/"other"/null`; `accountType` is `"public"/"private"`.
- Response: `network/dto/response/auth/user_response.dart#UserResponse` — the full current-user model. Fields: `id, email, username, name (displayName), avatarUrl, bio, joinedAt, followersCount, followingCount, postsCount, emailVerified, isBlueTick, isSuspended, accountType, gender` (camelCase keys + snake-case fallback; `isBlueTick` falls back to `isVerified`). Returned by `GET/PUT /api/users/me` and the avatar endpoints.

### Repository (`repositories/user_repository.dart` — NEW)
`UserRepository(ApiService)`, exposed via `repo_provider.dart#userRepo`:

| Method | Endpoint | Returns |
|---|---|---|
| `getMyProfile()` | `GET /api/users/me` | `UserResponse` |
| `updateProfile(UpdateProfileRequest)` | `PUT /api/users/me` | `UserResponse` |
| `uploadAvatar(File)` | `POST /api/users/me/avatar` (multipart, field `file`) | `UserResponse` |
| `removeAvatar()` | `DELETE /api/users/me/avatar` | `UserResponse` |

`uploadAvatar` builds a Dio `FormData` with a `MultipartFile` whose `contentType` is derived from the file extension (JPEG/PNG/WebP — backend `AvatarRules` rejects others). `ApiService.postMultipart(path, FormData)` (NEW) sends it — Dio sets the `multipart/form-data` content-type automatically.

### Multipart / media URLs
- `ApiService.postMultipart` — multipart POST helper.
- `AppConfig.resolveUrl(path)` (NEW) — resolves a relative backend media path (`/media/avatars/x.jpg`) to an absolute URL against `baseUrl`; absolute URLs (S3/CDN) pass through unchanged.

### Widgets
- `presentation/common_widgets/user_avatar.dart#UserAvatar(avatarUrl, name, radius)` (NEW) — circular avatar; shows the network image (via `AppConfig.resolveUrl`), falling back to initials, then a person icon.

### Screen + ViewModel
`presentation/screens/profile/edit_profile/` — `EditProfileScreen` + `EditProfileViewModel` (provider `editProfileViewModelProvider`, built with `userRepo` + `AuthStateNotifier`).

Flow:
- **Load** — `onModelReady` → `EditProfileViewModel.load()` → `UserRepository.getMyProfile()` → `AuthStateNotifier.setUser`; the screen then seeds display name, bio, the gender dropdown, and the private-account switch from the refreshed auth-state user.
- **Save** — `save(displayName, bio, gender, accountType)` → `UserRepository.updateProfile` → `setUser` → `AppRouter.pop`. Validation: name ≤ 80, bio ≤ 500. Gender dropdown (`Not specified` / Male / Female / Other) + a "Private account" `SwitchListTile` map to `gender` / `accountType`.
- **Avatar** — the avatar's camera button opens a bottom sheet (gallery / camera / remove). Gallery/camera use `ImagePickerUtils`; the picked `File` → `EditProfileViewModel.uploadAvatar` → `UserRepository.uploadAvatar` → `setUser`. Remove → `removeAvatar` → `DELETE` → `setUser`.

Every mutation refreshes the `AuthStateNotifier` user, so `UserAvatar` instances across the app (e.g. the Profile tab) update reactively.

### Profile tab (`screens/main/tabs/tab_profile/tab_profile.dart`)
Instagram-style header: a `Row` of `UserAvatar` + a `_StatColumn` trio (posts / followers / following counts), then display name with a blue-tick `Icon` when `isBlueTick`, `@username`, email, and bio. "Edit profile" tile pushes `routeEditProfile`.

### Post-login profile fetch
The backend auth responses carry no `user` object, so after `saveToken(...)` the sign-in (`sign_in_viewmodel.dart`), Google (`auth_mixin.dart`), and OTP-verify (`otp_view_model.dart`) flows now call `AuthRepository.getProfile()` (`GET /api/users/me`) → `AuthStateNotifier.setUser`. The fetch is best-effort (wrapped in try/catch) — login still completes if it fails, and the splash screen re-fetches on the next app entry.

## Social Feature — DONE (search, profiles, follow, posts, notices)

User search, follow graph, viewable profiles, basic posts, and the in-app notice inbox.

### Routes (`app_router.dart`)
`routeUserSearch` `/user-search`, `routeUserProfile` `/user-profile` (extra `userId`), `routeFollowList` `/follow-list` (extra `userId` + `type`), `routeFollowRequests` `/follow-requests`, `routeNotices` `/notices`. Create-post is hosted in the existing `tab_create_post` tab.

### Entry points
- Home tab (`tab_home.dart`) — a search `IconButton` in the `CustomAppBar` → `routeUserSearch`.
- Profile tab (`tab_profile.dart`) — new "Follow requests" + "Notices" `ListTile`s.
- `tab_create_post.dart` now renders `CreatePostScreen`.

### Data layer
- DTOs: `network/dto/social_models.dart` — `UserSearchItem`, `UserProfileView`, `FollowActionResult`, `FollowUserItem`/`FollowListResult`, `PostItem`/`PostListResult`, `NoticeItem`/`NoticeListResult`, `FollowState` constants.
- Repositories (`repo_provider.dart`): `SocialRepository` (search, profile, follow/unfollow, followers/following, requests + accept/reject, user posts), `PostRepository` (create multipart, delete), `NoticeRepository` (list, unread-count, mark-read).
- `RecentSearchService` (`services/recent_search_service.dart`) — **local-only** recent-search history (JSON list of tapped users under `StorageKeys.recentSearchesKey`); add / remove-one / clear-all. No API.
- Endpoint constants + path builders in `api_endpoints.dart`.

### Screens (`presentation/screens/social/`)
| Screen | Flow |
|---|---|
| `search/user_search_screen.dart` | Debounced query → `SocialRepository.search`; results show `UserListRow` with mutual-follower subtitle ("Followed by a, b +N"). A **Recent** section (per-item remove + Clear all) shows when the query is empty. Tap → remembers the user locally → `routeUserProfile`. |
| `profile/user_profile_screen.dart` | `SocialRepository.profile` → header (avatar, counts, name + blue tick, bio) + Follow / Message buttons. Counts tap → `routeFollowList`. Public → posts grid (`GET /api/users/{id}/posts`); private + not following → a lock placeholder. `Follow` toggles follow / requested / unfollow; `Message` → "Messaging coming soon" snackbar. |
| `follow/follow_list_screen.dart` | Followers / following list of `UserListRow`s → tap → profile. |
| `follow/follow_requests_screen.dart` | Incoming pending requests. Each row: **Accept** + **Cancel** while pending; on **Accept** the row stays and swaps to a **Follow back** action (`FollowRequestsViewModel` wraps each request in a mutable `FollowRequestRow` tracking `accepted` + `followState`). Follow-back toggles via `SocialRepository.follow/unfollow`; the button reflects `none → Follow back`, `following → Following`, `requested → Requested`. **Cancel** rejects + removes the row. No backend change — `GET /api/follow/requests` already returns each requester's `followState`. |
| `notices/notices_screen.dart` | In-app admin notices; tapping opens the body + marks it read. |
| `post/create_post_screen.dart` | Image picker (`ImagePickerUtils`) + caption → `PostRepository.create` (multipart). |

View-models extend `BaseViewModel`; providers in `vm_provider.dart`. Shared widget `common_widgets/user_list_row.dart#UserListRow` (avatar + name + blue tick + optional trailing) is reused across search, follow lists, and requests.

### Push handling (`firebase_service.dart`)
`onMessageOpenedApp` taps route by the FCM data `type`: `follow_request` → follow-requests screen; `follow_accepted` / `new_follower` → that user's profile; `admin_notification` → notices. Terminated-state taps are logged only (avoids racing the splash redirect).

## Create Post Module — DONE (feed tabs, post card, composer, engagement)

Two post types — **Normal** (Home tab) and **Fake vs Real** (Fake vs Real tab) — on a
signed-URL media pipeline. MVVM: Screen → `BaseConsumerState` → ViewModel → Repository →
`ApiService`.

### Endpoints (`core/constants/api_endpoints.dart`)
`mediaUploads`, `mediaBlob(id)`, `mediaFinalize`, `feed`, `posts`, `postById/Like/Save/
Share/Vote/Report`, `postComments`, `commentReplies/Like`, `commentById`, `mySaves`.

### DTOs (`network/dto/post_models.dart` — NEW)
`PostDto` (+ `PostAuthorDto`, `PostMediaDto`), `FeedResult`, `UploadTicket`,
`MediaAssetDto`, `CommentDto`/`CommentListResult`, `LikeResult`, `VoteResult`.
`social_models.dart#PostItem` updated to `coverUrl`/`type`/`kind` (grid thumbnails).

### Repositories (`repo_provider.dart`)
- `MediaRepository` (NEW) — `uploadFile`/`uploadAll`: `POST /api/media/uploads` → raw-bytes
  `PUT /api/media/blob/{id}` (`ApiService.putBytes`) → `POST /api/media/finalize`.
- `FeedRepository` (NEW) — `getFeed(channel, cursor)` for `FeedChannel.home` / `.fakeVsReal`.
- `PostRepository` (rewritten) — create, detail, delete, like/save/share/vote/report
  toggles, comments + replies + comment-like, `saved`.

### View-models (`vm_provider.dart`)
- `FeedViewModel` (`feedViewModelProvider` family by channel) — list, refresh, cursor
  paging, optimistic like/save/vote/share/report.
- `CreatePostViewModel` — media list + type + submit (upload then create).
- `PostDetailViewModel`, `CommentsViewModel` (top-level + 1 reply level + comment likes).

### Screens / widgets
| File | Role |
|---|---|
| `common_widgets/post_card.dart` (NEW) | Reusable card: avatar+username (→profile), Follow button, blue tick, media carousel / video placeholder, like/comment/share/save row, Fake-vs-Real vote bar, caption show-more, view count + relative time, `⋮` → About account / Report sheet |
| `main/tabs/feed_view.dart` (NEW) | Shared feed list — pull-to-refresh + infinite scroll; used by both tabs |
| `main/tabs/tab_home/tab_home.dart` | Story tray + `FeedView(home)` |
| `main/tabs/tab_fake_vs_real/tab_fake_vs_real.dart` | `FeedView(fakeVsReal)` |
| `social/post/create_post_screen.dart` | Composer — type toggle (Fake-vs-Real shown when `isBlueTick`), multi-photo / video picker, caption, reference links |
| `social/post/post_detail_screen.dart` | `PostCard` + comments entry + reference list |
| `social/post/comments_screen.dart` | Comments with 1-level replies, per-comment like, reply composer |
| `social/post/report_sheet.dart` (NEW) | Report bottom sheet — preset reasons + "Other" text |

Video playback with a mute control is a documented follow-up — videos currently render a
thumbnail with a play badge and open the detail screen on tap. The `domain/post/{shareId}`
deep link is handled by the messaging module.

## Messaging Module — DONE (1-to-1 chat, SignalR realtime, notifications)

1-to-1 chat backed by the `Modules.Messaging` API. MVVM: Screen → `BaseConsumerState`
→ ViewModel → `MessageRepository` → `ApiService`; realtime is additive via SignalR.

### Dependencies (`pubspec.yaml`)
`signalr_netcore` (SignalR client), `flutter_local_notifications`,
`flutter_image_compress`, `video_compress` (video compression + thumbnail).

### Network / services
- DTOs `network/dto/message_models.dart` — `ConversationDto`, `MessageDto`
  (+ `MessageReplyDto`, `ReactionDto`, `ChatUserDto`), list results.
- `MessageRepository` — REST: conversations, messages (newest-first pages), send,
  read, pin, react, delete. Endpoints under `ApiEndpoints` (`conversations`,
  `conversationMessages/Read/Pin`, `messageReact/ById`, `chatHub`).
- `MediaRepository.uploadBytes` (NEW) — uploads in-memory compressed image bytes.
- `ChatSocketService` (`services/chat_socket_service.dart`) — wraps the SignalR
  `HubConnection` to `{baseUrl}/hubs/chat?access_token=…`; exposes `onMessage` /
  `onRead` / `onReaction` broadcast streams; holds `activeConversationId`.
- `LocalNotificationService` — `flutter_local_notifications`; shows message
  notifications, routes a tap (payload = conversationId) to the chat.

### View-models (`vm_provider.dart`)
- `ConversationListViewModel` — loads conversations, keeps them live off
  `ChatSocketService.onMessage` / `onRead`, pin toggle (max 3), unread tracking;
  `sorted` puts pinned first then by recency.
- `ChatViewModel` — resolves the conversation (`open` accepts a `ConversationDto`,
  a `userId`, or a `conversationId`), loads newest-first history with scroll-up
  pagination, sends text / image (compress 70%) / video (compressed + thumbnail,
  ≤10 MB), reactions (one per user, replaceable), replies; live via the socket.

### Screens (`presentation/screens/messaging/`)
| File | Role |
|---|---|
| `tab_message/tab_message.dart` | Conversation list — `UserAvatar`, name, last-message preview (**bold when unread**), unread badge, pin icon, relative time; long-press → pin/unpin; compose FAB → user search |
| `chat_screen.dart` | Reverse `ListView` (newest at bottom) + scroll-up paging; header; reply bar; composer (text + attach photo/video); marks read |
| `message_bubble.dart` | Text / image / video (thumbnail + play badge) bubble; reply quote; reaction chips; relative time; **double-tap → ❤️**, long-press → emoji bar + Reply |
| `chat_time.dart` | `chatTimeShort` / `chatTimeRelative` formatters |

### Realtime + notification wiring
- `MainScreen.onModelReady` → `_bootstrapChat`: init `LocalNotificationService`,
  connect `ChatSocketService`, and a global `onMessage` listener that shows a
  local notification when the message isn't the user's own and isn't the open
  chat (`activeConversationId`).
- `firebase_service.dart` — `routeNotificationData` routes a `type:'message'` FCM
  to `routeChat`; terminated-launch payloads are stashed in
  `FirebaseService.pendingLaunchData` and consumed by `_bootstrapChat`.
- Route `routeChat` (`/chat`) in `app_router.dart`; the profile "Message" button
  opens the chat by `userId`.

## Pending Modules

- `splash` / `intro` — bootstrap and onboarding (screens exist; flow not yet documented here).
