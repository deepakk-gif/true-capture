import 'package:flutter/widgets.dart';

import '../../../../core/constants/api_endpoints.dart' show OtpPurpose;
import '../../../../core/router/app_router.dart';
import '../../../../network/dto/request/auth/forgot_password_request.dart';
import '../../../../repositories/auth_repository.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

class ForgotPasswordViewModel extends BaseViewModel {
  ForgotPasswordViewModel(this._authRepository);

  final AuthRepository _authRepository;

  Future<void> sendResetLink(
    BuildContext context, {
    required String email,
  }) async {
    await executeWithLoading(
      errorState: ScreenState.content,
      operation: () async {
        await _authRepository.forgotPassword(
          ForgotPasswordRequest(email: email),
        );
        if (!context.mounted) return;
        AppRouter.push(
          context,
          ScreenPath.routeOtpVerify,
          extra: {
            'email':   email,
            'purpose': OtpPurpose.passwordReset,
          },
        );
      },
    );
  }
}
