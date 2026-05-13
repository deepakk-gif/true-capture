/// Body for `POST /api/auth/reset-password`. Matches backend `ResetPasswordDto(Email, Code, NewPassword)`.
class ResetPasswordRequest {
  const ResetPasswordRequest({
    required this.email,
    required this.code,
    required this.newPassword,
  });

  final String email;
  final String code;
  final String newPassword;

  Map<String, dynamic> toJson() => {
        'email':       email,
        'code':        code,
        'newPassword': newPassword,
      };
}
