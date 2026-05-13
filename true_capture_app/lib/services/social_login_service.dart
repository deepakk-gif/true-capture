import '../enum/social_user_type.dart';
import '../log/app_logs.dart';

class SocialLoginResult {
  const SocialLoginResult({
    required this.provider,
    required this.idToken,
    this.email,
    this.name,
  });

  final SocialUserType provider;
  final String idToken;
  final String? email;
  final String? name;
}

class SocialLoginService {
  SocialLoginService._();
  static final SocialLoginService instance = SocialLoginService._();

  Future<SocialLoginResult?> signIn(SocialUserType type) async {
    appLog('SocialLoginService.signIn(${type.apiValue})');
    // TODO: Wire google_sign_in / sign_in_with_apple / flutter_facebook_auth
    return null;
  }

  Future<void> signOut() async {
    appLog('SocialLoginService.signOut');
    // TODO: Sign out from active provider
  }
}
