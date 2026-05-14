import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../network/dto/response/auth/user_response.dart';
import '../../services/local_service.dart';
import 'local_storage_provider.dart';

class AuthStateNotifier extends StateNotifier<UserResponse?> {
  AuthStateNotifier(this._storage) : super(null);

  final LocalStorageService _storage;

  Future<void> saveToken(String accessToken, {String? refreshToken}) async {
    await _storage.write(StorageKeys.accessTokenKey, accessToken);
    if (refreshToken != null) {
      await _storage.write(StorageKeys.refreshTokenKey, refreshToken);
    }
  }

  Future<void> setUser(UserResponse user) async {
    state = user;
    await _storage.write(StorageKeys.userIdKey, user.id);
  }

  Future<void> clear() async {
    state = null;
    await _storage.delete(StorageKeys.accessTokenKey);
    await _storage.delete(StorageKeys.refreshTokenKey);
    await _storage.delete(StorageKeys.userIdKey);
    await _storage.delete(StorageKeys.pendingVerifyEmailKey);
  }
}

final authStateNotifierProvider =
    StateNotifierProvider<AuthStateNotifier, UserResponse?>((ref) {
  return AuthStateNotifier(ref.read(localStorageServiceProvider));
});
