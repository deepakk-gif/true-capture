/// Body for `POST /api/auth/google`.
/// Matches backend `GoogleSignInDto(IdToken, FcmToken?, DeviceType?)`.
class GoogleSignInRequest {
  const GoogleSignInRequest({
    required this.idToken,
    this.fcmToken,
    this.deviceType,
  });

  final String  idToken;
  final String? fcmToken;
  final String? deviceType;

  Map<String, dynamic> toJson() => {
        'idToken': idToken,
        if (fcmToken   != null) 'fcmToken':   fcmToken,
        if (deviceType != null) 'deviceType': deviceType,
      };
}
