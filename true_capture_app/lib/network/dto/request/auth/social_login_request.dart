import '../../../../enum/social_user_type.dart';

/// Generic social-login envelope. Currently only the `google` branch is wired
/// up against the backend; Apple / Facebook are stubbed in `SocialLoginService`
/// pending backend support. Keys are camelCase to match the backend's default
/// `JsonSerializerDefaults.Web` JSON policy.
class SocialLoginRequest {
  const SocialLoginRequest({
    required this.provider,
    required this.idToken,
    this.email,
    this.name,
    this.fcmToken,
    this.deviceType,
  });

  final SocialUserType provider;
  final String         idToken;
  final String?        email;
  final String?        name;
  final String?        fcmToken;
  final String?        deviceType;

  Map<String, dynamic> toJson() => {
        'provider': provider.apiValue,
        'idToken':  idToken,
        if (email      != null) 'email':      email,
        if (name       != null) 'name':       name,
        if (fcmToken   != null) 'fcmToken':   fcmToken,
        if (deviceType != null) 'deviceType': deviceType,
      };
}
