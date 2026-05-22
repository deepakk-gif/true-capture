// DTOs for the Messaging module — conversations, messages, reactions.
// Wire-compatible with the backend `Modules.Messaging` records (camelCase JSON).

int _i(Object? v) => v is int ? v : (v is num ? v.toInt() : int.tryParse('$v') ?? 0);
DateTime? _d(Object? v) => v == null ? null : DateTime.tryParse(v.toString());

/// Message content kinds.
class MessageType {
  MessageType._();
  static const String text  = 'text';
  static const String image = 'image';
  static const String video = 'video';
}

/// The other participant of a 1-to-1 conversation.
class ChatUserDto {
  const ChatUserDto({
    required this.id,
    required this.username,
    this.displayName,
    this.avatarUrl,
    this.isBlueTick = false,
  });

  final int id;
  final String username;
  final String? displayName;
  final String? avatarUrl;
  final bool isBlueTick;

  String get name => displayName ?? username;

  factory ChatUserDto.fromJson(Map<String, dynamic> j) => ChatUserDto(
        id: _i(j['id']),
        username: j['username']?.toString() ?? '',
        displayName: j['displayName']?.toString(),
        avatarUrl: j['avatarUrl']?.toString(),
        isBlueTick: j['isBlueTick'] == true,
      );
}

/// A row in the conversation list.
class ConversationDto {
  ConversationDto({
    required this.id,
    required this.otherUser,
    this.lastMessagePreview,
    this.lastMessageAtUtc,
    this.lastMessageSenderId,
    this.isPinned = false,
    this.unreadCount = 0,
  });

  final int id;
  final ChatUserDto otherUser;
  String? lastMessagePreview;
  DateTime? lastMessageAtUtc;
  int? lastMessageSenderId;
  bool isPinned;
  int unreadCount;

  bool get hasUnread => unreadCount > 0;

  factory ConversationDto.fromJson(Map<String, dynamic> j) => ConversationDto(
        id: _i(j['id']),
        otherUser: ChatUserDto.fromJson(
            (j['otherUser'] as Map?)?.cast<String, dynamic>() ?? const {}),
        lastMessagePreview: j['lastMessagePreview']?.toString(),
        lastMessageAtUtc: _d(j['lastMessageAtUtc']),
        lastMessageSenderId:
            j['lastMessageSenderId'] == null ? null : _i(j['lastMessageSenderId']),
        isPinned: j['isPinned'] == true,
        unreadCount: _i(j['unreadCount']),
      );
}

class ConversationListResult {
  const ConversationListResult({this.items = const [], this.nextCursor});
  final List<ConversationDto> items;
  final String? nextCursor;

  factory ConversationListResult.fromJson(Map<String, dynamic> j) =>
      ConversationListResult(
        items: (j['items'] as List?)
                ?.map((e) => ConversationDto.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
        nextCursor: j['nextCursor']?.toString(),
      );
}

/// The quoted message shown above a reply.
class MessageReplyDto {
  const MessageReplyDto({
    required this.id,
    required this.senderId,
    required this.senderName,
    required this.type,
    required this.preview,
  });

  final int id;
  final int senderId;
  final String senderName;
  final String type;
  final String preview;

  factory MessageReplyDto.fromJson(Map<String, dynamic> j) => MessageReplyDto(
        id: _i(j['id']),
        senderId: _i(j['senderId']),
        senderName: j['senderName']?.toString() ?? '',
        type: j['type']?.toString() ?? 'text',
        preview: j['preview']?.toString() ?? '',
      );
}

/// An aggregated emoji-reaction bucket on a message.
class ReactionDto {
  const ReactionDto({required this.emoji, required this.count, this.mine = false});

  final String emoji;
  final int count;
  final bool mine;

  factory ReactionDto.fromJson(Map<String, dynamic> j) => ReactionDto(
        emoji: j['emoji']?.toString() ?? '',
        count: _i(j['count']),
        mine: j['mine'] == true,
      );
}

/// One chat message.
class MessageDto {
  MessageDto({
    required this.id,
    required this.conversationId,
    required this.senderId,
    required this.type,
    this.text,
    this.mediaUrl,
    this.thumbnailUrl,
    this.mediaWidth,
    this.mediaHeight,
    this.replyTo,
    this.reactions = const [],
    this.createdAtUtc,
  });

  final int id;
  final int conversationId;
  final int senderId;
  final String type;
  final String? text;
  final String? mediaUrl;
  final String? thumbnailUrl;
  final int? mediaWidth;
  final int? mediaHeight;
  final MessageReplyDto? replyTo;
  List<ReactionDto> reactions;
  final DateTime? createdAtUtc;

  bool get isImage => type == MessageType.image;
  bool get isVideo => type == MessageType.video;

  factory MessageDto.fromJson(Map<String, dynamic> j) => MessageDto(
        id: _i(j['id']),
        conversationId: _i(j['conversationId']),
        senderId: _i(j['senderId']),
        type: j['type']?.toString() ?? 'text',
        text: j['text']?.toString(),
        mediaUrl: j['mediaUrl']?.toString(),
        thumbnailUrl: j['thumbnailUrl']?.toString(),
        mediaWidth: j['mediaWidth'] == null ? null : _i(j['mediaWidth']),
        mediaHeight: j['mediaHeight'] == null ? null : _i(j['mediaHeight']),
        replyTo: j['replyTo'] == null
            ? null
            : MessageReplyDto.fromJson((j['replyTo'] as Map).cast<String, dynamic>()),
        reactions: (j['reactions'] as List?)
                ?.map((e) => ReactionDto.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
        createdAtUtc: _d(j['createdAtUtc']),
      );
}

class MessageListResult {
  const MessageListResult({this.items = const [], this.nextCursor});
  final List<MessageDto> items;
  final String? nextCursor;

  factory MessageListResult.fromJson(Map<String, dynamic> j) => MessageListResult(
        items: (j['items'] as List?)
                ?.map((e) => MessageDto.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
        nextCursor: j['nextCursor']?.toString(),
      );
}
