import 'package:flutter/widgets.dart';

import '../../../../core/constants/api_endpoints.dart' show OtpPurpose;
import '../../../../core/router/app_router.dart';
import '../../../../network/dto/request/auth/sign_up_request.dart';
import '../../../../repositories/auth_repository.dart';
import '../../base/base_view_model.dart';

class SignUpViewModel extends BaseViewModel {
  SignUpViewModel(this._authRepository);

  final AuthRepository _authRepository;

  Future<void> signUp(
    BuildContext context, {
    required String username,
    required String email,
    required String password,
  }) async {
    await executeWithLoading(
      operation: () async {
        await _authRepository.signUp(
          SignUpRequest(email: email, username: username, password: password),
        );
        if (!context.mounted) return;
        AppRouter.push(
          context,
          ScreenPath.routeOtpVerify,
          extra: {
            'email':   email,
            'purpose': OtpPurpose.verifyEmail,
          },
        );
      },
    );
  }
}
