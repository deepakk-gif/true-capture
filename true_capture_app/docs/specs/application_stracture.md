# App Architecture Structure Document

A generic, copy-pasteable folder structure and patterns for building any Flutter app with MVVM + Riverpod architecture.

---

## 1. Complete Folder Structure

```
lib/
├── config/                        # Environment configuration
│   ├── app_config.dart            # API URLs, timeouts, env constants
│   ├── png_config.dart            # PNG asset path constants
│   └── svg_config.dart            # SVG asset path constants
│
├── core/                          # Core utilities
│   ├── constants/                 # App-wide constants
│   │   ├── app_constants.dart    # General app constants
│   │   ├── color_helper.dart      # Color palette/helper
│   │   ├── language_keys.dart     # Localization string keys
│   │   └── api_endpoints.dart     # API endpoint constants
│   ├── extensions/                # Dart extensions
│   │   ├── context_extensions.dart
│   │   ├── date_time_formate_extesion.dart
│   │   └── map_extension.dart
│   ├── router/                    # Navigation
│   │   └── app_router.dart        # GoRouter + ScreenPath + AnimationType
│   ├── theme/                     # Theming
│   │   └── app_theme.dart         # Light/dark theme definitions
│   └── utils/                     # Utilities
│       ├── image_picker_utils.dart
│       └── share_helper.dart
│
├── enum/                          # App-wide enums
│   ├── bio_matric_enum.dart
│   └── social_user_type.dart
│
├── extension/                      # Helper extensions
│   ├── toast_helper.dart
│   └── keyboard_hide_extension.dart
│
├── log/                           # Logging
│   └── app_logs.dart              # appLog() debug utility
│
├── mixin/                         # Dart mixins
│   ├── auth_mixin.dart            # Auth navigation mixin
│   └── model_converter.dart       # JSON serialization mixin
│
├── network/                       # Network layer
│   ├── dto/                       # Data Transfer Objects (Freezed)
│   │   ├── request/               # API request models
│   │   │   └── auth/
│   │   │       ├── sign_in_request.dart
│   │   │       ├── sign_up_request.dart
│   │   │       ├── social_login_request.dart
│   │   │       └── otp_request.dart
│   │   └── response/             # API response models
│   │       ├── auth/
│   │       │   ├── user_response.dart
│   │       │   └── auth_response.dart
│   │       └── common/
│   │           └── api_response.dart
│   ├── interceptors/             # Dio interceptors
│   │   ├── auth_interceptor.dart  # Token injection
│   │   ├── error_interceptor.dart # Error handling
│   │   └── logging_interceptor.dart
│   └── helper/
│       ├── error_handler.dart
│       └── response_error.dart
│
├── package/                       # Reusable widget packages
│   └── bottom_navigation/         # Custom bottom navigation bar
│
├── repositories/                  # Repository pattern implementations
│   ├── auth_repository.dart       # Sign in/up, OTP, social login
│   ├── common_repository.dart     # Shared/common API calls
│   └── # ADD YOUR REPOS HERE     # exercise_repository, home_repository, etc.
│
├── services/                      # Singleton services
│   ├── api_service.dart           # Dio HTTP client singleton
│   ├── bio_matric_service.dart    # Biometric auth (fingerprint/face)
│   ├── firebase_service.dart      # FCM push notifications
│   ├── local_service.dart         # Country/locale detection
│   ├── platform_informaion.dart   # Device info
│   ├── social_login_service.dart  # Google, Facebook, Apple sign-in
│   └── time_zone_service.dart     # Device timezone
│
├── presentation/                  # UI Layer
│   ├── common_model/              # Shared data models
│   │   ├── action_button.dart
│   │   └── dropdown_value_model.dart
│   │
│   ├── common_widgets/           # Reusable widgets
│   │   ├── custom_app_bar.dart
│   │   ├── custom_button.dart
│   │   ├── custom_input_field.dart
│   │   ├── custom_otp_field.dart
│   │   ├── custom_date_time_textfield.dart
│   │   ├── loading_indicator.dart
│   │   ├── error_widget.dart
│   │   ├── empty_widget.dart
│   │   └── # ADD YOUR WIDGETS HERE
│   │
│   ├── formater/                 # Data formatters
│   │   └── date_formater.dart
│   │
│   ├── providers/                # Riverpod providers
│   │   ├── repo_provider.dart    # Repository DI
│   │   ├── vm_provider.dart      # ViewModel providers
│   │   ├── local_storage_provider.dart  # StorageKeys + service
│   │   ├── language_provider.dart
│   │   ├── theme_provider.dart
│   │   ├── user_data_provider.dart
│   │   └── refresh_provider.dart
│   │
│   └── screens/                  # Feature modules
│       ├── base/                 # BASE CLASSES (centralized)
│       │   ├── base_view_model.dart
│       │   ├── base_consumer_state.dart
│       │   ├── screen_state.dart
│       │   └── screen_state_aware.dart
│       │
│       ├── splash/               # SPLASH MODULE
│       │   ├── splash_screen.dart
│       │   └── splash_viewmodel.dart
│       │
│       ├── intro/                # INTRO MODULE
│       │   └── intro_screen.dart
│       │
│       ├── auth/                 # AUTH MODULE
│       │   ├── sign_in/
│       │   │   ├── sign_in_screen.dart
│       │   │   └── sign_in_viewmodel.dart
│       │   ├── sign_up/
│       │   │   ├── sign_up_screen.dart
│       │   │   └── sign_up_view_model.dart
│       │   ├── forgot_password/
│       │   │   ├── forgot_password_screen.dart
│       │   │   └── forgot_password_view_model.dart
│       │   └── otp/
│       │       ├── otp_verify_screen.dart
│       │       └── otp_view_model.dart
│       │
│       ├── main/                 # MAIN / HOME MODULE (after auth)
│       │   ├── main_screen.dart   # Bottom nav host
│       │   ├── main_view_model.dart
│       │   └── tabs/
│       │       ├── tab_home/
│       │       ├── tab_profile/
│       │       └── tab_settings/
│       │
│       └── # ADD YOUR MODULES HERE
│           # example_module/
│           # ├── example_screen.dart
│           # ├── example_view_model.dart
│           # └── widgets/
│
├── firebase_options.dart        # Firebase configuration
└── main.dart                    # App entry point
```

