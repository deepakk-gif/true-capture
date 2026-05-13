import 'dart:async';

import 'package:dio/dio.dart';

import '../../core/constants/api_endpoints.dart';
import '../../services/local_service.dart';
import '../../presentation/providers/local_storage_provider.dart';

/// Intercepts `401 Unauthorized` responses, exchanges the stored refresh token
/// for a new access/refresh pair via `POST /api/auth/refresh`, persists them,
/// and replays the original request exactly once.
///
/// Concurrent 401s are coalesced through a single in-flight `_refreshFuture` so
/// a burst of failed calls produces exactly one refresh round-trip.
class RefreshInterceptor extends Interceptor {
  RefreshInterceptor(this._dio, this._storage);

  final Dio                _dio;
  final LocalStorageService _storage;

  Future<String?>? _refreshFuture;

  static const String _retriedFlag = 'tc_retried_after_refresh';

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    final status = err.response?.statusCode;
    final req    = err.requestOptions;
    final path   = req.path;

    // Only retry on 401, never on the auth endpoints themselves, and only once
    // per request. Bail on the rest to the error chain.
    final isAuthCall = path.startsWith('/api/auth/');
    final alreadyTried = req.extra[_retriedFlag] == true;

    if (status != 401 || isAuthCall || alreadyTried) {
      handler.next(err);
      return;
    }

    try {
      final newAccessToken = await (_refreshFuture ??= _doRefresh());
      _refreshFuture = null;

      if (newAccessToken == null) {
        handler.next(err);
        return;
      }

      // Clone the original request with the new token + retry flag.
      req.headers['Authorization'] = 'Bearer $newAccessToken';
      req.extra[_retriedFlag]       = true;

      final retried = await _dio.fetch<dynamic>(req);
      handler.resolve(retried);
    } catch (_) {
      _refreshFuture = null;
      handler.next(err);
    }
  }

  Future<String?> _doRefresh() async {
    final refreshToken = await _storage.read(StorageKeys.refreshTokenKey);
    if (refreshToken == null || refreshToken.isEmpty) return null;

    // Use a bare Dio so we don't recurse through this same interceptor.
    final bare = Dio(BaseOptions(baseUrl: _dio.options.baseUrl));
    final res = await bare.post<Map<String, dynamic>>(
      ApiEndpoints.refresh,
      data: {'refreshToken': refreshToken},
    );

    final data = res.data ?? const {};
    final access  = (data['accessToken']  ?? data['access_token'])  as String?;
    final refresh = (data['refreshToken'] ?? data['refresh_token']) as String?;
    if (access == null || access.isEmpty) return null;

    await _storage.write(StorageKeys.accessTokenKey, access);
    if (refresh != null && refresh.isNotEmpty) {
      await _storage.write(StorageKeys.refreshTokenKey, refresh);
    }
    return access;
  }
}
