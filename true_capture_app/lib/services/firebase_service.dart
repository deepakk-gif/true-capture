import 'dart:io' show Platform;

import 'package:firebase_messaging/firebase_messaging.dart';

import '../core/router/app_router.dart';
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
      _handleNotificationTap(message);
    });
    // Terminated-state taps can't deep-navigate here — the app is still booting
    // through the splash route. Stash the payload; the main shell consumes it
    // post-bootstrap via FirebaseService.takePendingLaunch().
    FirebaseMessaging.instance.getInitialMessage().then((message) {
      if (message != null) {
        appLog('Opened from terminated: ${message.messageId}', tag: 'FCM');
        FirebaseService.pendingLaunchData = message.data;
      }
    });
  }

  /// Routes a notification tap by its `type` data payload. Activity pushes
  /// (follows, likes, comments, mentions, …) open the in-app activity feed;
  /// `message` pushes deep-link straight into the chat.
  void _handleNotificationTap(RemoteMessage message) =>
      FirebaseService.routeNotificationData(message.data);

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

  /// Notification data payload captured when the app was launched from a
  /// terminated state — consumed once by the main shell after bootstrap.
  static Map<String, dynamic>? pendingLaunchData;

  /// Returns and clears any terminated-launch payload.
  static Map<String, dynamic>? takePendingLaunch() {
    final data = pendingLaunchData;
    pendingLaunchData = null;
    return data;
  }

  /// Routes a notification payload to its target screen. Reused for foreground
  /// taps and the terminated-state launch payload.
  static void routeNotificationData(Map<String, dynamic> data) {
    switch (data['type']?.toString()) {
      case 'activity':
      case 'admin_notification':
        AppRouter.router.push(ScreenPath.routeNotifications);
        break;
      case 'message':
        final convId = int.tryParse(data['conversationId']?.toString() ?? '');
        if (convId != null) {
          AppRouter.router.push(ScreenPath.routeChat, extra: {
            'conversationId': convId,
            'title': data['senderName']?.toString(),
          });
        }
        break;
    }
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
