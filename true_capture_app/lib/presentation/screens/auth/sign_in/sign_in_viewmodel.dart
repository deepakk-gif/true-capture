import 'package:flutter/widgets.dart';

import '../../../../core/router/app_router.dart';
import '../../../../enum/social_user_type.dart';
import '../../../../mixin/auth_mixin.dart';
import '../../../../network/dto/request/auth/sign_in_request.dart';
import '../../../../repositories/auth_repository.dart';
import '../../../../services/firebase_service.dart';
import '../../../../services/local_service.dart';
import '../../../providers/user_data_provider.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

class SignInViewModel extends BaseViewModel with AuthMixin {
  SignInViewModel(this._authRepository, this._storage, this._authStateNotifier);

  final AuthRepository      _authRepository;
  final LocalStorageService _storage;
  final AuthStateNotifier   _authStateNotifier;

  Future<void> signIn(
    BuildContext context, {
    required String email,
    required String password,
  }) async {
    await executeWithLoading(
      errorState: ScreenState.content,
      operation: () async {
        final fcmToken = await FirebaseService.cachedToken(_storage);
        final response = await _authRepository.signIn(
          SignInRequest(
            email:      email,
            password:   password,
            fcmToken:   fcmToken,
            deviceType: FirebaseService.currentDeviceType(),
          ),
        );
        await _authStateNotifier.saveToken(
          response.accessToken,
          refreshToken: response.refreshToken,
          accessExpiresAtUtc: response.accessExpiresAtUtc,
        );
        // The auth response carries no user object — load the full profile
        // from /api/users/me. Best-effort: login already succeeded, and the
        // profile re-loads on the next app entry if this call fails.
        try {
          final user = await _authRepository.getProfile();
          await _authStateNotifier.setUser(user);
        } catch (_) {/* non-fatal */}
        if (!context.mounted) return;
        AppRouter.go(context, ScreenPath.routeMain);
      },
    );
  }

  Future<void> signInWithProvider(
    BuildContext context,
    SocialUserType type,
  ) async {
    await executeWithLoading(
      errorState: ScreenState.content,
      operation: () async {
        await signInWithSocial(
          socialType:        type,
          authRepository:    _authRepository,
          authStateNotifier: _authStateNotifier,
          storage:           _storage,
          context:           context,
          onSuccess: () {
            if (!context.mounted) return;
            AppRouter.go(context, ScreenPath.routeMain);
          },
          // signInWithSocial swallows its own errors; forward them to the
          // shared error channel so they reach the user as a snackbar.
          onError: setError,
        );
      },
    );
  }
}
