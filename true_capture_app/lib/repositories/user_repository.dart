import 'dart:io';

import 'package:dio/dio.dart';

import '../core/constants/api_endpoints.dart';
import '../network/dto/request/user/update_profile_request.dart';
import '../network/dto/response/auth/user_response.dart';
import '../services/api_service.dart';

/// Profile read/update + avatar management for the signed-in user.
/// Talks to the backend `UsersController` (`/api/users/me`).
class UserRepository {
  UserRepository(this._apiService);

  final ApiService _apiService;

  /// `GET /api/users/me` — the current user's full profile.
  Future<UserResponse> getMyProfile() async {
    final response =
        await _apiService.get<Map<String, dynamic>>(ApiEndpoints.userProfile);
    return UserResponse.fromJson(response.data!);
  }

  /// `PUT /api/users/me` — update display name + bio.
  Future<UserResponse> updateProfile(UpdateProfileRequest request) async {
    final response = await _apiService.put<Map<String, dynamic>>(
      ApiEndpoints.updateProfile,
      data: request.toJson(),
    );
    return UserResponse.fromJson(response.data!);
  }

  /// `POST /api/users/me/avatar` — upload a new avatar image (multipart).
  Future<UserResponse> uploadAvatar(File image) async {
    final mime = _imageMimeType(image.path).split('/');
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(
        image.path,
        filename: image.path.split('/').last,
        contentType: DioMediaType(mime[0], mime[1]),
      ),
    });
    final response = await _apiService.postMultipart<Map<String, dynamic>>(
      ApiEndpoints.uploadAvatar,
      formData,
    );
    return UserResponse.fromJson(response.data!);
  }

  /// `DELETE /api/users/me/avatar` — clear the current avatar.
  Future<UserResponse> removeAvatar() async {
    final response =
        await _apiService.delete<Map<String, dynamic>>(ApiEndpoints.uploadAvatar);
    return UserResponse.fromJson(response.data!);
  }

  /// Maps a file extension to an image MIME type the backend accepts
  /// (JPEG/PNG/WebP). Defaults to JPEG — image_picker re-encodes to JPEG.
  static String _imageMimeType(String path) {
    switch (path.toLowerCase().split('.').last) {
      case 'png':
        return 'image/png';
      case 'webp':
        return 'image/webp';
      default:
        return 'image/jpeg';
    }
  }
}
