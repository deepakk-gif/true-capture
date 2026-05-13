import 'package:flutter/material.dart';

import '../core/router/app_router.dart';
import '../enum/social_user_type.dart';
import '../log/app_logs.dart';

mixin AuthMixin {
  Future<void> signInWithSocial({
    required SocialUserType socialType,
    required VoidCallback onSuccess,
    void Function(String message)? onError,
  }) async {
    try {
      appLog('Sign in with ${socialType.apiValue}');
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
