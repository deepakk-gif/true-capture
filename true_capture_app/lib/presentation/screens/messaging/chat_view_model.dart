import 'dart:async';
import 'dart:io';

import 'package:flutter_image_compress/flutter_image_compress.dart';
import 'package:video_compress/video_compress.dart';

import '../../../log/app_logs.dart';
import '../../../network/dto/message_models.dart';
import '../../../network/helper/error_handler.dart';
import '../../../repositories/media_repository.dart';
import '../../../repositories/message_repository.dart';
import '../../../services/chat_socket_service.dart';
import '../base/base_view_model.dart';
import '../base/screen_state.dart';

/// Backs the chat screen — loads message history (newest-first), sends text /
/// image / video, applies reactions and replies, and stays live via the socket.
class ChatViewModel extends BaseViewModel {
  ChatViewModel(this._repo, this._mediaRepo, this._socket, this.currentUserId);

  final MessageRepository _repo;
  final MediaRepository _mediaRepo;
  final ChatSocketService _socket;
  final int currentUserId;

  static const int _maxMediaBytes = 10 * 1024 * 1024; // 10 MB

  int? conversationId;
  ChatUserDto? otherUser;
  String headerTitle = 'Chat';

  /// Newest-first — the chat list renders reversed so index 0 sits at the bottom.
  List<MessageDto> messages = [];
  String? _cursor;
  bool _loadingMore = false;
  bool sending = false;
  MessageDto? replyingTo;

  StreamSubscription<MessageDto>? _messageSub;
  StreamSubscription<ReactionEvent>? _reactionSub;

  bool get hasMore => _cursor != null;
  bool get loadingMore => _loadingMore;

  /// Resolves the conversation (creating it for a `userId` entry point) and loads.
  Future<void> open({
    ConversationDto? conversation,
    int? conversationId,
    int? userId,
    String? title,
  }) async {
    await executeWithLoading(
      initialState: ScreenState.progress,
      operation: () async {
        if (conversation != null) {
          this.conversationId = conversation.id;
          otherUser = conversation.otherUser;
          headerTitle = conversation.otherUser.name;
        } else if (userId != null) {
          final conv = await _repo.getOrCreateConversation(userId);
          this.conversationId = conv.id;
          otherUser = conv.otherUser;
          headerTitle = conv.otherUser.name;
        } else {
          this.conversationId = conversationId;
          headerTitle = title ?? 'Chat';
        }

        final res = await _repo.messages(this.conversationId!);
        messages = res.items;
        _cursor = res.nextCursor;
      },
    );

    if (!hasError) {
      changeScreenState(ScreenState.content);
      _socket.activeConversationId = conversationId;
      _messageSub = _socket.onMessage.listen(_onSocketMessage);
      _reactionSub = _socket.onReaction.listen(_onSocketReaction);
      _markRead();
    }
  }

  Future<void> loadOlder() async {
    if (_loadingMore || _cursor == null || conversationId == null) return;
    _loadingMore = true;
    notifyListeners();
    try {
      final res = await _repo.messages(conversationId!, cursor: _cursor);
      messages = [...messages, ...res.items];
      _cursor = res.nextCursor;
    } catch (e, s) {
      appLogError(e, s, 'CHAT');
      setError(ErrorHandler.handle(e).message);
    } finally {
      _loadingMore = false;
      notifyListeners();
    }
  }

  void setReply(MessageDto? message) {
    replyingTo = message;
    notifyListeners();
  }

  Future<bool> sendText(String text) async {
    final trimmed = text.trim();
    if (trimmed.isEmpty || conversationId == null) return false;
    return _send(() => _repo.sendMessage(
          conversationId!,
          type: MessageType.text,
          text: trimmed,
          replyToMessageId: replyingTo?.id,
        ));
  }

