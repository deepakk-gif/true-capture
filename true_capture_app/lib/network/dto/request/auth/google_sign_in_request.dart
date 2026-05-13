/// Body for `POST /api/auth/google`. Matches backend `GoogleSignInDto(IdToken)`.
class GoogleSignInRequest {
  const GoogleSignInRequest({required this.idToken});

  final String idToken;

  Map<String, dynamic> toJson() => {'idToken': idToken};
}
