enum Environment { dev, staging, prod }

class AppConfig {
  AppConfig._();

  static Environment environment = Environment.dev;

  static String get baseUrl {
    switch (environment) {
      case Environment.dev:
        return 'https://api.dev.truecapture.com';
      case Environment.staging:
        return 'https://api.staging.truecapture.com';
      case Environment.prod:
        return 'https://api.truecapture.com';
    }
  }

  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);
  static const Duration sendTimeout = Duration(seconds: 30);

  static const String appName = 'True Capture';
}