---

## 2. Base Classes (Centralized in `presentation/screens/base/`)

### `screen_state.dart` - Screen State Enum

```dart
enum ScreenState {
  none,         // Initial/unset state
  progress,     // Full-screen loading (initial load)
  apiProgress,  // In-place loading (pull-to-refresh, button action)
  content,      // Normal content display
  empty,        // No data state
  error,        // Error state
  noInternet,   // No internet connection
  refresh,      // Content with refresh trigger callback
  action,       // User-triggered action (button press)
}
```

### `base_view_model.dart` - Base ViewModel

```dart
abstract class BaseViewModel extends ChangeNotifier {
  String? _error;
  String? get errorMessage => _error;
  bool get hasError => _error != null;

  ValueNotifier<ScreenState> screenState = ValueNotifier(ScreenState.content);

  void changeScreenState(ScreenState value);
  void setError(String? errorMessage);
  void clearError();

  Future<T?> executeWithLoading<T>({
    required Future<T> Function() operation,
    Function(dynamic error, dynamic stackError, String errorMessge)? errorCallBack,
    ScreenState initialState = ScreenState.apiProgress,
  }) async {
    // 1. Set screenState to initialState
    // 2. Try executing operation
    // 3. On success, set screenState to content
    // 4. On error, set error and call errorCallBack
    // 5. Return result
  }
}
```

### `base_consumer_state.dart` - Base Consumer State

```dart
abstract class BaseConsumerState<T extends ConsumerStatefulWidget,
    V extends BaseViewModel> extends ConsumerState<T> {
  late final V viewModel;

  String screenName();           // e.g., "SPLASH SCREEN"
  V createViewModel();           // e.g., ref.read(splashVm)
  void onModelReady(V model) {}   // Override for init logic
  bool isBottomSheet() => false;

  @override
  void initState() {
    super.initState();
    viewModel = createViewModel();
    onModelReady(viewModel);
  }

  void showMessage(String message) { }
  void showErrorMessage(String message) { }
}
```

