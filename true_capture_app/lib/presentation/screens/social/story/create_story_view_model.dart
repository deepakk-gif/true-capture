import 'dart:io';

import '../../../../repositories/story_repository.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

class CreateStoryViewModel extends BaseViewModel {
  CreateStoryViewModel(this._storyRepo);

  final StoryRepository _storyRepo;

  File? image;

  void setImage(File file) {
    image = file;
    clearError();
    notifyListeners();
  }

  /// Uploads the picked image as a story. Returns true on success.
  Future<bool> submit(String caption) async {
    if (image == null) {
      setError('Choose an image for your story.');
      return false;
    }
    final ok = await executeWithLoading<bool>(
      errorState: ScreenState.content,
      operation: () async {
        await _storyRepo.create(image!, caption);
        image = null;
        return true;
      },
    );
    notifyListeners();
    return ok ?? false;
  }
}
