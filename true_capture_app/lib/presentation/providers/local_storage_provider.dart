import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../services/local_service.dart';

class StorageKeys {
  StorageKeys._();

  static const String accessTokenKey = 'access_token';
  static const String refreshTokenKey = 'refresh_token';
  static const String isFirstSignUpDoneKey = 'check_first_sign_up_done';
  static const String isFirstIntroDoneKey = 'check_first_intro_done_value';
  static const String themeModeKey = 'theme_mode';
  static const String languageCodeKey = 'language_code';
  static const String isNotificationsEnabledKey = 'notifications_enabled';
  static const String faceLockStatusKey = 'face_lock_status';
  static const String fingurePrintLockStatusKey = 'fingure_lock_status';
  static const String biometricAsked = 'biometric_asked';
  static const String userIdKey = 'user_id';
}

final localStorageServiceProvider = Provider<LocalStorageService>((ref) {
  return LocalStorageService();
});
