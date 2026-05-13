import 'package:flutter/foundation.dart';

void appLog(Object? message, {String tag = 'APP'}) {
  if (kDebugMode) {
    debugPrint('[$tag] $message');
  }
}

void appLogError(Object? error, [StackTrace? stack, String tag = 'ERROR']) {
  if (kDebugMode) {
    debugPrint('[$tag] $error');
    if (stack != null) debugPrint('[$tag] $stack');
  }
}
