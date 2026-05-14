/// Body for `POST /api/auth/login`.
/// Matches backend `LoginDto(Email, Password, FcmToken?, DeviceType?)`.
class SignInRequest {
  const SignInRequest({
    required this.email,
    required this.password,
    this.fcmToken,
    this.deviceType,
  });

  final String  email;
  final String  password;
  final String? fcmToken;
  final String? deviceType;

  Map<String, dynamic> toJson() => {
        'email': email,
        'password': password,
        if (fcmToken   != null) 'fcmToken':   fcmToken,
        if (deviceType != null) 'deviceType': deviceType,
      };
}
