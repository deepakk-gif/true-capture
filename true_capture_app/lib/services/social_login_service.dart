import 'package:google_sign_in/google_sign_in.dart';

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

/// Wraps the platform social-sign-in SDKs and surfaces a normalized
/// `SocialLoginResult` carrying the verifier-ready ID token the backend
/// expects.
class SocialLoginService {
  SocialLoginService._();
  static final SocialLoginService instance = SocialLoginService._();

  final GoogleSignIn _google = GoogleSignIn(
    scopes: const ['email', 'profile', 'openid'],
  );

  /// Returns null when the user cancelled or the platform did not yield an ID token.
  Future<SocialLoginResult?> signIn(SocialUserType type) async {
    appLog('SocialLoginService.signIn(${type.apiValue})');
    switch (type) {
      case SocialUserType.google:
        return _signInWithGoogle();
      case SocialUserType.apple:
      case SocialUserType.facebook:
        // Not wired under MVP — backend currently only accepts Google ID tokens.
        return null;
    }
  }

  Future<SocialLoginResult?> _signInWithGoogle() async {
    final account = await _google.signIn();
    if (account == null) return null;   // user cancelled

    final auth = await account.authentication;
    final idToken = auth.idToken;
    if (idToken == null || idToken.isEmpty) return null;

    return SocialLoginResult(
      provider: SocialUserType.google,
      idToken:  idToken,
      email:    account.email,
      name:     account.displayName,
    );
  }

  Future<void> signOut() async {
    appLog('SocialLoginService.signOut');
    try {
      await _google.signOut();
    } catch (e, s) {
      appLogError(e, s, 'SOCIAL_SIGN_OUT');
    }
  }
}
