import 'package:flutter/widgets.dart';

import '../../../../core/router/app_router.dart';
import '../../../../network/dto/request/auth/otp_request.dart';
import '../../../../repositories/auth_repository.dart';
import '../../../providers/user_data_provider.dart';
import '../../base/base_view_model.dart';

class OtpViewModel extends BaseViewModel {
  OtpViewModel(this._authRepository, this._authStateNotifier);

  final AuthRepository _authRepository;
  final AuthStateNotifier _authStateNotifier;

  Future<void> verify(
    BuildContext context, {
    required String email,
    required String otp,
  }) async {
    await executeWithLoading(
      operation: () async {
        final response = await _authRepository.verifyOtp(
          OtpVerifyRequest(email: email, otp: otp),
        );
        await _authStateNotifier.saveToken(
          response.accessToken,
          refreshToken: response.refreshToken,
        );
        if (response.user != null) {
          await _authStateNotifier.setUser(response.user!);
        }
        if (!context.mounted) return;
        AppRouter.go(context, ScreenPath.routeMain);
      },
    );
  }

  Future<void> resend(String email) async {
    await executeWithLoading(
      operation: () => _authRepository.sendOtp(OtpSendRequest(email: email)),
    );
  }
}
