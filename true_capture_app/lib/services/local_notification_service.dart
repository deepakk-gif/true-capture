import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import '../core/router/app_router.dart';
import '../log/app_logs.dart';

/// Shows local notifications for incoming chat messages (when the user is not on
/// that chat screen) and routes a tap to the conversation.
class LocalNotificationService {
  LocalNotificationService._();
  static final LocalNotificationService instance = LocalNotificationService._();

  final _plugin = FlutterLocalNotificationsPlugin();
  bool _ready = false;

  static const _channelId = 'messages';

  Future<void> initialize() async {
    if (_ready) return;
    const settings = InitializationSettings(
      android: AndroidInitializationSettings('@mipmap/ic_launcher'),
      iOS: DarwinInitializationSettings(),
    );
    await _plugin.initialize(
      settings,
      onDidReceiveNotificationResponse: (resp) => _route(resp.payload),
    );
    _ready = true;
  }

  /// Shows a message notification. [conversationId] is carried as the tap payload.
  Future<void> showMessage({
    required int conversationId,
    required String title,
    required String body,
  }) async {
    if (!_ready) await initialize();
    const details = NotificationDetails(
      android: AndroidNotificationDetails(
        _channelId,
        'Messages',
        channelDescription: 'New chat messages',
        importance: Importance.high,
        priority: Priority.high,
      ),
      iOS: DarwinNotificationDetails(),
    );
    await _plugin.show(conversationId, title, body, details,
        payload: conversationId.toString());
  }

  /// If the app was launched by tapping a local notification, returns its
  /// conversation-id payload so the splash flow can deep-link to the chat.
  Future<int?> launchConversationId() async {
    final launch = await _plugin.getNotificationAppLaunchDetails();
    if (launch?.didNotificationLaunchApp != true) return null;
    return int.tryParse(launch?.notificationResponse?.payload ?? '');
  }

  void _route(String? payload) {
    final convId = int.tryParse(payload ?? '');
    if (convId == null) return;
    appLog('Local notification tapped — conversation $convId', tag: 'NOTIF');
    AppRouter.router.push(ScreenPath.routeChat, extra: {'conversationId': convId});
  }
}
