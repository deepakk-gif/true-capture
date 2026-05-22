import '../core/constants/api_endpoints.dart';
import '../network/dto/activity_models.dart';
import '../services/api_service.dart';

/// The signed-in user's activity feed (notifications).
class NotificationRepository {
  NotificationRepository(this._api);

  final ApiService _api;

  Future<List<NotificationItem>> feed({String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.notifications,
      queryParameters: cursor == null ? null : {'cursor': cursor},
    );
    final items = (r.data?['items'] as List?) ?? const [];
    return items
        .map((e) => NotificationItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<int> unreadCount() async {
    final r = await _api.get<Map<String, dynamic>>(ApiEndpoints.notificationsUnread);
    return (r.data?['count'] as num?)?.toInt() ?? 0;
  }

  Future<void> markAllRead() => _api.post(ApiEndpoints.notificationsReadAll);
}
