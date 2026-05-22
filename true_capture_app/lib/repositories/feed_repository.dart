import '../core/constants/api_endpoints.dart';
import '../network/dto/post_models.dart';
import '../services/api_service.dart';

/// Feed channels — the Home tab (Normal posts) and the Fake vs Real tab.
class FeedChannel {
  FeedChannel._();
  static const String home       = 'home';
  static const String fakeVsReal = 'fake_vs_real';
}

/// Reads the paged post feed for the Home and Fake vs Real tabs.
class FeedRepository {
  FeedRepository(this._api);

  final ApiService _api;

  /// `GET /api/feed?channel=&cursor=` — one page of posts, newest first.
  Future<FeedResult> getFeed(String channel, {String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.feed,
      queryParameters: {
        'channel': channel,
        'cursor': ?cursor,
      },
    );
    return FeedResult.fromJson(r.data!);
  }
}
