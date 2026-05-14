/// Body for `POST /api/auth/register`.
/// Matches backend `RegisterDto(Email, Username, Password, FcmToken?, DeviceType?)`.
class SignUpRequest {
  const SignUpRequest({
    required this.email,
    required this.username,
    required this.password,
    this.fcmToken,
    this.deviceType,
  });

  final String  email;
  final String  username;
  final String  password;
  final String? fcmToken;
  final String? deviceType;

  Map<String, dynamic> toJson() => {
        'email':    email,
        'username': username,
        'password': password,
        if (fcmToken   != null) 'fcmToken':   fcmToken,
        if (deviceType != null) 'deviceType': deviceType,
      };
}
