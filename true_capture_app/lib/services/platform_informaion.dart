import 'dart:io';

import 'package:flutter/foundation.dart';

class AppPlatformInfo {
  AppPlatformInfo._();
  static final AppPlatformInfo instance = AppPlatformInfo._();

  bool get isAndroid => !kIsWeb && Platform.isAndroid;
  bool get isIOS => !kIsWeb && Platform.isIOS;
  bool get isWeb => kIsWeb;
  bool get isMacOS => !kIsWeb && Platform.isMacOS;
  bool get isWindows => !kIsWeb && Platform.isWindows;
  bool get isLinux => !kIsWeb && Platform.isLinux;
  bool get isMobile => isAndroid || isIOS;
  bool get isDesktop => isMacOS || isWindows || isLinux;

  String get platformName {
    if (isWeb) return 'web';
    if (isAndroid) return 'android';
    if (isIOS) return 'ios';
    if (isMacOS) return 'macos';
    if (isWindows) return 'windows';
    if (isLinux) return 'linux';
    return 'unknown';
  }
}
