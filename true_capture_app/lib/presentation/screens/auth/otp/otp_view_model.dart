import 'package:flutter/widgets.dart';

import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/router/app_router.dart';
import '../../../../network/dto/request/auth/otp_request.dart';
import '../../../../repositories/auth_repository.dart';
import '../../../../services/firebase_service.dart';
import '../../../../services/local_service.dart';
import '../../../providers/local_storage_provider.dart';
import '../../../providers/user_data_provider.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

class OtpViewModel extends BaseViewModel {
  OtpViewModel(this._authRepository, this._authStateNotifier, this._storage);

  final AuthRepository _authRepository;
  final AuthStateNotifier _authStateNotifier;
  final LocalStorageService _storage;

  /// Handles a 6-digit OTP submission. Behavior branches on [purpose]:
  /// - [OtpPurpose.verifyEmail]: calls `/verify-otp`, persists tokens, navigates to main.
  /// - [OtpPurpose.passwordReset]: forwards the code to the reset-password screen — the
  ///   actual OTP consumption happens inside `/reset-password`.
  Future<void> verify(
    BuildContext context, {
    required String email,
    required String code,
    required OtpPurpose purpose,
  }) async {
    await executeWithLoading(
      errorState: ScreenState.content,
      operation: () async {
        switch (purpose) {
          case OtpPurpose.verifyEmail:
            final fcmToken = await FirebaseService.cachedToken(_storage);
            final response = await _authRepository.verifyOtp(
              OtpVerifyRequest(
                email:      email,
                code:       code,
                purpose:    purpose,
                fcmToken:   fcmToken,
                deviceType: FirebaseService.currentDeviceType(),
              ),
            );
            await _authStateNotifier.saveToken(
              response.accessToken,
              refreshToken: response.refreshToken,
              accessExpiresAtUtc: response.accessExpiresAtUtc,
            );
            // The auth response carries no user object — load the full
            // profile from /api/users/me. Best-effort: verify succeeded.
            try {
              final user = await _authRepository.getProfile();
              await _authStateNotifier.setUser(user);
            } catch (_) {/* non-fatal */}
            await _storage.delete(StorageKeys.pendingVerifyEmailKey);
            if (!context.mounted) return;
            AppRouter.go(context, ScreenPath.routeMain);
            break;
          case OtpPurpose.passwordReset:
            if (!context.mounted) return;
            AppRouter.push(
              context,
              ScreenPath.routeResetPassword,
              extra: {'email': email, 'code': code},
            );
            break;
        }
      },
    );
  }

  Future<void> resend({
    required String email,
    required OtpPurpose purpose,
  }) async {
    await executeWithLoading(
      errorState: ScreenState.content,
      operation: () => _authRepository.sendOtp(
        OtpSendRequest(email: email, purpose: purpose),
      ),
    );
  }
}
