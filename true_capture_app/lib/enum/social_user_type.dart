enum SocialUserType {
  google,
  facebook,
  apple;

  String get apiValue {
    switch (this) {
      case SocialUserType.google:
        return 'google';
      case SocialUserType.facebook:
        return 'facebook';
      case SocialUserType.apple:
        return 'apple';
    }
  }
}
