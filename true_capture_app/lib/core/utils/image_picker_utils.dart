import 'dart:io';

import 'package:image_picker/image_picker.dart';

class ImagePickerUtils {
  ImagePickerUtils._();

  static final ImagePicker _picker = ImagePicker();

  static Future<File?> pickFromGallery({int imageQuality = 80}) async {
    final XFile? file = await _picker.pickImage(
      source: ImageSource.gallery,
      imageQuality: imageQuality,
    );
    return file == null ? null : File(file.path);
  }

  static Future<File?> pickFromCamera({int imageQuality = 80}) async {
    final XFile? file = await _picker.pickImage(
      source: ImageSource.camera,
      imageQuality: imageQuality,
    );
    return file == null ? null : File(file.path);
  }

  static Future<List<File>> pickMultiple({int imageQuality = 80}) async {
    final List<XFile> files =
        await _picker.pickMultiImage(imageQuality: imageQuality);
    return files.map((f) => File(f.path)).toList();
  }
}
