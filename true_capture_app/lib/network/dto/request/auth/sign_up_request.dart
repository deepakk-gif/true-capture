/// Body for `POST /api/auth/register`. Matches backend `RegisterDto(Email, Username, Password)`.
class SignUpRequest {
  const SignUpRequest({
    required this.email,
    required this.username,
    required this.password,
  });

  final String email;
  final String username;
  final String password;

  Map<String, dynamic> toJson() => {
        'email':    email,
        'username': username,
        'password': password,
      };
}
