import 'package:flutter/animation.dart';

/// Shared motion tokens — the single source of truth for animation timing and
/// curves across the app. Referenced by the `flutter-widget-animation` skill;
/// do not hard-code `Duration(...)` or `Curves.*` in widgets, use these.
class AppMotion {
  AppMotion._();

  static const Duration fast = Duration(milliseconds: 150); // taps / feedback
  static const Duration normal = Duration(milliseconds: 250); // entrances
  static const Duration slow = Duration(milliseconds: 400); // large/hero elements
  static const Duration stagger = Duration(milliseconds: 70); // gap between siblings

  static const Curve enter = Curves.easeOutCubic; // elements appearing
  static const Curve exit = Curves.easeInCubic; // elements leaving
  static const Curve standard = Curves.easeInOut; // state changes
}
