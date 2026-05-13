import 'package:flutter/widgets.dart';

import '../../../../core/router/app_router.dart';
import '../../../../repositories/auth_repository.dart';
import '../../base/base_view_model.dart';

class ForgotPasswordViewModel extends BaseViewModel {
  ForgotPasswordViewModel(this._authRepository);

  final AuthRepository _authRepository;

  Future<void> sendResetLink(
    BuildContext context, {
    required String email,
  }) async {
    await executeWithLoading(
      operation: () async {
        await _authRepository.forgotPassword(email);
        if (!context.mounted) return;
        AppRouter.push(
          context,
          ScreenPath.routeOtpVerify,
          extra: {'email': email},
        );
      },
    );
  }
}
