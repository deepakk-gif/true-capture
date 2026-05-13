import 'package:flutter/material.dart';

import '../core/router/app_router.dart';
import '../enum/social_user_type.dart';
import '../log/app_logs.dart';
import '../network/dto/request/auth/google_sign_in_request.dart';
import '../presentation/providers/user_data_provider.dart';
import '../repositories/auth_repository.dart';
import '../services/social_login_service.dart';

mixin AuthMixin {
  /// Runs the full social-sign-in flow:
  ///   1. Gets a provider-issued ID token via [SocialLoginService].
  ///   2. POSTs it to the backend (`/api/auth/google` for Google).
  ///   3. Persists tokens via [AuthStateNotifier].
  ///   4. Navigates to the main shell.
  ///
  /// Surfaces nulls / errors via [onError]; calls [onSuccess] only after
  /// tokens are saved.
  Future<void> signInWithSocial({
    required SocialUserType   socialType,
    required AuthRepository   authRepository,
    required AuthStateNotifier authStateNotifier,
    required BuildContext     context,
    required VoidCallback     onSuccess,
    void Function(String message)? onError,
  }) async {
    try {
      appLog('Sign in with ${socialType.apiValue}');
      final social = await SocialLoginService.instance.signIn(socialType);
      if (social == null) {
        onError?.call('Sign-in was cancelled.');
        return;
      }

      if (socialType != SocialUserType.google) {
        // Backend only supports Google in MVP.
        onError?.call('${socialType.apiValue} sign-in is not yet supported.');
        return;
      }

      final response = await authRepository.googleSignIn(
        GoogleSignInRequest(idToken: social.idToken),
      );
      await authStateNotifier.saveToken(
        response.accessToken,
        refreshToken: response.refreshToken,
      );
      if (response.user != null) {
        await authStateNotifier.setUser(response.user!);
      }
      onSuccess();
    } catch (e, s) {
      appLogError(e, s, 'AUTH_MIXIN');
      onError?.call(e.toString());
    }
  }

  void navigateToSignIn(BuildContext context) =>
      AppRouter.go(context, ScreenPath.routeSignIn);

  void navigateToMain(BuildContext context) =>
      AppRouter.go(context, ScreenPath.routeMain);

  void navigateToIntro(BuildContext context) =>
      AppRouter.go(context, ScreenPath.routeIntro);
}
