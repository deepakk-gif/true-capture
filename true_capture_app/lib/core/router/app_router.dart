import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../presentation/screens/auth/forgot_password/forgot_password_screen.dart';
import '../../presentation/screens/auth/otp/otp_verify_screen.dart';
import '../../presentation/screens/auth/sign_in/sign_in_screen.dart';
import '../../presentation/screens/auth/sign_up/sign_up_screen.dart';
import '../../presentation/screens/intro/intro_screen.dart';
import '../../presentation/screens/main/main_screen.dart';
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
  static const String routeMain = '/main';
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
              email: args['email'] as String? ?? '',
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
