/// Body for `POST /api/auth/forgot-password`. Matches backend `ForgotPasswordDto(Email)`.
class ForgotPasswordRequest {
  const ForgotPasswordRequest({required this.email});

  final String email;

  Map<String, dynamic> toJson() => {'email': email};
}
