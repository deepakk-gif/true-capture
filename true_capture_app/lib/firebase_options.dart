// Placeholder for FlutterFire-generated configuration.
// Run `flutterfire configure` to overwrite this file with real values.
import 'package:firebase_core/firebase_core.dart' show FirebaseOptions;
import 'package:flutter/foundation.dart' show defaultTargetPlatform, TargetPlatform;

class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform {
    switch (defaultTargetPlatform) {
      case TargetPlatform.android:
        return _placeholder('android');
      case TargetPlatform.iOS:
        return _placeholder('ios');
      case TargetPlatform.macOS:
        return _placeholder('macos');
      default:
        return _placeholder('web');
    }
  }

  static FirebaseOptions _placeholder(String platform) => FirebaseOptions(
        apiKey: 'REPLACE_ME_$platform',
        appId: 'REPLACE_ME_$platform',
        messagingSenderId: 'REPLACE_ME',
        projectId: 'REPLACE_ME',
      );
}
