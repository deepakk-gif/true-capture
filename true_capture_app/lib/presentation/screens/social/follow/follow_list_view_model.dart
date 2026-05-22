import '../../../../network/dto/social_models.dart';
import '../../../../repositories/social_repository.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

/// Backs the followers / following list screen.
class FollowListViewModel extends BaseViewModel {
  FollowListViewModel(this._socialRepo);

  final SocialRepository _socialRepo;

  List<FollowUserItem> items = [];

  Future<void> load(int userId, String type) async {
    await executeWithLoading(
      operation: () async {
        final result = type == 'following'
            ? await _socialRepo.following(userId)
            : await _socialRepo.followers(userId);
        items = result.items;
        if (items.isEmpty) changeScreenState(ScreenState.empty);
      },
    );
  }
}
