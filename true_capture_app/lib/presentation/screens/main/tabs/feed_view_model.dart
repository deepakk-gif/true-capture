import '../../../../log/app_logs.dart';
import '../../../../network/dto/post_models.dart';
import '../../../../network/helper/error_handler.dart';
import '../../../../repositories/feed_repository.dart';
import '../../../../repositories/post_repository.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

/// Backs a feed tab (Home or Fake vs Real). Holds the post list, drives
/// pull-to-refresh + cursor pagination, and applies optimistic engagement edits.
class FeedViewModel extends BaseViewModel {
  FeedViewModel(this._feedRepo, this._postRepo, this.channel);

  final FeedRepository _feedRepo;
  final PostRepository _postRepo;
  final String channel;

  List<PostDto> posts = [];
  String? _cursor;
  bool _loadingMore = false;

  bool get hasMore => _cursor != null;
  bool get loadingMore => _loadingMore;

  Future<void> load() async {
    await executeWithLoading(
      initialState: ScreenState.progress,
      operation: () async {
        final res = await _feedRepo.getFeed(channel);
        posts = res.items;
        _cursor = res.nextCursor;
      },
    );
    if (!hasError) {
      changeScreenState(posts.isEmpty ? ScreenState.empty : ScreenState.content);
    }
  }

  /// Pull-to-refresh — reloads page one without flipping to the loading view.
  Future<void> refresh() async {
    try {
      final res = await _feedRepo.getFeed(channel);
      posts = res.items;
      _cursor = res.nextCursor;
      changeScreenState(posts.isEmpty ? ScreenState.empty : ScreenState.content);
      notifyListeners();
    } catch (e, s) {
      appLogError(e, s, 'FEED');
      setError(ErrorHandler.handle(e).message);
    }
  }

  Future<void> loadMore() async {
    if (_loadingMore || _cursor == null) return;
    _loadingMore = true;
    notifyListeners();
    try {
      final res = await _feedRepo.getFeed(channel, cursor: _cursor);
      posts = [...posts, ...res.items];
      _cursor = res.nextCursor;
    } catch (e, s) {
      appLogError(e, s, 'FEED');
      setError(ErrorHandler.handle(e).message);
    } finally {
      _loadingMore = false;
      notifyListeners();
    }
  }

  Future<void> toggleLike(PostDto p) async {
    final wasLiked = p.likedByMe;
    final oldCount = p.likeCount;
    p.likedByMe = !wasLiked;
    p.likeCount = oldCount + (wasLiked ? -1 : 1);
    notifyListeners();
    try {
      final r = await _postRepo.toggleLike(p.id);
      p.likedByMe = r.liked;
      p.likeCount = r.count;
    } catch (e, s) {
      p.likedByMe = wasLiked;
      p.likeCount = oldCount;
      appLogError(e, s, 'FEED');
      setError(ErrorHandler.handle(e).message);
    }
    notifyListeners();
  }

  Future<void> toggleSave(PostDto p) async {
    final was = p.savedByMe;
    p.savedByMe = !was;
    notifyListeners();
    try {
      p.savedByMe = await _postRepo.toggleSave(p.id);
    } catch (e, s) {
      p.savedByMe = was;
      appLogError(e, s, 'FEED');
      setError(ErrorHandler.handle(e).message);
    }
    notifyListeners();
  }

  Future<void> vote(PostDto p, bool value) async {
    try {
      final r = await _postRepo.vote(p.id, value);
      p.trueVotes = r.trueVotes;
      p.falseVotes = r.falseVotes;
      p.myVote = r.myVote;
      notifyListeners();
    } catch (e, s) {
      appLogError(e, s, 'FEED');
      setError(ErrorHandler.handle(e).message);
    }
  }

  Future<String?> share(PostDto p) async {
    try {
      final url = await _postRepo.share(p.id);
      p.shareCount += 1;
      notifyListeners();
      return url;
    } catch (e, s) {
      appLogError(e, s, 'FEED');
      setError(ErrorHandler.handle(e).message);
      return null;
    }
  }

  Future<bool> report(int postId, String reason, String? otherText) async {
    try {
      await _postRepo.report(postId, reason, otherText);
      return true;
    } catch (e, s) {
      appLogError(e, s, 'FEED');
      setError(ErrorHandler.handle(e).message);
      return false;
    }
  }
}
