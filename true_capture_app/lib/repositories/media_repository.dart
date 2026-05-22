import 'dart:io';

import '../core/constants/api_endpoints.dart';
import '../network/dto/post_models.dart';
import '../services/api_service.dart';

/// Drives the signed-URL media pipeline: reserve a slot → PUT the bytes →
/// finalize. Only image and video files are accepted (GIF / audio are rejected
/// by the backend).
class MediaRepository {
  MediaRepository(this._api);

  final ApiService _api;

  /// Uploads a single file end-to-end and returns the ready [MediaAssetDto].
  Future<MediaAssetDto> uploadFile(File file) async {
    final length = await file.length();
    final mime   = mimeOf(file.path);
    final kind   = mime.startsWith('video/') ? 'video' : 'photo';

    final ticketRes = await _api.post<Map<String, dynamic>>(
      ApiEndpoints.mediaUploads,
      data: {'mimeType': mime, 'byteSize': length, 'kind': kind},
    );
    final ticket = UploadTicket.fromJson(ticketRes.data!);

    await _api.putBytes(ApiEndpoints.mediaBlob(ticket.uploadId), await file.readAsBytes());

    final finRes = await _api.post<Map<String, dynamic>>(
      ApiEndpoints.mediaFinalize,
      data: {'uploadId': ticket.uploadId, 'captureMetadata': null},
    );
    return MediaAssetDto.fromJson(finRes.data!);
  }

  /// Uploads raw bytes (e.g. an in-memory compressed image) end-to-end.
  Future<MediaAssetDto> uploadBytes(
    List<int> bytes, {
    required String mimeType,
    required String kind,
  }) async {
    final ticketRes = await _api.post<Map<String, dynamic>>(
      ApiEndpoints.mediaUploads,
      data: {'mimeType': mimeType, 'byteSize': bytes.length, 'kind': kind},
    );
    final ticket = UploadTicket.fromJson(ticketRes.data!);
    await _api.putBytes(ApiEndpoints.mediaBlob(ticket.uploadId), bytes);
    final finRes = await _api.post<Map<String, dynamic>>(
      ApiEndpoints.mediaFinalize,
      data: {'uploadId': ticket.uploadId, 'captureMetadata': null},
    );
    return MediaAssetDto.fromJson(finRes.data!);
  }

  /// Uploads several files sequentially, preserving order.
  Future<List<MediaAssetDto>> uploadAll(List<File> files) async {
    final out = <MediaAssetDto>[];
    for (final f in files) {
      out.add(await uploadFile(f));
    }
    return out;
  }

  static String mimeOf(String path) {
    switch (path.toLowerCase().split('.').last) {
      case 'png':
        return 'image/png';
      case 'webp':
        return 'image/webp';
      case 'mp4':
        return 'video/mp4';
      case 'mov':
        return 'video/quicktime';
      default:
        return 'image/jpeg';
    }
  }

  static bool isVideoPath(String path) {
    final ext = path.toLowerCase().split('.').last;
    return ext == 'mp4' || ext == 'mov';
  }
}
