class SignInRequest {
  const SignInRequest({
    required this.email,
    required this.password,
    this.fcmToken,
  });

  final String email;
  final String password;
  final String? fcmToken;

  Map<String, dynamic> toJson() => {
        'email': email,
        'password': password,
        if (fcmToken != null) 'fcm_token': fcmToken,
      };
}
