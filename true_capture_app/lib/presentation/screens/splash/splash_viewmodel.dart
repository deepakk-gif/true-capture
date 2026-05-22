import 'dart:async';

import 'package:flutter/widgets.dart';

import '../../../core/router/app_router.dart';
import '../../../mixin/auth_mixin.dart';
import '../../../repositories/auth_repository.dart';
import '../../../services/local_service.dart';
import '../../providers/local_storage_provider.dart';
import '../../providers/user_data_provider.dart';
import '../base/base_view_model.dart';

class SplashViewmodel extends BaseViewModel with AuthMixin {
  SplashViewmodel(
    this._authRepository,
    this._localStorageService,
    this._authStateNotifier,
  );

  final AuthRepository _authRepository;
  final LocalStorageService _localStorageService;
  final AuthStateNotifier _authStateNotifier;

  Future<void> setupBeforeStart(BuildContext context) async {
    await Future<void>.delayed(const Duration(milliseconds: 800));
    await executeWithLoading(
      operation: () async {
        final token =
            await _localStorageService.read(StorageKeys.accessTokenKey);
        if (token == null || token.isEmpty) {
          if (!context.mounted) return;
          AppRouter.go(context, ScreenPath.routeIntro);
          return;
        }

        // Email-verification gate: /register issues a token before the user
        // completes OTP verify. The OTP screen is only meant to be reached
        // in-session, right after register. If the app was relaunched while a
        // verification was still pending, treat the half-finished registration
        // as abandoned — clear the unverified token (clear() also drops the
        // pending key) and send the user to sign-in instead of trapping them
        // on the OTP screen across restarts.
        final pendingEmail = await _localStorageService
            .read(StorageKeys.pendingVerifyEmailKey);
        if (pendingEmail != null && pendingEmail.isNotEmpty) {
          await _authStateNotifier.clear();
          if (!context.mounted) return;
          AppRouter.go(context, ScreenPath.routeSignIn);
          return;
        }

        // Fast path: the stored access token is still valid by its persisted
        // expiry — go straight to Main with no network round-trip (works
        // offline). Restore the cached profile so Main renders real data
        // immediately, then refresh it in the background (best-effort).
        if (await _authStateNotifier.hasValidAccessToken()) {
          await _authStateNotifier.loadCachedUser();
          if (!context.mounted) return;
          AppRouter.go(context, ScreenPath.routeMain);
          unawaited(_refreshProfileInBackground());
          return;
        }

        // Slow path: token has no stored expiry or is expired. Hit /users/me —
        // the refresh interceptor swaps an expired access token on a 401.
        try {
          final user = await _authRepository.getProfile();
          await _authStateNotifier.setUser(user);
          if (!context.mounted) return;
          AppRouter.go(context, ScreenPath.routeMain);
        } catch (_) {
          if (!context.mounted) return;
          AppRouter.go(context, ScreenPath.routeSignIn);
        }
      },
      errorCallBack: (error, stack, message) {
        if (!context.mounted) return;
        AppRouter.go(context, ScreenPath.routeIntro);
      },
    );
  }

  /// Best-effort profile refresh after the launch fast-path. A failure
  /// (offline / transient) is swallowed — the cached profile stays in place.
  Future<void> _refreshProfileInBackground() async {
    try {
      final user = await _authRepository.getProfile();
      await _authStateNotifier.setUser(user);
    } catch (_) {/* keep the cached profile */}
  }
}