### `screen_state_aware.dart` - State Overlay Widget

```dart
ScreenStateAware(
  state: viewModel.screenState,           // ValueNotifier<ScreenState>
  progress: Center(child: CircularProgressIndicator()),
  apiProgress: Center(child: CircularProgressIndicator()),
  error: (context) => MyErrorWidget(onTap: () {}),
  empty: Center(child: Text('No data')),
  builder: (context) => MyContent(),
)
```

---

## 3. Services Layer

| Service | File | Purpose |
|---------|------|---------|
| `ApiService` | `services/api_service.dart` | Dio HTTP client singleton |
| `FirebaseService` | `services/firebase_service.dart` | FCM push notifications |
| `BiometricService` | `services/bio_matric_service.dart` | Fingerprint/Face auth |
| `SocialLoginService` | `services/social_login_service.dart` | Google, Facebook, Apple |
| `LocalStorageService` | `services/local_service.dart` | FlutterSecureStorage wrapper |
| `TimeZoneService` | `services/time_zone_service.dart` | Device timezone |
| `AppPlatformInfo` | `services/platform_informaion.dart` | Device/app info |

### Singleton Pattern

```dart
class ApiService with NetworkParseHelper {
  ApiService._();
  static ApiService? _instance;
  static Dio? _dio;

  static ApiService get instance {
    _instance ??= ApiService._();
    return _instance!;
  }

  void initialize() { /* Dio base config */ }
  Dio get dio => _dio! /* with interceptors */;
}
```

---

## 4. Repository Pattern

```
lib/repositories/
├── auth_repository.dart       # Sign in/up, OTP, social login
├── common_repository.dart     # Shared API calls
└── # ADD YOUR REPOS HERE
```

```dart
class AuthRepository {
  AuthRepository(this._apiService);
  final ApiService _apiService;

  Future<SignUpResponse> signUp(SignUpRequest request) async {
    final response = await _apiService.post<Map<String, dynamic>>(
      ApiEndpoints.signUp,
      data: request.toJson(),
    );
    return SignUpResponse.fromJson(response.data!);
  }
}
```

---

## 5. Provider Pattern

### `repo_provider.dart` - Repository DI

```dart
final authRepo = Provider<AuthRepository>((ref) {
  final apiService = ApiService.instance;
  return AuthRepository(apiService);
});

final localStorageServiceProvider = Provider<LocalStorageService>((ref) {
  return LocalStorageService();
});
```

### `vm_provider.dart` - ViewModel Providers

```dart
final splashVm = Provider.autoDispose<SplashViewmodel>((ref) {
  var auth = ref.read(authRepo);
  var localStorageService = ref.read(localStorageServiceProvider);
  return SplashViewmodel(auth, localStorageService);
});

final signInViewModelProvider = Provider.autoDispose<SignInViewModel>((ref) {
  final authRepository = ref.watch(authRepo);
  final authStateService = ref.watch(authStateNotifierProvider.notifier);
  return SignInViewModel(authRepository, authStateService);
});
```

### `local_storage_provider.dart` - Storage Keys

```dart
class StorageKeys {
  static const String accessTokenKey = 'access_token';
  static const String refreshTokenKey = 'refresh_token';
  static const String isFirstSignUpDoneKey = 'check_first_sign_up_done';
  static const String isFirstIntroDoneKey = 'check_first_intro_done_value';
  static const String themeModeKey = 'theme_mode';
  static const String languageCodeKey = 'language_code';
  static const String isNotificationsEnabledKey = 'notifications_enabled';
  static const String faceLockStatusKey = 'face_lock_status';
  static const String fingurePrintLockStatusKey = 'fingure_lock_status';
  static const String biometricAsked = 'biometric_asked';
}
```

---

## 6. Routing, Theme, Notification, Storage Configuration

