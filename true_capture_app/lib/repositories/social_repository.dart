import '../core/constants/api_endpoints.dart';
import '../network/dto/social_models.dart';
import '../services/api_service.dart';

/// User search + the follow graph + viewing other users' profiles.
/// Talks to the backend `Modules.Social` endpoints.
class SocialRepository {
  SocialRepository(this._api);

  final ApiService _api;

  Future<List<UserSearchItem>> search(String query) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.userSearch,
      queryParameters: {'q': query},
    );
    final items = (r.data?['items'] as List?) ?? const [];
    return items
        .map((e) => UserSearchItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<UserProfileView> profile(int userId) async {
    final r = await _api.get<Map<String, dynamic>>(ApiEndpoints.userById(userId));
    return UserProfileView.fromJson(r.data!);
  }

  Future<FollowActionResult> follow(int userId) async {
    final r = await _api.post<Map<String, dynamic>>(ApiEndpoints.followUser(userId));
    return FollowActionResult.fromJson(r.data!);
  }

  Future<FollowActionResult> unfollow(int userId) async {
    final r = await _api.delete<Map<String, dynamic>>(ApiEndpoints.followUser(userId));
    return FollowActionResult.fromJson(r.data!);
  }

  Future<FollowListResult> followers(int userId, {String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.userFollowers(userId),
      queryParameters: cursor == null ? null : {'cursor': cursor},
    );
    return FollowListResult.fromJson(r.data!);
  }

  Future<FollowListResult> following(int userId, {String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.userFollowing(userId),
      queryParameters: cursor == null ? null : {'cursor': cursor},
    );
    return FollowListResult.fromJson(r.data!);
  }

  Future<FollowListResult> followRequests({String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.followRequests,
      queryParameters: cursor == null ? null : {'cursor': cursor},
    );
    return FollowListResult.fromJson(r.data!);
  }

  Future<void> acceptRequest(int requesterId) =>
      _api.post(ApiEndpoints.acceptRequest(requesterId));

  Future<void> rejectRequest(int requesterId) =>
      _api.post(ApiEndpoints.rejectRequest(requesterId));

  Future<PostListResult> userPosts(int userId, {String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.userPosts(userId),
      queryParameters: cursor == null ? null : {'cursor': cursor},
    );
    return PostListResult.fromJson(r.data!);
  }
}
