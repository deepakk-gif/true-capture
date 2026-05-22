import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../constants/api_endpoints.dart';
import '../../network/dto/activity_models.dart';
import '../../network/dto/message_models.dart';
import '../../presentation/screens/messaging/chat_screen.dart';
import '../../presentation/screens/auth/forgot_password/forgot_password_screen.dart';
import '../../presentation/screens/auth/otp/otp_verify_screen.dart';
import '../../presentation/screens/auth/reset_password/reset_password_screen.dart';
import '../../presentation/screens/auth/sign_in/sign_in_screen.dart';
import '../../presentation/screens/auth/sign_up/sign_up_screen.dart';
import '../../presentation/screens/intro/intro_screen.dart';
import '../../presentation/screens/main/main_screen.dart';
import '../../presentation/screens/profile/edit_profile/edit_profile_screen.dart';
import '../../presentation/screens/social/follow/follow_list_screen.dart';
import '../../presentation/screens/social/follow/follow_requests_screen.dart';
import '../../presentation/screens/social/notifications/notification_feed_screen.dart';
import '../../presentation/screens/social/post/comments_screen.dart';
import '../../presentation/screens/social/post/post_detail_screen.dart';
import '../../presentation/screens/social/profile/user_profile_screen.dart';
import '../../presentation/screens/social/search/user_search_screen.dart';
import '../../presentation/screens/social/story/create_story_screen.dart';
import '../../presentation/screens/social/story/story_viewer_screen.dart';
import '../../presentation/screens/splash/splash_screen.dart';

enum AnimationType { slideRight, slideLeft, slideUp, fade, scale, rotate, none }

class ScreenPath {
  ScreenPath._();

  static const String routeSplash = '/splash';
  static const String routeIntro = '/intro';
  static const String routeSignIn = '/sign-in';
  static const String routeSignUp = '/sign-up';
  static const String routeForgotPassword = '/forgot-password';
  static const String routeOtpVerify = '/otp-verify';
  static const String routeResetPassword = '/reset-password';
  static const String routeMain = '/main';
  static const String routeEditProfile = '/edit-profile';
  static const String routeUserSearch = '/user-search';
  static const String routeUserProfile = '/user-profile';
  static const String routeFollowList = '/follow-list';
  static const String routeFollowRequests = '/follow-requests';
  static const String routeNotifications = '/notifications';
  static const String routePostDetail = '/post-detail';
  static const String routeComments = '/comments';
  static const String routeChat = '/chat';
  static const String routeStoryViewer = '/story-viewer';
  static const String routeCreateStory = '/create-story';
}

class AppRouter {
  AppRouter._();

