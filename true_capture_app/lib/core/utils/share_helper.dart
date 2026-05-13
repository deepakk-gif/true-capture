import 'package:share_plus/share_plus.dart';

class ShareHelper {
  ShareHelper._();

  static Future<void> shareText(String text, {String? subject}) {
    return Share.share(text, subject: subject);
  }

  static Future<void> shareFiles(List<String> paths, {String? text}) {
    return Share.shareXFiles(
      paths.map((p) => XFile(p)).toList(),
      text: text,
    );
  }
}