### `core/router/app_router.dart`

```dart
enum AnimationType { slideRight, slideLeft, slideUp, fade, scale, rotate, none }

class ScreenPath {
  static const String routeSplash = '/splash';
  static const String routeIntro = '/intro';
  static const String routeSignIn = '/sign-in';
  static const String routeSignUp = '/sign-up';
  static const String routeMain = '/main';
  // ... add all routes
}

class AppRouter {
  static final GoRouter router = GoRouter(
    initialLocation: ScreenPath.routeSplash,
    routes: [
      GoRoute(
        path: ScreenPath.routeSplash,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const SplashScreen(),
          animationType: AnimationType.fade,
        ),
      ),
      // ... all routes
    ],
  );

  static CustomTransitionPage<T> animatedPage<T>({
    required LocalKey key,
    required Widget child,
    required AnimationType animationType,
    Duration duration = const Duration(milliseconds: 300),
    Curve curve = Curves.easeInOut,
  }) { /* transition logic */ }

  static void go(BuildContext context, String location) => context.go(location);
  static Future<T?> push<T>(BuildContext context, String location, {Object? extra}) => context.push(location, extra: extra);
  static void pop(BuildContext context, {dynamic argument}) => context.pop(argument);
}
```

### `core/theme/app_theme.dart`

```dart
class AppTheme {
  // Customize these colors for your app
  static const Color primaryThemeColor = Color(0xFF996FD6);
  static const Color backgroundContainer = Color.fromRGBO(208, 201, 234, 0.3);

  static ThemeData lightTheme = ThemeData(
    brightness: Brightness.light,
    colorScheme: ColorScheme.fromSeed(seedColor: primaryThemeColor, useMaterial3: true),
    // ... full TextTheme, InputDecoration, ElevatedButton styles
  );

  static ThemeData darkTheme = ThemeData(
    brightness: Brightness.dark,
    colorScheme: ColorScheme.fromSeed(seedColor: primaryThemeColor, brightness: Brightness.dark, useMaterial3: true),
    // ...
  );
}
```

### `services/firebase_service.dart` - Notifications

```dart
class FirebaseService with FirebaseMessageManager {
  static FirebaseService get instance {
    _instance ??= FirebaseService._();
    return _instance!;
  }

  Future<void> initialize() async {
    await _requestPermission();
    await _getToken();
    _setupForegroundHandler();   // onMessage.listen
    _setupBackgroundHandler();   // onMessageOpenedApp + getInitialMessage
    _setupTokenRefreshListener();
  }

  void _sendTokenToServer(String token) async {
    // TODO: Send token to backend
  }
}
```

### Local Storage Service

```dart
class LocalStorageService {
  static final LocalStorageService _instance = LocalStorageService._internal();
  factory LocalStorageService() => _instance;
  LocalStorageService._internal();

  final FlutterSecureStorage _storage = const FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
    iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock_this_device),
  );

  Future<void> write(String key, String value) => _storage.write(key: key, value: value);
  Future<String?> read(String key) => _storage.read(key: key);
  Future<void> delete(String key) => _storage.delete(key: key);
  Future<void> deleteAll() => _storage.deleteAll();
}
```

---

## 7. Sample Module Implementation

### Splash Module

**`splash/splash_viewmodel.dart`**
```dart
class SplashViewmodel extends BaseViewModel with AuthMixin {
  SplashViewmodel(this._authRepository, this._localStorageService);

  final AuthRepository _authRepository;
  final LocalStorageService _localStorageService;

  Future<void> setupBeforeStart() async {
    await executeWithLoading(
      operation: () async {
        final token = await _localStorageService.read(StorageKeys.accessTokenKey);
        if (token == null) {
          AppRouter.go(context, ScreenPath.routeIntro);
          return;
        }
        // Validate token, fetch user, navigate based on profileStatus
      },
      errorCallBack: (error, stackError, message) {
        AppRouter.go(context, ScreenPath.routeIntro);
      },
    );
  }
}
```