  static final GoRouter router = GoRouter(
    initialLocation: ScreenPath.routeSplash,
    routes: [
      GoRoute(
        path: ScreenPath.routeSplash,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const SplashScreen(),
          animationType: AnimationType.fade,
        ),
      ),
      GoRoute(
        path: ScreenPath.routeIntro,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const IntroScreen(),
          animationType: AnimationType.fade,
        ),
      ),
      GoRoute(
        path: ScreenPath.routeSignIn,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const SignInScreen(),
          animationType: AnimationType.slideRight,
        ),
      ),
      GoRoute(
        path: ScreenPath.routeSignUp,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const SignUpScreen(),
          animationType: AnimationType.slideRight,
        ),
      ),
      GoRoute(
        path: ScreenPath.routeForgotPassword,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const ForgotPasswordScreen(),
          animationType: AnimationType.slideRight,
        ),
      ),
      GoRoute(
        path: ScreenPath.routeOtpVerify,
        pageBuilder: (context, state) {
          final args = state.extra as Map<String, dynamic>? ?? const {};
          return animatedPage(
            key: state.pageKey,
            child: OtpVerifyScreen(
              email:   args['email']   as String? ?? '',
              purpose: args['purpose'] as OtpPurpose? ?? OtpPurpose.verifyEmail,
            ),
            animationType: AnimationType.slideRight,
          );
        },
      ),
      GoRoute(
        path: ScreenPath.routeResetPassword,
        pageBuilder: (context, state) {
          final args = state.extra as Map<String, dynamic>? ?? const {};
          return animatedPage(
            key: state.pageKey,
            child: ResetPasswordScreen(
              email: args['email'] as String? ?? '',
              code:  args['code']  as String? ?? '',
            ),
            animationType: AnimationType.slideRight,
          );
        },
      ),
      GoRoute(
        path: ScreenPath.routeMain,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const MainScreen(),
          animationType: AnimationType.fade,
        ),
      ),
      GoRoute(
        path: ScreenPath.routeEditProfile,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const EditProfileScreen(),
          animationType: AnimationType.slideRight,
        ),
      ),
      GoRoute(
        path: ScreenPath.routeUserSearch,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const UserSearchScreen(),
          animationType: AnimationType.slideUp,
        ),
      ),
      GoRoute(
        path: ScreenPath.routeUserProfile,
        pageBuilder: (context, state) {
          final args = state.extra as Map<String, dynamic>? ?? const {};
          return animatedPage(
            key: state.pageKey,
            child: UserProfileScreen(userId: args['userId'] as int? ?? 0),
            animationType: AnimationType.slideRight,
          );
        },
      ),
      GoRoute(
        path: ScreenPath.routeFollowList,
        pageBuilder: (context, state) {
          final args = state.extra as Map<String, dynamic>? ?? const {};
          return animatedPage(
            key: state.pageKey,
            child: FollowListScreen(
              userId: args['userId'] as int? ?? 0,
              type: args['type'] as String? ?? 'followers',
            ),
            animationType: AnimationType.slideRight,
          );
        },
      ),
      GoRoute(
        path: ScreenPath.routeFollowRequests,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const FollowRequestsScreen(),
          animationType: AnimationType.slideRight,
        ),
      ),
      GoRoute(
        path: ScreenPath.routeNotifications,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const NotificationFeedScreen(),
          animationType: AnimationType.slideRight,
        ),
      ),
      GoRoute(
        path: ScreenPath.routePostDetail,
        pageBuilder: (context, state) {
          final args = state.extra as Map<String, dynamic>? ?? const {};
          return animatedPage(
            key: state.pageKey,
            child: PostDetailScreen(postId: args['postId'] as int? ?? 0),
            animationType: AnimationType.slideRight,
          );
        },
      ),
      GoRoute(
        path: ScreenPath.routeComments,
        pageBuilder: (context, state) {
          final args = state.extra as Map<String, dynamic>? ?? const {};
          return animatedPage(
            key: state.pageKey,
            child: CommentsScreen(postId: args['postId'] as int? ?? 0),
            animationType: AnimationType.slideUp,
          );
        },
      ),
      GoRoute(
        path: ScreenPath.routeChat,
        pageBuilder: (context, state) {
          final args = state.extra as Map<String, dynamic>? ?? const {};
          return animatedPage(
            key: state.pageKey,
            child: ChatScreen(
              conversation:   args['conversation']   as ConversationDto?,
              conversationId: args['conversationId'] as int?,
              userId:         args['userId']         as int?,
              title:          args['title']          as String?,
            ),
            animationType: AnimationType.slideRight,
          );
        },
      ),
      GoRoute(
        path: ScreenPath.routeStoryViewer,
        pageBuilder: (context, state) {
          final args = state.extra as Map<String, dynamic>? ?? const {};
          return animatedPage(
            key: state.pageKey,
            child: StoryViewerScreen(
              userStories: args['userStories'] as UserStories,
            ),
            animationType: AnimationType.fade,
          );
        },
      ),
      GoRoute(
        path: ScreenPath.routeCreateStory,
        pageBuilder: (context, state) => animatedPage(
          key: state.pageKey,
          child: const CreateStoryScreen(),
          animationType: AnimationType.slideUp,
        ),
      ),
    ],
  );

  static CustomTransitionPage<T> animatedPage<T>({
    required LocalKey key,
    required Widget child,
    required AnimationType animationType,
    Duration duration = const Duration(milliseconds: 300),
    Curve curve = Curves.easeInOut,
  }) {
    return CustomTransitionPage<T>(
      key: key,
      child: child,
      transitionDuration: duration,
      transitionsBuilder: (context, animation, secondaryAnimation, child) {
        final curved = CurvedAnimation(parent: animation, curve: curve);
        switch (animationType) {
          case AnimationType.slideRight:
            return SlideTransition(
              position: Tween<Offset>(
                begin: const Offset(1, 0),
                end: Offset.zero,
              ).animate(curved),
              child: child,
            );
          case AnimationType.slideLeft:
            return SlideTransition(
              position: Tween<Offset>(
                begin: const Offset(-1, 0),
                end: Offset.zero,
              ).animate(curved),
              child: child,
            );
          case AnimationType.slideUp:
            return SlideTransition(
              position: Tween<Offset>(
                begin: const Offset(0, 1),
                end: Offset.zero,
              ).animate(curved),
              child: child,
            );
          case AnimationType.fade:
            return FadeTransition(opacity: curved, child: child);
          case AnimationType.scale:
            return ScaleTransition(scale: curved, child: child);
          case AnimationType.rotate:
            return RotationTransition(turns: curved, child: child);
          case AnimationType.none:
            return child;
        }
      },
    );
  }

  static void go(BuildContext context, String location, {Object? extra}) =>
      context.go(location, extra: extra);

  static Future<T?> push<T>(BuildContext context, String location,
          {Object? extra}) =>
      context.push<T>(location, extra: extra);

  static void pop<T>(BuildContext context, [T? argument]) =>
      context.pop<T>(argument);

  static void replace(BuildContext context, String location, {Object? extra}) =>
      context.pushReplacement(location, extra: extra);
}
