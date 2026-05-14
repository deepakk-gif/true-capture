import 'dart:convert';

import 'package:dio/dio.dart';

import '../../log/app_logs.dart';

class LoggingInterceptor extends Interceptor {
  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    appLog('==> ${options.method} ${options.uri}', tag: 'HTTP');
    if (options.data != null) {
      appLog('Body: ${jsonEncode(options.data)}', tag: 'HTTP');
    }
    handler.next(options);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    appLog(
      '<== ${response.statusCode} ${response.requestOptions.uri}',
      tag: 'HTTP',
    );
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    appLog(
      'XX  ${err.response?.statusCode} ${err.requestOptions.uri} :: ${err.message}',
      tag: 'HTTP',
    );
    handler.next(err);
  }
}
