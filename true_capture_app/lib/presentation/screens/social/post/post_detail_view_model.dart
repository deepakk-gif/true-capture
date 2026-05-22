import '../../../../log/app_logs.dart';
import '../../../../network/dto/post_models.dart';
import '../../../../network/helper/error_handler.dart';
import '../../../../repositories/post_repository.dart';
import '../../base/base_view_model.dart';

/// Backs the post-detail screen — loads one post and applies engagement edits.
class PostDetailViewModel extends BaseViewModel {
  PostDetailViewModel(this._postRepo);

  final PostRepository _postRepo;

  PostDto? post;
  bool _busy = false;

  Future<void> load(int postId) async {
    await executeWithLoading(
      operation: () async {
        post = await _postRepo.detail(postId);
      },
    );
  }

  Future<void> toggleLike() async {
    final p = post;
    if (p == null || _busy) return;
    _busy = true;
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
      appLogError(e, s, 'POST_DETAIL');
      setError(ErrorHandler.handle(e).message);
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  Future<void> toggleSave() async {
    final p = post;
    if (p == null) return;
    final was = p.savedByMe;
    p.savedByMe = !was;
    notifyListeners();
    try {
      p.savedByMe = await _postRepo.toggleSave(p.id);
    } catch (e, s) {
      p.savedByMe = was;
      appLogError(e, s, 'POST_DETAIL');
      setError(ErrorHandler.handle(e).message);
    }
    notifyListeners();
  }

  Future<void> vote(bool value) async {
    final p = post;
    if (p == null) return;
    try {
      final r = await _postRepo.vote(p.id, value);
      p.trueVotes = r.trueVotes;
      p.falseVotes = r.falseVotes;
      p.myVote = r.myVote;
      notifyListeners();
    } catch (e, s) {
      appLogError(e, s, 'POST_DETAIL');
      setError(ErrorHandler.handle(e).message);
    }
  }

  Future<String?> share() async {
    final p = post;
    if (p == null) return null;
    try {
      final url = await _postRepo.share(p.id);
      p.shareCount += 1;
      notifyListeners();
      return url;
    } catch (e, s) {
      appLogError(e, s, 'POST_DETAIL');
      setError(ErrorHandler.handle(e).message);
      return null;
    }
  }

  Future<bool> report(String reason, String? otherText) async {
    final p = post;
    if (p == null) return false;
    try {
      await _postRepo.report(p.id, reason, otherText);
      return true;
    } catch (e, s) {
      appLogError(e, s, 'POST_DETAIL');
      setError(ErrorHandler.handle(e).message);
      return false;
    }
  }
}
