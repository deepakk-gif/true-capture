/// Body for `POST /api/auth/refresh` and `POST /api/auth/logout`.
/// Matches backend `RefreshDto(RefreshToken)`.
class RefreshTokenRequest {
  const RefreshTokenRequest({required this.refreshToken});

  final String refreshToken;

  Map<String, dynamic> toJson() => {'refreshToken': refreshToken};
}
