import 'package:firebase_messaging/firebase_messaging.dart';

import '../log/app_logs.dart';

mixin FirebaseMessageManager {
  Future<void> _requestPermission() async {
    final settings = await FirebaseMessaging.instance.requestPermission(
      alert: true,
      badge: true,
      sound: true,
    );
    appLog('FCM permission: ${settings.authorizationStatus}', tag: 'FCM');
  }

  Future<String?> _getToken() async {
    final token = await FirebaseMessaging.instance.getToken();
    appLog('FCM token: $token', tag: 'FCM');
    return token;
  }

  void _setupForegroundHandler() {
    FirebaseMessaging.onMessage.listen((message) {
      appLog('Foreground message: ${message.messageId}', tag: 'FCM');
    });
  }

  void _setupBackgroundHandler() {
    FirebaseMessaging.onMessageOpenedApp.listen((message) {
      appLog('Opened from background: ${message.messageId}', tag: 'FCM');
    });
    FirebaseMessaging.instance.getInitialMessage().then((message) {
      if (message != null) {
        appLog('Opened from terminated: ${message.messageId}', tag: 'FCM');
      }
    });
  }

  void _setupTokenRefreshListener() {
    FirebaseMessaging.instance.onTokenRefresh.listen((token) {
      appLog('FCM token refreshed: $token', tag: 'FCM');
    });
  }
}

class FirebaseService with FirebaseMessageManager {
  FirebaseService._();
  static FirebaseService? _instance;

  static FirebaseService get instance {
    _instance ??= FirebaseService._();
    return _instance!;
  }

  Future<void> initialize() async {
    await _requestPermission();
    await _getToken();
    _setupForegroundHandler();
    _setupBackgroundHandler();
    _setupTokenRefreshListener();
  }
}
