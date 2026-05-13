import '../../../../enum/social_user_type.dart';

class SocialLoginRequest {
  const SocialLoginRequest({
    required this.provider,
    required this.idToken,
    this.email,
    this.name,
    this.fcmToken,
  });

  final SocialUserType provider;
  final String idToken;
  final String? email;
  final String? name;
  final String? fcmToken;

  Map<String, dynamic> toJson() => {
        'provider': provider.apiValue,
        'id_token': idToken,
        if (email != null) 'email': email,
        if (name != null) 'name': name,
        if (fcmToken != null) 'fcm_token': fcmToken,
      };
}
