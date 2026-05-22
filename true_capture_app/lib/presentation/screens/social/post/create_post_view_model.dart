import 'dart:io';

import '../../../../network/dto/post_models.dart';
import '../../../../repositories/media_repository.dart';
import '../../../../repositories/post_repository.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

/// Composes a Normal or Fake-vs-Real post: collects media + caption (+ reference
/// links for Fake vs Real), uploads the media through the signed-URL pipeline,
/// then creates the post.
class CreatePostViewModel extends BaseViewModel {
  CreatePostViewModel(this._mediaRepo, this._postRepo);

  final MediaRepository _mediaRepo;
  final PostRepository _postRepo;

  final List<File> media = [];
  String postType = PostType.normal;

  void setType(String type) {
    if (postType == type) return;
    postType = type;
    clearError();
    notifyListeners();
  }

  void addMedia(File file) {
    media.add(file);
    clearError();
    notifyListeners();
  }

  void removeMedia(int index) {
    if (index < 0 || index >= media.length) return;
    media.removeAt(index);
    notifyListeners();
  }

  bool get isFakeVsReal => postType == PostType.fakeVsReal;

  /// Uploads media + creates the post. Returns the created post or null.
  Future<PostDto?> submit({
    required String caption,
    required List<String> references,
  }) async {
    final trimmedCaption = caption.trim();
    final refs = references.map((r) => r.trim()).where((r) => r.isNotEmpty).toList();

    if (media.isEmpty) {
      setError('Add at least one photo or video.');
      return null;
    }
    if (isFakeVsReal && trimmedCaption.isEmpty) {
      setError('A caption is required for Fake vs Real posts.');
      return null;
    }
    if (isFakeVsReal && refs.isEmpty) {
      setError('Add at least one reference link.');
      return null;
    }

    return executeWithLoading<PostDto>(
      errorState: ScreenState.content,
      operation: () async {
        final assets = await _mediaRepo.uploadAll(media);
        final post = await _postRepo.create(
          type: postType,
          mediaAssetIds: assets.map((a) => a.id).toList(),
          caption: trimmedCaption.isEmpty ? null : trimmedCaption,
          references: isFakeVsReal ? refs : null,
        );
        media.clear();
        return post;
      },
    );
  }
}
