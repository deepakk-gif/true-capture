import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../screens/auth/forgot_password/forgot_password_view_model.dart';
import '../screens/auth/otp/otp_view_model.dart';
import '../screens/auth/reset_password/reset_password_view_model.dart';
import '../screens/auth/sign_in/sign_in_viewmodel.dart';
import '../screens/auth/sign_up/sign_up_view_model.dart';
import '../screens/main/main_view_model.dart';
import '../screens/splash/splash_viewmodel.dart';
import 'local_storage_provider.dart';
import 'repo_provider.dart';
import 'user_data_provider.dart';

final splashVm = Provider.autoDispose<SplashViewmodel>((ref) {
  return SplashViewmodel(
    ref.read(authRepo),
    ref.read(localStorageServiceProvider),
    ref.read(authStateNotifierProvider.notifier),
  );
});

final signInViewModelProvider =
    Provider.autoDispose<SignInViewModel>((ref) {
  return SignInViewModel(
    ref.read(authRepo),
    ref.read(localStorageServiceProvider),
    ref.read(authStateNotifierProvider.notifier),
  );
});

final signUpViewModelProvider =
    Provider.autoDispose<SignUpViewModel>((ref) {
  return SignUpViewModel(
    ref.read(authRepo),
    ref.read(localStorageServiceProvider),
    ref.read(authStateNotifierProvider.notifier),
  );
});

final forgotPasswordViewModelProvider =
    Provider.autoDispose<ForgotPasswordViewModel>((ref) {
  return ForgotPasswordViewModel(ref.read(authRepo));
});

final otpViewModelProvider = Provider.autoDispose<OtpViewModel>((ref) {
  return OtpViewModel(
    ref.read(authRepo),
    ref.read(authStateNotifierProvider.notifier),
    ref.read(localStorageServiceProvider),
  );
});

final resetPasswordViewModelProvider =
    Provider.autoDispose<ResetPasswordViewModel>((ref) {
  return ResetPasswordViewModel(ref.read(authRepo));
});

final mainVm = Provider.autoDispose<MainViewModel>((ref) {
  return MainViewModel();
});
