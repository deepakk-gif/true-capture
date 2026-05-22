import 'dart:convert';

import 'package:dio/dio.dart';

import '../../log/app_logs.dart';

class LoggingInterceptor extends Interceptor {
  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    appLog('==> ${options.method} ${options.uri}', tag: 'HTTP');
    final data = options.data;
    if (data != null) {
      appLog('Body: ${_describeBody(data)}', tag: 'HTTP');
    }
    handler.next(options);
  }

  /// Renders a request body for logging without assuming it's JSON-encodable.
  /// Streams (raw-bytes uploads) and [FormData] (multipart) would otherwise
  /// make `jsonEncode` throw, which Dio surfaces as an opaque `unknown` error.
  String _describeBody(Object data) {
    if (data is Stream) return '<stream>';
    if (data is List<int>) return '<${data.length} bytes>';
    if (data is FormData) {
      final fields = data.fields.map((e) => '${e.key}=${e.value}').join(', ');
      final files = data.files.map((e) => e.key).join(', ');
      return 'FormData(fields: {$fields}, files: [$files])';
    }
    try {
      return jsonEncode(data);
    } catch (_) {
      return data.toString();
    }
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
