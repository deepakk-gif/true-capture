import 'dart:io' show Platform;

import 'package:firebase_messaging/firebase_messaging.dart';

import '../log/app_logs.dart';
import '../presentation/providers/local_storage_provider.dart';
import 'local_service.dart';

mixin FirebaseMessageManager {
  Future<void> _requestPermission() async {
    final settings = await FirebaseMessaging.instance.requestPermission(
      alert: true,
      badge: true,
      sound: true,
    );
    appLog('FCM permission: ${settings.authorizationStatus}', tag: 'FCM');
  }

  Future<String?> _getToken(LocalStorageService storage) async {
    final token = await FirebaseMessaging.instance.getToken();
    appLog('FCM token: $token', tag: 'FCM');
    if (token != null && token.isNotEmpty) {
      await storage.write(StorageKeys.fcmTokenKey, token);
    }
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

  void _setupTokenRefreshListener(LocalStorageService storage) {
    FirebaseMessaging.instance.onTokenRefresh.listen((token) async {
      appLog('FCM token refreshed: $token', tag: 'FCM');
      await storage.write(StorageKeys.fcmTokenKey, token);
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

  /// `"ios"` / `"android"` / `"web"`. Sent to the backend so an admin can
  /// filter notification targets by platform later.
  static String currentDeviceType() {
    if (Platform.isIOS) return 'ios';
    if (Platform.isAndroid) return 'android';
    return 'web';
  }

  /// Reads the most recent cached FCM token from secure storage. `null` if the
  /// device hasn't produced one yet (initial first-run before permission grant).
  static Future<String?> cachedToken(LocalStorageService storage) =>
      storage.read(StorageKeys.fcmTokenKey);

  Future<void> initialize() async {
    final storage = LocalStorageService();
    await _requestPermission();
    await _getToken(storage);
    await storage.write(StorageKeys.deviceTypeKey, currentDeviceType());
    _setupForegroundHandler();
    _setupBackgroundHandler();
    _setupTokenRefreshListener(storage);
  }
}
