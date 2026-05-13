import 'package:flutter/material.dart';

import '../constants/app_constants.dart';

extension ContextExtensions on BuildContext {
  ThemeData get theme => Theme.of(this);
  TextTheme get textTheme => Theme.of(this).textTheme;
  ColorScheme get colors => Theme.of(this).colorScheme;
  MediaQueryData get mq => MediaQuery.of(this);
  Size get screenSize => MediaQuery.sizeOf(this);
  double get screenWidth => MediaQuery.sizeOf(this).width;
  double get screenHeight => MediaQuery.sizeOf(this).height;

  bool get isMobile => screenWidth < AppConstants.mobileBreakpoint;
  bool get isTablet =>
      screenWidth >= AppConstants.mobileBreakpoint &&
      screenWidth < AppConstants.desktopBreakpoint;
  bool get isDesktop => screenWidth >= AppConstants.desktopBreakpoint;
  bool get isLandscape => mq.orientation == Orientation.landscape;
  bool get isPortrait => mq.orientation == Orientation.portrait;

  void unfocus() => FocusScope.of(this).unfocus();
}
