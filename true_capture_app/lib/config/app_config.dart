enum Environment { dev, staging, prod, local }

class AppConfig {
  AppConfig._();

  static Environment environment = Environment.local;

  static String get baseUrl {
    switch (environment) {
      case Environment.dev:
        return 'https://api.dev.truecapture.com';
      case Environment.staging:
        return 'https://api.staging.truecapture.com';
      case Environment.prod:
        return 'https://api.truecapture.com';
      case Environment.local:
        return 'http://10.0.2.2:5080/';
    }
  }

  /// True when pointed at a localhost backend — the backend issues a fixed OTP
  /// in its Development environment, so the OTP screen can pre-fill it.
  static bool get isLocal => environment == Environment.local;

  /// Fixed OTP the backend's Development environment issues for every send.
  /// Pre-filled on the OTP screen when [isLocal] so localhost testing skips
  /// a real inbox. Must match `OtpService.DevFixedCode` on the backend.
  static const String localTestOtp = '123456';

  /// Resolves a possibly-relative media path returned by the backend (e.g.
  /// "/media/avatars/x.jpg") into an absolute URL against [baseUrl]. Absolute
  /// URLs (S3/CDN) are returned unchanged. Returns null for null/empty input.
  static String? resolveUrl(String? path) {
    if (path == null || path.isEmpty) return null;
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    final base = baseUrl.endsWith('/')
        ? baseUrl.substring(0, baseUrl.length - 1)
        : baseUrl;
    return '$base/${path.startsWith('/') ? path.substring(1) : path}';
  }

  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);
  static const Duration sendTimeout = Duration(seconds: 30);

  static const String appName = 'True Capture';
}
