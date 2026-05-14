import 'dart:io';

import 'package:dio/dio.dart';

import 'response_error.dart';

class ErrorHandler {
  ErrorHandler._();

  static ResponseError handle(Object error) {
    if (error is ResponseError) return error;
    if (error is DioException) return _fromDio(error);
    if (error is SocketException) {
      return const ResponseError(message: 'No internet connection');
    }
    return ResponseError(message: error.toString());
  }

  static ResponseError _fromDio(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return const ResponseError(message: 'Request timed out');
      case DioExceptionType.connectionError:
        return const ResponseError(message: 'No internet connection');
      case DioExceptionType.cancel:
        return const ResponseError(message: 'Request cancelled');
      case DioExceptionType.badResponse:
        final data = e.response?.data;
        String message = 'Something went wrong';
        if (data is Map) {
          final errors = data['errors'];
          if (errors is List && errors.isNotEmpty) {
            message = errors.map((x) => x.toString()).join('; ');
          } else if (data['message'] is String) {
            message = data['message'] as String;
          }
        }
        return ResponseError(
          message: message,
          statusCode: e.response?.statusCode,
          data: data,
        );
      case DioExceptionType.badCertificate:
      case DioExceptionType.unknown:
        return ResponseError(message: e.message ?? 'Unknown error');
    }
  }
}

mixin NetworkParseHelper {
  T parseOrThrow<T>(Response response, T Function(Object? data) parser) {
    if (response.statusCode != null &&
        response.statusCode! >= 200 &&
        response.statusCode! < 300) {
      return parser(response.data);
    }
    throw ResponseError(
      message: 'Unexpected status ${response.statusCode}',
      statusCode: response.statusCode,
      data: response.data,
    );
  }
}