**`splash/splash_screen.dart`**
```dart
class SplashScreen extends ConsumerStatefulWidget {
  const SplashScreen({super.key});
  @override
  ConsumerState<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends BaseConsumerState<SplashScreen, SplashViewmodel> {
  @override
  void onModelReady(SplashViewmodel model) {
    model.setupBeforeStart();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: ScreenStateAware(
        state: viewModel.screenState,
        progress: Center(child: CircularProgressIndicator()),
        error: (context) => ErrorWidget(viewModel.errorMessage),
        builder: (context) => MySplashContent(),
      ),
    );
  }

  @override
  SplashViewmodel createViewModel() => SplashViewmodel(
    ref.read(authRepo),
    ref.read(localStorageServiceProvider),
  );

  @override
  String screenName() => "SPLASH SCREEN";
}
```

### Intro Module

**`intro/intro_screen.dart`**
```dart
class IntroScreen extends ConsumerStatefulWidget {
  const IntroScreen({super.key});
  @override
  ConsumerState<IntroScreen> createState() => _IntroScreenState();
}

class _IntroScreenState extends ConsumerState<IntroScreen> {
  // Simple screen - no ViewModel needed for intro

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: PageView(
        children: [IntroPage1(), IntroPage2(), IntroPage3()],
        onPageChanged: (index) {
          // Track page
        },
      ),
    );
  }
}
```

### Auth Module

**`auth/sign_in/sign_in_viewmodel.dart`**
```dart
class SignInViewModel extends BaseViewModel with AuthMixin {
  SignInViewModel(this._authRepository, this._authStateNotifier);

  final AuthRepository _authRepository;
  final AuthStateNotifier _authStateNotifier;

  Future<void> signIn(String email, String password) async {
    await executeWithLoading(
      operation: () async {
        final response = await _authRepository.signIn(email, password);
        await _authStateNotifier.saveToken(response.accessToken);
        AppRouter.go(context, ScreenPath.routeMain);
      },
    );
  }

  Future<void> signInWithGoogle() async {
    await signInWithSocial(
      socialType: SocialUserType.google,
      onSuccess: () => AppRouter.go(context, ScreenPath.routeMain),
    );
  }
}
```

**`auth/sign_in/sign_in_screen.dart`**
```dart
class SignInScreen extends ConsumerStatefulWidget {
  const SignInScreen({super.key});
  @override
  ConsumerState<SignInScreen> createState() => _SignInScreenState();
}

class _SignInScreenState extends BaseConsumerState<SignInScreen, SignInViewModel> {
  @override
  void onModelReady(SignInViewModel model) {}

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: ScreenStateAware(
        state: viewModel.screenState,
        progress: Center(child: CircularProgressIndicator()),
        builder: (context) => SignInContent(
          onSignIn: (email, password) => viewModel.signIn(email, password),
          onGoogleSignIn: () => viewModel.signInWithGoogle(),
          onForgotPassword: () => AppRouter.push(context, ScreenPath.routeForgotPassword),
          onSignUp: () => AppRouter.push(context, ScreenPath.routeSignUp),
        ),
      ),
    );
  }

  @override
  SignInViewModel createViewModel() => SignInViewModel(
    ref.read(authRepo),
    ref.read(authStateNotifierProvider.notifier),
  );

  @override
  String screenName() => "SIGN IN";
}
```

### Main Module (Bottom Nav Host)

**`main/main_screen.dart`**
```dart
class MainScreen extends ConsumerStatefulWidget {
  const MainScreen({super.key});
  @override
  ConsumerState<MainScreen> createState() => _MainScreenState();
}

class _MainScreenState extends BaseConsumerState<MainScreen, MainViewModel> {
  @override
  void onModelReady(MainViewModel model) {}

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: viewModel.currentIndex,
        children: [
          TabHome(),
          TabProfile(),
          TabSettings(),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: viewModel.currentIndex,
        onTap: viewModel.changeTab,
        items: [
          BottomNavigationBarItem(icon: Icon(Icons.home), label: 'Home'),
          BottomNavigationBarItem(icon: Icon(Icons.person), label: 'Profile'),
          BottomNavigationBarItem(icon: Icon(Icons.settings), label: 'Settings'),
        ],
      ),
    );
  }

  @override
  MainViewModel createViewModel() => ref.read(mainVm);

  @override
  String screenName() => "MAIN";
}
```

