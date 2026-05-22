import 'dart:io';

import 'package:dio/dio.dart';

import '../core/constants/api_endpoints.dart';
import '../network/dto/activity_models.dart';
import '../services/api_service.dart';

/// Ephemeral 24-hour stories.
class StoryRepository {
  StoryRepository(this._api);

  final ApiService _api;

  /// `GET /api/stories` — active stories of the viewer + people they follow.
  Future<List<UserStories>> feed() async {
    final r = await _api.get<Map<String, dynamic>>(ApiEndpoints.stories);
    final items = (r.data?['items'] as List?) ?? const [];
    return items.map((e) => UserStories.fromJson(e as Map<String, dynamic>)).toList();
  }

  /// `POST /api/stories` — post an image story.
  Future<StoryItem> create(File image, String? caption) async {
    final ext = image.path.toLowerCase().split('.').last;
    final subtype = ext == 'png' ? 'png' : (ext == 'webp' ? 'webp' : 'jpeg');
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(
        image.path,
        filename: image.path.split('/').last,
        contentType: DioMediaType('image', subtype),
      ),
      if (caption != null && caption.trim().isNotEmpty) 'caption': caption.trim(),
    });
    final r = await _api.postMultipart<Map<String, dynamic>>(ApiEndpoints.stories, formData);
    return StoryItem.fromJson(r.data!);
  }

  Future<void> delete(int storyId) => _api.delete(ApiEndpoints.storyById(storyId));
}
