import '../../../../log/app_logs.dart';
import '../../../../network/dto/social_models.dart';
import '../../../../network/helper/error_handler.dart';
import '../../../../repositories/social_repository.dart';
import '../../base/base_view_model.dart';

class UserProfileViewModel extends BaseViewModel {
  UserProfileViewModel(this._socialRepo);

  final SocialRepository _socialRepo;

  UserProfileView? profile;
  List<PostItem> posts = [];
  bool busyFollow = false;

  Future<void> load(int userId) async {
    await executeWithLoading(
      operation: () async {
        profile = await _socialRepo.profile(userId);
        posts = profile!.canViewContent
            ? (await _socialRepo.userPosts(userId)).items
            : [];
      },
    );
  }

  /// Follow / unfollow / cancel-request — toggles based on the current state,
  /// then refreshes the profile so counts + content visibility update.
  Future<void> toggleFollow() async {
    final p = profile;
    if (p == null || p.isMe || busyFollow) return;

    busyFollow = true;
    notifyListeners();
    try {
      if (p.followState == FollowState.none) {
        await _socialRepo.follow(p.id);
      } else {
        await _socialRepo.unfollow(p.id);
      }
      profile = await _socialRepo.profile(p.id);
      posts = profile!.canViewContent
          ? (await _socialRepo.userPosts(p.id)).items
          : [];
    } catch (e, s) {
      appLogError(e, s, 'USER_PROFILE');
      setError(ErrorHandler.handle(e).message);
    } finally {
      busyFollow = false;
      notifyListeners();
    }
  }
}
