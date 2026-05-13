import 'package:flutter/widgets.dart';

import '../../../../core/router/app_router.dart';
import '../../../../network/dto/request/auth/reset_password_request.dart';
import '../../../../repositories/auth_repository.dart';
import '../../base/base_view_model.dart';

class ResetPasswordViewModel extends BaseViewModel {
  ResetPasswordViewModel(this._authRepository);

  final AuthRepository _authRepository;

  Future<void> submit(
    BuildContext context, {
    required String email,
    required String code,
    required String newPassword,
  }) async {
    await executeWithLoading(
      operation: () async {
        await _authRepository.resetPassword(
          ResetPasswordRequest(email: email, code: code, newPassword: newPassword),
        );
        if (!context.mounted) return;
        // Wipe nav stack and land on sign-in for a clean re-login with the new password.
        AppRouter.go(context, ScreenPath.routeSignIn);
      },
    );
  }
}
