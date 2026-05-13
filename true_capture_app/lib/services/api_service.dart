import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../network/helper/error_handler.dart';
import '../network/interceptors/auth_interceptor.dart';
import '../network/interceptors/error_interceptor.dart';
import '../network/interceptors/logging_interceptor.dart';
import 'local_service.dart';

class ApiService with NetworkParseHelper {
  ApiService._();
  static ApiService? _instance;
  static Dio? _dio;

  static ApiService get instance {
    _instance ??= ApiService._();
    return _instance!;
  }

  void initialize() {
    _dio = Dio(BaseOptions(
      baseUrl: AppConfig.baseUrl,
      connectTimeout: AppConfig.connectTimeout,
      receiveTimeout: AppConfig.receiveTimeout,
      sendTimeout: AppConfig.sendTimeout,
      contentType: 'application/json',
      responseType: ResponseType.json,
    ));
    _dio!.interceptors.addAll([
      AuthInterceptor(LocalStorageService()),
      LoggingInterceptor(),
      ErrorInterceptor(),
    ]);
  }

  Dio get dio {
    if (_dio == null) initialize();
    return _dio!;
  }

  Future<Response<T>> get<T>(String path,
          {Map<String, dynamic>? queryParameters}) =>
      dio.get<T>(path, queryParameters: queryParameters);

  Future<Response<T>> post<T>(String path, {Object? data}) =>
      dio.post<T>(path, data: data);

  Future<Response<T>> put<T>(String path, {Object? data}) =>
      dio.put<T>(path, data: data);

  Future<Response<T>> patch<T>(String path, {Object? data}) =>
      dio.patch<T>(path, data: data);

  Future<Response<T>> delete<T>(String path, {Object? data}) =>
      dio.delete<T>(path, data: data);
}
