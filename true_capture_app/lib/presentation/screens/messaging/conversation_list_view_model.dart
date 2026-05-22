import 'dart:async';

import '../../../log/app_logs.dart';
import '../../../network/dto/message_models.dart';
import '../../../network/helper/error_handler.dart';
import '../../../repositories/message_repository.dart';
import '../../../services/chat_socket_service.dart';
import '../base/base_view_model.dart';
import '../base/screen_state.dart';

/// Backs the conversation-list screen — loads conversations, keeps them live via
/// the chat socket, and applies pin / unread changes.
class ConversationListViewModel extends BaseViewModel {
  ConversationListViewModel(this._repo, this._socket, this.currentUserId) {
    _messageSub = _socket.onMessage.listen(_onSocketMessage);
    _readSub = _socket.onRead.listen(_onSocketRead);
  }

  final MessageRepository _repo;
  final ChatSocketService _socket;
  final int currentUserId;

  late final StreamSubscription<MessageDto> _messageSub;
  late final StreamSubscription<ReadEvent> _readSub;

  List<ConversationDto> conversations = [];

  /// Pinned conversations first, then by most-recent activity.
  List<ConversationDto> get sorted {
    final list = [...conversations];
    list.sort((a, b) {
      if (a.isPinned != b.isPinned) return a.isPinned ? -1 : 1;
      final at = a.lastMessageAtUtc ?? DateTime(0);
      final bt = b.lastMessageAtUtc ?? DateTime(0);
      return bt.compareTo(at);
    });
    return list;
  }

  Future<void> load() async {
    await executeWithLoading(
      initialState: ScreenState.progress,
      operation: () async {
        final res = await _repo.conversations();
        conversations = res.items;
      },
    );
    if (!hasError) {
      changeScreenState(
          conversations.isEmpty ? ScreenState.empty : ScreenState.content);
    }
  }

  Future<void> refresh() async {
    try {
      final res = await _repo.conversations();
      conversations = res.items;
      changeScreenState(
          conversations.isEmpty ? ScreenState.empty : ScreenState.content);
      notifyListeners();
    } catch (e, s) {
      appLogError(e, s, 'CONVERSATIONS');
      setError(ErrorHandler.handle(e).message);
    }
  }

  Future<void> togglePin(ConversationDto conv) async {
    final wasPinned = conv.isPinned;
    if (!wasPinned && conversations.where((c) => c.isPinned).length >= 3) {
      setError('You can pin at most 3 chats.');
      return;
    }
    conv.isPinned = !wasPinned;
    notifyListeners();
    try {
      await _repo.pin(conv.id, conv.isPinned);
    } catch (e, s) {
      conv.isPinned = wasPinned;
      appLogError(e, s, 'CONVERSATIONS');
      setError(ErrorHandler.handle(e).message);
      notifyListeners();
    }
  }

  /// Clears the unread badge once the user opens that conversation.
  void markConversationRead(int conversationId) {
    final conv = _find(conversationId);
    if (conv != null && conv.unreadCount != 0) {
      conv.unreadCount = 0;
      notifyListeners();
    }
  }

  void _onSocketMessage(MessageDto m) {
    final conv = _find(m.conversationId);
    if (conv == null) {
      // A brand-new conversation — refetch the list lazily.
      refresh();
      return;
    }
    conv.lastMessagePreview = m.text ?? (m.isVideo ? '🎞 Video' : '📷 Photo');
    conv.lastMessageAtUtc = m.createdAtUtc ?? DateTime.now().toUtc();
    conv.lastMessageSenderId = m.senderId;
    if (m.senderId != currentUserId) conv.unreadCount += 1;
    if (screenState.value == ScreenState.empty) {
      changeScreenState(ScreenState.content);
    }
    notifyListeners();
  }

  void _onSocketRead(ReadEvent e) {
    final conv = _find(e.conversationId);
    if (conv != null && e.readerUserId == currentUserId && conv.unreadCount != 0) {
      conv.unreadCount = 0;
      notifyListeners();
    }
  }

  ConversationDto? _find(int id) {
    for (final c in conversations) {
      if (c.id == id) return c;
    }
    return null;
  }

  @override
  void dispose() {
    _messageSub.cancel();
    _readSub.cancel();
    super.dispose();
  }
}
