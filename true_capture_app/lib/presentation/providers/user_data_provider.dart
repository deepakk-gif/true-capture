import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../network/dto/response/auth/user_response.dart';
import '../../services/local_service.dart';
import 'local_storage_provider.dart';

class AuthStateNotifier extends StateNotifier<UserResponse?> {
  AuthStateNotifier(this._storage) : super(null);

  final LocalStorageService _storage;

  /// Treat a token as expired this long *before* its real expiry, so the
  /// launch fast-path never hands Main a token that dies moments later.
  static const Duration _expirySkew = Duration(seconds: 30);

  Future<void> saveToken(
    String accessToken, {
    String? refreshToken,
    DateTime? accessExpiresAtUtc,
  }) async {
    await _storage.write(StorageKeys.accessTokenKey, accessToken);
    if (refreshToken != null) {
      await _storage.write(StorageKeys.refreshTokenKey, refreshToken);
    }
    // Persist expiry so launch can judge token validity locally (offline).
    // Delete on null so a stale expiry can't outlive its token.
    if (accessExpiresAtUtc != null) {
      await _storage.write(
        StorageKeys.accessExpiresAtKey,
        accessExpiresAtUtc.toUtc().toIso8601String(),
      );
    } else {
      await _storage.delete(StorageKeys.accessExpiresAtKey);
    }
  }

  Future<void> setUser(UserResponse user) async {
    state = user;
    await _storage.write(StorageKeys.userIdKey, user.id);
    // Cache the full profile so Main/Profile render real data instantly on
    // the next launch — even offline.
    await _storage.write(
      StorageKeys.userProfileKey,
      jsonEncode(user.toJson()),
    );
  }

  /// Loads the cached user profile into state without a network call.
  /// Returns the user, or null when nothing is cached / it fails to parse.
  Future<UserResponse?> loadCachedUser() async {
    final raw = await _storage.read(StorageKeys.userProfileKey);
    if (raw == null || raw.isEmpty) return null;
    try {
      final user =
          UserResponse.fromJson(jsonDecode(raw) as Map<String, dynamic>);
      state = user;
      return user;
    } catch (_) {
      return null;
    }
  }

  /// True only when a non-empty access token is stored and its persisted
  /// expiry is still in the future (minus [_expirySkew]). False when the
  /// token is missing/expired, or when no expiry was stored — in which case
  /// the caller must verify the token online instead.
  Future<bool> hasValidAccessToken() async {
    final token = await _storage.read(StorageKeys.accessTokenKey);
    if (token == null || token.isEmpty) return false;
    final raw = await _storage.read(StorageKeys.accessExpiresAtKey);
    final expiry = raw == null ? null : DateTime.tryParse(raw);
    if (expiry == null) return false;
    return DateTime.now()
        .toUtc()
        .isBefore(expiry.toUtc().subtract(_expirySkew));
  }

  Future<void> clear() async {
    state = null;
    await _storage.delete(StorageKeys.accessTokenKey);
    await _storage.delete(StorageKeys.refreshTokenKey);
    await _storage.delete(StorageKeys.accessExpiresAtKey);
    await _storage.delete(StorageKeys.userIdKey);
    await _storage.delete(StorageKeys.userProfileKey);
    await _storage.delete(StorageKeys.pendingVerifyEmailKey);
  }
}

final authStateNotifierProvider =
    StateNotifierProvider<AuthStateNotifier, UserResponse?>((ref) {
  return AuthStateNotifier(ref.read(localStorageServiceProvider));
});
