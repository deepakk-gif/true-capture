class ApiResponse<T> {
  const ApiResponse({
    required this.success,
    this.message,
    this.data,
  });

  final bool success;
  final String? message;
  final T? data;

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json)? fromJsonT,
  ) {
    return ApiResponse<T>(
      success: json['success'] as bool? ?? false,
      message: json['message']?.toString(),
      data: fromJsonT != null ? fromJsonT(json['data']) : json['data'] as T?,
    );
  }
}