---

## 8. Standard Screen Template

```dart
// === SCREEN FILE ===
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gym_app/core/router/app_router.dart';
import 'package:gym_app/presentation/providers/vm_provider.dart';
import 'package:gym_app/presentation/screens/base/base_consumer_state.dart';
import 'package:gym_app/presentation/screens/base/screen_state_aware.dart';
import 'screen_name_viewmodel.dart';

class ScreenNameScreen extends ConsumerStatefulWidget {
  const ScreenNameScreen({super.key});
  @override
  ConsumerState<ScreenNameScreen> createState() => _ScreenNameScreenState();
}

class _ScreenNameScreenState extends BaseConsumerState<ScreenNameScreen, ScreenNameViewModel> {
  @override
  void onModelReady(ScreenNameViewModel model) {
    model.loadData();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: ScreenStateAware(
        state: viewModel.screenState,
        progress: Center(child: CircularProgressIndicator()),
        apiProgress: Center(child: CircularProgressIndicator()),
        error: (context) => ErrorView(message: viewModel.errorMessage, onRetry: () => viewModel.retry()),
        empty: EmptyView(message: 'No data'),
        builder: (context) => MyContent(),
      ),
    );
  }

  @override
  ScreenNameViewModel createViewModel() => ref.read(myScreenVm);

  @override
  String screenName() => "SCREEN_NAME";
}

// === VIEWMODEL FILE ===
import 'package:flutter/material.dart';
import 'package:gym_app/presentation/screens/base/base_view_model.dart';
import 'package:gym_app/repositories/my_repository.dart';

class ScreenNameViewModel extends BaseViewModel {
  ScreenNameViewModel(this._repository);
  final MyRepository _repository;
  List<MyModel> items = [];

  Future<void> loadData() async {
    await executeWithLoading(
      operation: () async {
        items = await _repository.getItems();
      },
    );
  }

  Future<void> retry() async => loadData();
}
```

---

## 9. Key File Paths

| Purpose | File Path |
|---------|-----------|
| Base ViewModel | `lib/presentation/screens/base/base_view_model.dart` |
| Base Consumer State | `lib/presentation/screens/base/base_consumer_state.dart` |
| Screen State Enum | `lib/presentation/screens/base/screen_state.dart` |
| Screen State Aware | `lib/presentation/screens/base/screen_state_aware.dart` |
| App Router | `lib/core/router/app_router.dart` |
| App Theme | `lib/core/theme/app_theme.dart` |
| API Service | `lib/services/api_service.dart` |
| Firebase Service | `lib/services/firebase_service.dart` |
| Local Storage | `lib/presentation/providers/local_storage_provider.dart` |
| Repo Provider | `lib/presentation/providers/repo_provider.dart` |
| VM Provider | `lib/presentation/providers/vm_provider.dart` |
| Auth Repository | `lib/repositories/auth_repository.dart` |
| Auth Mixin | `lib/mixin/auth_mixin.dart` |

---

## 10. How to Use

1. Create new Flutter project: `flutter create new_app`
2. Copy this folder structure into `lib/`
3. Copy `pubspec.yaml` dependencies
4. Run `flutter pub get`
5. Run `dart pub run build_runner build --delete-conflicting-outputs` for Freezed models
6. Implement splash, intro, auth modules using templates in Section 7
7. Add your own feature modules following Section 8 template
8. Register ViewModels in `vm_provider.dart`
9. Register Repositories in `repo_provider.dart`
10. Add routes in `app_router.dart`
11. Customize theme colors in `app_theme.dart`

Rules :
- Create responsive, should be support mobile (landscape and portrait)  and desktop
- Ask App name and bunddle name, if create from blank folder, if folder not blank, don't ask

12. Configure Firebase in `firebase_options.dart`
