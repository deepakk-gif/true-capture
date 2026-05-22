import '../core/constants/api_endpoints.dart';
import '../network/dto/post_models.dart';
import '../network/dto/social_models.dart';
import '../services/api_service.dart';

/// Post create / read / delete plus all engagement: like, save, share, vote,
/// report and comments (with 1-level replies + comment likes).
class PostRepository {
  PostRepository(this._api);

  final ApiService _api;

  /// `POST /api/posts` — create a Normal or Fake-vs-Real post from finalized media.
  Future<PostDto> create({
    required String type,
    required List<int> mediaAssetIds,
    String? caption,
    List<String>? references,
  }) async {
    final r = await _api.post<Map<String, dynamic>>(ApiEndpoints.posts, data: {
      'type': type,
      'mediaAssetIds': mediaAssetIds,
      'caption': caption,
      'references': references,
    });
    return PostDto.fromJson(r.data!);
  }

  Future<void> delete(int postId) => _api.delete(ApiEndpoints.postById(postId));

  /// `GET /api/posts/{id}` — full post detail; the backend records a view.
  Future<PostDto> detail(int postId) async {
    final r = await _api.get<Map<String, dynamic>>(ApiEndpoints.postById(postId));
    return PostDto.fromJson(r.data!);
  }

  /// `POST /api/posts/{id}/like` — toggle a like.
  Future<LikeResult> toggleLike(int postId) async {
    final r = await _api.post<Map<String, dynamic>>(ApiEndpoints.postLike(postId));
    return LikeResult.fromJson(r.data!);
  }

  /// `POST /api/posts/{id}/save` — toggle a bookmark; returns the new saved state.
  Future<bool> toggleSave(int postId) async {
    final r = await _api.post<Map<String, dynamic>>(ApiEndpoints.postSave(postId));
    return r.data?['saved'] == true;
  }

  /// `POST /api/posts/{id}/share` — returns the canonical share URL.
  Future<String> share(int postId) async {
    final r = await _api.post<Map<String, dynamic>>(ApiEndpoints.postShare(postId));
    return r.data?['url']?.toString() ?? '';
  }

  /// `POST /api/posts/{id}/vote` — vote real (`true`) / fake (`false`).
  Future<VoteResult> vote(int postId, bool value) async {
    final r = await _api.post<Map<String, dynamic>>(
      ApiEndpoints.postVote(postId),
      data: {'value': value},
    );
    return VoteResult.fromJson(r.data!);
  }

  /// `POST /api/posts/{id}/report` — file a report against a post.
  Future<void> report(int postId, String reason, String? otherText) => _api.post(
        ApiEndpoints.postReport(postId),
        data: {'reason': reason, 'otherText': otherText},
      );

  /// `GET /api/posts/{id}/comments` — top-level comments, oldest first.
  Future<CommentListResult> comments(int postId, {String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.postComments(postId),
      queryParameters: {'cursor': ?cursor},
    );
    return CommentListResult.fromJson(r.data!);
  }

  /// `GET /api/comments/{id}/replies` — replies to a comment.
  Future<CommentListResult> replies(int commentId, {String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.commentReplies(commentId),
      queryParameters: {'cursor': ?cursor},
    );
    return CommentListResult.fromJson(r.data!);
  }

  /// `POST /api/posts/{id}/comments` — add a comment or a 1-level reply.
  Future<CommentDto> addComment(int postId, String text, {int? parentCommentId}) async {
    final r = await _api.post<Map<String, dynamic>>(
      ApiEndpoints.postComments(postId),
      data: {'text': text, 'parentCommentId': parentCommentId},
    );
    return CommentDto.fromJson(r.data!);
  }

  /// `POST /api/comments/{id}/like` — toggle a like on a comment.
  Future<LikeResult> toggleCommentLike(int commentId) async {
    final r = await _api.post<Map<String, dynamic>>(ApiEndpoints.commentLike(commentId));
    return LikeResult.fromJson(r.data!);
  }

  Future<void> deleteComment(int commentId) =>
      _api.delete(ApiEndpoints.commentById(commentId));

  /// `GET /api/users/me/saves` — the caller's bookmarked posts.
  Future<PostListResult> saved({String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.mySaves,
      queryParameters: {'cursor': ?cursor},
    );
    return PostListResult.fromJson(r.data!);
  }
}
