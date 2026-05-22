import '../../../../log/app_logs.dart';
import '../../../../network/dto/post_models.dart';
import '../../../../network/helper/error_handler.dart';
import '../../../../repositories/post_repository.dart';
import '../../base/base_view_model.dart';

/// Backs the comments screen — top-level comments plus one level of replies,
/// each likeable. A reply to a reply is not allowed (enforced by the backend).
class CommentsViewModel extends BaseViewModel {
  CommentsViewModel(this._postRepo);

  final PostRepository _postRepo;

  List<CommentDto> comments = [];
  final Map<int, List<CommentDto>> replies = {};
  final Set<int> expanded = {};

  bool sending = false;
  int? replyingToId;             // top-level comment being replied to
  String? replyingToUsername;

  Future<void> load(int postId) async {
    await executeWithLoading(
      operation: () async {
        final res = await _postRepo.comments(postId);
        comments = res.items;
      },
    );
  }

  void startReply(CommentDto comment) {
    // Replies attach to the top-level comment (1-level threading).
    replyingToId = comment.parentCommentId ?? comment.id;
    replyingToUsername = comment.authorUsername;
    notifyListeners();
  }

  void cancelReply() {
    replyingToId = null;
    replyingToUsername = null;
    notifyListeners();
  }

  Future<void> toggleReplies(int commentId) async {
    if (expanded.contains(commentId)) {
      expanded.remove(commentId);
      notifyListeners();
      return;
    }
    expanded.add(commentId);
    notifyListeners();
    if (!replies.containsKey(commentId)) {
      try {
        final res = await _postRepo.replies(commentId);
        replies[commentId] = res.items;
        notifyListeners();
      } catch (e, s) {
        appLogError(e, s, 'COMMENTS');
        setError(ErrorHandler.handle(e).message);
      }
    }
  }

  /// Posts a comment, or a reply when [replyingToId] is set.
  Future<bool> add(int postId, String text) async {
    if (text.trim().isEmpty || sending) return false;
    sending = true;
    notifyListeners();
    final parentId = replyingToId;
    try {
      final created =
          await _postRepo.addComment(postId, text.trim(), parentCommentId: parentId);
      if (parentId == null) {
        comments = [...comments, created];
      } else {
        replies[parentId] = [...?replies[parentId], created];
        expanded.add(parentId);
      }
      replyingToId = null;
      replyingToUsername = null;
      return true;
    } catch (e, s) {
      appLogError(e, s, 'COMMENTS');
      setError(ErrorHandler.handle(e).message);
      return false;
    } finally {
      sending = false;
      notifyListeners();
    }
  }

  Future<void> toggleLike(CommentDto c) async {
    final wasLiked = c.likedByMe;
    final oldCount = c.likeCount;
    c.likedByMe = !wasLiked;
    c.likeCount = oldCount + (wasLiked ? -1 : 1);
    notifyListeners();
    try {
      final r = await _postRepo.toggleCommentLike(c.id);
      c.likedByMe = r.liked;
      c.likeCount = r.count;
    } catch (e, s) {
      c.likedByMe = wasLiked;
      c.likeCount = oldCount;
      appLogError(e, s, 'COMMENTS');
      setError(ErrorHandler.handle(e).message);
    }
    notifyListeners();
  }

  Future<void> delete(int commentId, {int? parentId}) async {
    try {
      await _postRepo.deleteComment(commentId);
      if (parentId == null) {
        comments = comments.where((c) => c.id != commentId).toList();
      } else {
        replies[parentId] =
            (replies[parentId] ?? []).where((c) => c.id != commentId).toList();
      }
      notifyListeners();
    } catch (e, s) {
      appLogError(e, s, 'COMMENTS');
      setError(ErrorHandler.handle(e).message);
    }
  }
}
