class ResponseError implements Exception {
  const ResponseError({
    required this.message,
    this.statusCode,
    this.data,
  });

  final String message;
  final int? statusCode;
  final dynamic data;

  @override
  String toString() => 'ResponseError($statusCode): $message';
}
