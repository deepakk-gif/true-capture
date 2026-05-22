import 'package:flutter/widgets.dart';

import '../../../../core/constants/api_endpoints.dart' show OtpPurpose;
import '../../../../core/router/app_router.dart';
import '../../../../network/dto/request/auth/sign_up_request.dart';
import '../../../../repositories/auth_repository.dart';
import '../../../../services/firebase_service.dart';
import '../../../../services/local_service.dart';
import '../../../providers/local_storage_provider.dart';
import '../../../providers/user_data_provider.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

class SignUpViewModel extends BaseViewModel {
  SignUpViewModel(this._authRepository, this._storage, this._authStateNotifier);

  final AuthRepository _authRepository;
  final LocalStorageService _storage;
  final AuthStateNotifier _authStateNotifier;

  Future<void> signUp(
    BuildContext context, {
    required String username,
    required String email,
    required String password,
  }) async {
    await executeWithLoading(
      errorState: ScreenState.content,
      operation: () async {
        final fcmToken = await FirebaseService.cachedToken(_storage);
        final response = await _authRepository.signUp(
          SignUpRequest(
            email:      email,
            username:   username,
            password:   password,
            fcmToken:   fcmToken,
            deviceType: FirebaseService.currentDeviceType(),
          ),
        );

        // Backend already issues tokens on register; persist them so the user
        // stays in a "pending verification" state across an app relaunch.
        await _authStateNotifier.saveToken(
          response.accessToken,
          refreshToken: response.refreshToken,
          accessExpiresAtUtc: response.accessExpiresAtUtc,
        );
        await _storage.write(StorageKeys.pendingVerifyEmailKey, email);

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
