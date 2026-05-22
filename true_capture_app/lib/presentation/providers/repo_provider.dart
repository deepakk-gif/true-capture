import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../repositories/auth_repository.dart';
import '../../repositories/common_repository.dart';
import '../../repositories/feed_repository.dart';
import '../../repositories/media_repository.dart';
import '../../repositories/message_repository.dart';
import '../../repositories/notification_repository.dart';
import '../../repositories/post_repository.dart';
import '../../repositories/social_repository.dart';
import '../../repositories/story_repository.dart';
import '../../repositories/user_repository.dart';
import '../../services/api_service.dart';
import '../../services/chat_socket_service.dart';
import '../../services/recent_search_service.dart';
import 'local_storage_provider.dart';

final apiServiceProvider = Provider<ApiService>((ref) {
  return ApiService.instance;
});

final authRepo = Provider<AuthRepository>((ref) {
  return AuthRepository(ref.read(apiServiceProvider));
});

final commonRepo = Provider<CommonRepository>((ref) {
  return CommonRepository(ref.read(apiServiceProvider));
});

final userRepo = Provider<UserRepository>((ref) {
  return UserRepository(ref.read(apiServiceProvider));
});

final socialRepo = Provider<SocialRepository>((ref) {
  return SocialRepository(ref.read(apiServiceProvider));
});

final postRepo = Provider<PostRepository>((ref) {
  return PostRepository(ref.read(apiServiceProvider));
});

final mediaRepo = Provider<MediaRepository>((ref) {
  return MediaRepository(ref.read(apiServiceProvider));
});

final feedRepo = Provider<FeedRepository>((ref) {
  return FeedRepository(ref.read(apiServiceProvider));
});

final messageRepo = Provider<MessageRepository>((ref) {
  return MessageRepository(ref.read(apiServiceProvider));
});

/// The process-wide SignalR chat connection.
final chatSocketProvider = Provider<ChatSocketService>((ref) {
  return ChatSocketService.instance;
});

final notificationRepo = Provider<NotificationRepository>((ref) {
  return NotificationRepository(ref.read(apiServiceProvider));
});

final storyRepo = Provider<StoryRepository>((ref) {
  return StoryRepository(ref.read(apiServiceProvider));
});

final recentSearchServiceProvider = Provider<RecentSearchService>((ref) {
  return RecentSearchService(ref.read(localStorageServiceProvider));
});
