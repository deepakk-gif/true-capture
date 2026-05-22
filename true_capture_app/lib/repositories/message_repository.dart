import '../core/constants/api_endpoints.dart';
import '../network/dto/message_models.dart';
import '../services/api_service.dart';

/// REST surface of the messaging module. Realtime delivery is handled separately
/// by `ChatSocketService`; media is uploaded via `MediaRepository`.
class MessageRepository {
  MessageRepository(this._api);

  final ApiService _api;

  /// `GET /api/conversations` — the caller's conversation list.
  Future<ConversationListResult> conversations({String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.conversations,
      queryParameters: {'cursor': ?cursor},
    );
    return ConversationListResult.fromJson(r.data!);
  }

  /// `POST /api/conversations` — get-or-create the direct chat with a user.
  Future<ConversationDto> getOrCreateConversation(int userId) async {
    final r = await _api.post<Map<String, dynamic>>(
      ApiEndpoints.conversations,
      data: {'userId': userId},
    );
    return ConversationDto.fromJson(r.data!);
  }

  /// `GET /api/conversations/{id}/messages` — newest-first message page.
  Future<MessageListResult> messages(int conversationId, {String? cursor}) async {
    final r = await _api.get<Map<String, dynamic>>(
      ApiEndpoints.conversationMessages(conversationId),
      queryParameters: {'cursor': ?cursor},
    );
    return MessageListResult.fromJson(r.data!);
  }

  /// `POST /api/conversations/{id}/messages` — send a text / image / video message.
  Future<MessageDto> sendMessage(
    int conversationId, {
    required String type,
    String? text,
    String? mediaUrl,
    String? thumbnailUrl,
    int? mediaWidth,
    int? mediaHeight,
    int? replyToMessageId,
  }) async {
    final r = await _api.post<Map<String, dynamic>>(
      ApiEndpoints.conversationMessages(conversationId),
      data: {
        'type': type,
        'text': text,
        'mediaUrl': mediaUrl,
        'thumbnailUrl': thumbnailUrl,
        'mediaWidth': mediaWidth,
        'mediaHeight': mediaHeight,
        'replyToMessageId': replyToMessageId,
      },
    );
    return MessageDto.fromJson(r.data!);
  }

  /// `POST /api/conversations/{id}/read` — mark read up to a message id.
  Future<void> markRead(int conversationId, int lastMessageId) => _api.post(
        ApiEndpoints.conversationRead(conversationId),
        data: {'lastMessageId': lastMessageId},
      );

  /// `POST /api/conversations/{id}/pin` — pin / unpin (max 3).
  Future<void> pin(int conversationId, bool pinned) => _api.post(
        ApiEndpoints.conversationPin(conversationId),
        data: {'pinned': pinned},
      );

  /// `POST /api/messages/{id}/react` — set / replace / clear (null emoji) a reaction.
  Future<List<ReactionDto>> react(int messageId, String? emoji) async {
    final r = await _api.post<List<dynamic>>(
      ApiEndpoints.messageReact(messageId),
      data: {'emoji': emoji},
    );
    return (r.data ?? const [])
        .map((e) => ReactionDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<void> deleteMessage(int messageId) =>
      _api.delete(ApiEndpoints.messageById(messageId));
}
