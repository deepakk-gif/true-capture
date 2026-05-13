import 'package:dio/dio.dart';

import '../../services/local_service.dart';
import '../../presentation/providers/local_storage_provider.dart';

class AuthInterceptor extends Interceptor {
  AuthInterceptor(this._storage);

  final LocalStorageService _storage;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final token = await _storage.read(StorageKeys.accessTokenKey);
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }
}
