import 'dart:async';

import 'package:signalr_netcore/signalr_client.dart';

import '../config/app_config.dart';
import '../core/constants/api_endpoints.dart';
import '../log/app_logs.dart';
import '../network/dto/message_models.dart';

/// A `MessageRead` realtime event.
typedef ReadEvent = ({int conversationId, int readerUserId, int lastReadMessageId});

/// A `ReactionUpdated` realtime event.
typedef ReactionEvent = ({int conversationId, int messageId, List<ReactionDto> reactions});

/// Wraps the SignalR `/hubs/chat` connection. Connect once after login; the
/// service exposes the server-push events as broadcast streams the messaging
/// screens listen to. Realtime is additive — REST remains the source of truth.
class ChatSocketService {
  ChatSocketService._();
  static final ChatSocketService instance = ChatSocketService._();

  HubConnection? _connection;

  /// The conversation the user is currently viewing — set by the chat screen so
  /// the global message listener can suppress notifications for the open chat.
  int? activeConversationId;

  final _messageController  = StreamController<MessageDto>.broadcast();
  final _readController     = StreamController<ReadEvent>.broadcast();
  final _reactionController = StreamController<ReactionEvent>.broadcast();

  Stream<MessageDto>   get onMessage  => _messageController.stream;
  Stream<ReadEvent>    get onRead     => _readController.stream;
  Stream<ReactionEvent> get onReaction => _reactionController.stream;

  bool get isConnected => _connection?.state == HubConnectionState.Connected;

  /// Opens the hub connection authenticated by [tokenProvider]. Safe to call
  /// repeatedly — a live connection is reused.
  Future<void> connect(Future<String?> Function() tokenProvider) async {
    if (_connection != null) return;

    final url = '${AppConfig.baseUrl.replaceAll(RegExp(r"/+$"), "")}'
        '${ApiEndpoints.chatHub}';
    final connection = HubConnectionBuilder()
        .withUrl(
          url,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => (await tokenProvider()) ?? '',
          ),
        )
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveMessage', _onReceiveMessage);
    connection.on('MessageRead', _onMessageRead);
    connection.on('ReactionUpdated', _onReactionUpdated);

    _connection = connection;
    try {
      await connection.start();
      appLog('Chat socket connected', tag: 'SOCKET');
    } catch (e, s) {
      appLogError(e, s, 'SOCKET');
      _connection = null;
    }
  }

  Future<void> disconnect() async {
    try {
      await _connection?.stop();
    } catch (_) {/* ignore */}
    _connection = null;
  }

  void _onReceiveMessage(List<Object?>? args) {
    final raw = args?.isNotEmpty == true ? args!.first : null;
    if (raw is Map) {
      _messageController.add(MessageDto.fromJson(raw.cast<String, dynamic>()));
    }
  }

  void _onMessageRead(List<Object?>? args) {
    if (args == null || args.length < 3) return;
    _readController.add((
      conversationId: _int(args[0]),
      readerUserId: _int(args[1]),
      lastReadMessageId: _int(args[2]),
    ));
  }

  void _onReactionUpdated(List<Object?>? args) {
    if (args == null || args.length < 3) return;
    final list = (args[2] as List?)
            ?.map((e) => ReactionDto.fromJson((e as Map).cast<String, dynamic>()))
            .toList() ??
        const <ReactionDto>[];
    _reactionController.add((
      conversationId: _int(args[0]),
      messageId: _int(args[1]),
      reactions: list,
    ));
  }

  static int _int(Object? v) =>
      v is int ? v : (v is num ? v.toInt() : int.tryParse('$v') ?? 0);
}
