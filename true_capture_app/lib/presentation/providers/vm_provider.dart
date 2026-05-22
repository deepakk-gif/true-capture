import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../screens/auth/forgot_password/forgot_password_view_model.dart';
import '../screens/auth/otp/otp_view_model.dart';
import '../screens/auth/reset_password/reset_password_view_model.dart';
import '../screens/auth/sign_in/sign_in_viewmodel.dart';
import '../screens/auth/sign_up/sign_up_view_model.dart';
import '../screens/main/main_view_model.dart';
import '../screens/main/tabs/feed_view_model.dart';
import '../screens/messaging/chat_view_model.dart';
import '../screens/messaging/conversation_list_view_model.dart';
import '../screens/profile/edit_profile/edit_profile_view_model.dart';
import '../screens/social/follow/follow_list_view_model.dart';
import '../screens/social/follow/follow_requests_view_model.dart';
import '../screens/social/notifications/notification_feed_view_model.dart';
import '../screens/social/post/comments_view_model.dart';
import '../screens/social/post/create_post_view_model.dart';
import '../screens/social/post/post_detail_view_model.dart';
import '../screens/social/profile/user_profile_view_model.dart';
import '../screens/social/search/user_search_view_model.dart';
import '../screens/social/story/create_story_view_model.dart';
import '../screens/splash/splash_viewmodel.dart';
import 'local_storage_provider.dart';
import 'repo_provider.dart';
import 'user_data_provider.dart';

final splashVm = Provider.autoDispose<SplashViewmodel>((ref) {
  return SplashViewmodel(
    ref.read(authRepo),
    ref.read(localStorageServiceProvider),
    ref.read(authStateNotifierProvider.notifier),
  );
});

final signInViewModelProvider =
    Provider.autoDispose<SignInViewModel>((ref) {
  return SignInViewModel(
    ref.read(authRepo),
    ref.read(localStorageServiceProvider),
    ref.read(authStateNotifierProvider.notifier),
  );
});

final signUpViewModelProvider =
    Provider.autoDispose<SignUpViewModel>((ref) {
  return SignUpViewModel(
    ref.read(authRepo),
    ref.read(localStorageServiceProvider),
    ref.read(authStateNotifierProvider.notifier),
  );
});

final forgotPasswordViewModelProvider =
    Provider.autoDispose<ForgotPasswordViewModel>((ref) {
  return ForgotPasswordViewModel(ref.read(authRepo));
});

final otpViewModelProvider = Provider.autoDispose<OtpViewModel>((ref) {
  return OtpViewModel(
    ref.read(authRepo),
    ref.read(authStateNotifierProvider.notifier),
    ref.read(localStorageServiceProvider),
  );
});

final resetPasswordViewModelProvider =
    Provider.autoDispose<ResetPasswordViewModel>((ref) {
  return ResetPasswordViewModel(ref.read(authRepo));
});

final mainVm = Provider.autoDispose<MainViewModel>((ref) {
  return MainViewModel();
});

final editProfileViewModelProvider =
    Provider.autoDispose<EditProfileViewModel>((ref) {
  return EditProfileViewModel(
    ref.read(userRepo),
    ref.read(authStateNotifierProvider.notifier),
  );
});

// ---- Social feature view-models ----------------------------------------

final userSearchViewModelProvider =
    Provider.autoDispose<UserSearchViewModel>((ref) {
  return UserSearchViewModel(
    ref.read(socialRepo),
    ref.read(recentSearchServiceProvider),
  );
});

final userProfileViewModelProvider =
    Provider.autoDispose<UserProfileViewModel>((ref) {
  return UserProfileViewModel(ref.read(socialRepo));
});

final followListViewModelProvider =
    Provider.autoDispose<FollowListViewModel>((ref) {
  return FollowListViewModel(ref.read(socialRepo));
});

final followRequestsViewModelProvider =
    Provider.autoDispose<FollowRequestsViewModel>((ref) {
  return FollowRequestsViewModel(ref.read(socialRepo));
});

final createPostViewModelProvider =
    Provider.autoDispose<CreatePostViewModel>((ref) {
  return CreatePostViewModel(ref.read(mediaRepo), ref.read(postRepo));
});

/// One feed view-model per channel ("home" / "fake_vs_real").
final feedViewModelProvider =
    Provider.autoDispose.family<FeedViewModel, String>((ref, channel) {
  return FeedViewModel(ref.read(feedRepo), ref.read(postRepo), channel);
});

final notificationFeedViewModelProvider =
    Provider.autoDispose<NotificationFeedViewModel>((ref) {
  return NotificationFeedViewModel(ref.read(notificationRepo));
});

final postDetailViewModelProvider =
    Provider.autoDispose<PostDetailViewModel>((ref) {
  return PostDetailViewModel(ref.read(postRepo));
});

final commentsViewModelProvider =
    Provider.autoDispose<CommentsViewModel>((ref) {
  return CommentsViewModel(ref.read(postRepo));
});

final createStoryViewModelProvider =
    Provider.autoDispose<CreateStoryViewModel>((ref) {
  return CreateStoryViewModel(ref.read(storyRepo));
});

// ---- Messaging view-models ---------------------------------------------

int _currentUserId(Ref ref) =>
    int.tryParse(ref.read(authStateNotifierProvider)?.id ?? '') ?? 0;

final conversationListViewModelProvider =
    Provider.autoDispose<ConversationListViewModel>((ref) {
  return ConversationListViewModel(
    ref.read(messageRepo),
    ref.read(chatSocketProvider),
    _currentUserId(ref),
  );
});

final chatViewModelProvider = Provider.autoDispose<ChatViewModel>((ref) {
  return ChatViewModel(
    ref.read(messageRepo),
    ref.read(mediaRepo),
    ref.read(chatSocketProvider),
    _currentUserId(ref),
  );
});