  Future<bool> sendImage(File file) async {
    if (conversationId == null) return false;
    return _send(() async {
      final bytes = await FlutterImageCompress.compressWithFile(
        file.absolute.path,
        quality: 70,
      );
      final data = bytes ?? await file.readAsBytes();
      if (data.length > _maxMediaBytes) {
        throw const _MediaTooLarge();
      }
      final asset = await _mediaRepo.uploadBytes(
        data, mimeType: 'image/jpeg', kind: 'photo');
      return _repo.sendMessage(
        conversationId!,
        type: MessageType.image,
        mediaUrl: asset.url,
        thumbnailUrl: asset.thumbnailUrl ?? asset.url,
        replyToMessageId: replyingTo?.id,
      );
    });
  }

  Future<bool> sendVideo(File file) async {
    if (conversationId == null) return false;
    return _send(() async {
      final info = await VideoCompress.compressVideo(
        file.absolute.path,
        quality: VideoQuality.MediumQuality,
      );
      final compressed = info?.file ?? file;
      if (await compressed.length() > _maxMediaBytes) {
        throw const _MediaTooLarge();
      }
      final videoAsset = await _mediaRepo.uploadFile(compressed);

      // A separate thumbnail upload so the message can render a preview frame.
      String? thumbUrl;
      try {
        final thumb = await VideoCompress.getFileThumbnail(file.absolute.path);
        thumbUrl = (await _mediaRepo.uploadFile(thumb)).url;
      } catch (e, s) {
        appLogError(e, s, 'CHAT');
      }

      return _repo.sendMessage(
        conversationId!,
        type: MessageType.video,
        mediaUrl: videoAsset.url,
        thumbnailUrl: thumbUrl,
        replyToMessageId: replyingTo?.id,
      );
    });
  }

  Future<bool> _send(Future<MessageDto> Function() op) async {
    if (sending) return false;
    sending = true;
    notifyListeners();
    try {
      final message = await op();
      if (!messages.any((m) => m.id == message.id)) {
        messages = [message, ...messages];
      }
      replyingTo = null;
      return true;
    } on _MediaTooLarge {
      setError('That file is over the 10 MB limit, even after compression.');
      return false;
    } catch (e, s) {
      appLogError(e, s, 'CHAT');
      setError(ErrorHandler.handle(e).message);
      return false;
    } finally {
      sending = false;
      notifyListeners();
    }
  }

  /// Sets / replaces / clears (null emoji) the caller's reaction on a message.
  Future<void> react(MessageDto message, String? emoji) async {
    try {
      final mine = message.reactions.where((r) => r.mine).toList();
      final clearing = emoji != null && mine.any((r) => r.emoji == emoji);
      final updated = await _repo.react(message.id, clearing ? null : emoji);
      _applyReactions(message.id, updated);
    } catch (e, s) {
      appLogError(e, s, 'CHAT');
      setError(ErrorHandler.handle(e).message);
    }
  }

  void _onSocketMessage(MessageDto m) {
    if (m.conversationId != conversationId) return;
    if (messages.any((x) => x.id == m.id)) return;
    messages = [m, ...messages];
    if (m.senderId != currentUserId) _markRead();
    notifyListeners();
  }

  void _onSocketReaction(ReactionEvent e) {
    if (e.conversationId != conversationId) return;
    _applyReactions(e.messageId, e.reactions);
  }

  void _applyReactions(int messageId, List<ReactionDto> reactions) {
    final idx = messages.indexWhere((m) => m.id == messageId);
    if (idx >= 0) {
      messages[idx].reactions = reactions;
      notifyListeners();
    }
  }

  void _markRead() {
    final convId = conversationId;
    if (convId == null || messages.isEmpty) return;
    // messages[0] is the newest.
    _repo.markRead(convId, messages.first.id).catchError((Object e, StackTrace s) {
      appLogError(e, s, 'CHAT');
    });
  }

  @override
  void dispose() {
    if (_socket.activeConversationId == conversationId) {
      _socket.activeConversationId = null;
    }
    _messageSub?.cancel();
    _reactionSub?.cancel();
    super.dispose();
  }
}

/// Thrown internally when compressed media still exceeds the 10 MB cap.
class _MediaTooLarge implements Exception {
  const _MediaTooLarge();
}
