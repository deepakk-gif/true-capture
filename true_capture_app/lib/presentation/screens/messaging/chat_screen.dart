import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../network/dto/message_models.dart';
import '../../common_widgets/user_avatar.dart';
import '../../providers/vm_provider.dart';
import '../base/base_consumer_state.dart';
import '../base/screen_state_aware.dart';
import 'chat_view_model.dart';
import 'message_bubble.dart';

/// 1-to-1 chat. Opened with one of: a `conversation` (from the list), a `userId`
/// (from a profile), or a `conversationId` (+ optional `title`, from a notification).
class ChatScreen extends ConsumerStatefulWidget {
  const ChatScreen({
    super.key,
    this.conversation,
    this.conversationId,
    this.userId,
    this.title,
  });

  final ConversationDto? conversation;
  final int? conversationId;
  final int? userId;
  final String? title;

  @override
  ConsumerState<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends BaseConsumerState<ChatScreen, ChatViewModel> {
  final _controller = TextEditingController();
  final _scrollController = ScrollController();
  final _picker = ImagePicker();

  @override
  void onModelReady(ChatViewModel model) {
    _scrollController.addListener(_onScroll);
    model.open(
      conversation: widget.conversation,
      conversationId: widget.conversationId,
      userId: widget.userId,
      title: widget.title,
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    // Reversed list — the top (older messages) is at maxScrollExtent.
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 300) {
      viewModel.loadOlder();
    }
  }

  Future<void> _sendText() async {
    final ok = await viewModel.sendText(_controller.text);
    if (ok) _controller.clear();
  }

  void _attach() {
    showModalBottomSheet<void>(
      context: context,
      builder: (sheet) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Send a photo'),
              onTap: () async {
                Navigator.pop(sheet);
                final f = await _picker.pickImage(source: ImageSource.gallery);
                if (f != null) await viewModel.sendImage(File(f.path));
              },
            ),
            ListTile(
              leading: const Icon(Icons.videocam_outlined),
              title: const Text('Send a video'),
              onTap: () async {
                Navigator.pop(sheet);
                final f = await _picker.pickVideo(source: ImageSource.gallery);
                if (f != null) await viewModel.sendVideo(File(f.path));
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        titleSpacing: 0,
        title: ListenableBuilder(
          listenable: viewModel,
          builder: (context, _) => Row(
            children: [
              UserAvatar(
                avatarUrl: viewModel.otherUser?.avatarUrl,
                name: viewModel.headerTitle,
                radius: 16,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(viewModel.headerTitle,
                    overflow: TextOverflow.ellipsis),
              ),
            ],
          ),
        ),
      ),
      body: ListenableBuilder(
        listenable: viewModel,
        builder: (context, _) => ScreenStateAware(
          state: viewModel.screenState,
          builder: (context) => Column(
            children: [
              Expanded(child: _messageList()),
              if (viewModel.replyingTo != null) _replyBar(),
              _composer(),
            ],
          ),
        ),
      ),
    );
  }

  Widget _messageList() {
    if (viewModel.messages.isEmpty) {
      return const Center(child: Text('Say hello 👋'));
    }
    return ListView.builder(
      controller: _scrollController,
      reverse: true,
      padding: const EdgeInsets.symmetric(vertical: 8),
      itemCount: viewModel.messages.length + (viewModel.hasMore ? 1 : 0),
      itemBuilder: (context, index) {
        if (index >= viewModel.messages.length) {
          return const Padding(
            padding: EdgeInsets.all(12),
            child: Center(child: CircularProgressIndicator()),
          );
        }
        final m = viewModel.messages[index];
        return MessageBubble(
          message: m,
          isMine: m.senderId == viewModel.currentUserId,
          onReact: (emoji) => viewModel.react(m, emoji),
          onReply: () => viewModel.setReply(m),
        );
      },
    );
  }

  Widget _replyBar() {
    final r = viewModel.replyingTo!;
    return Container(
      color: Theme.of(context).colorScheme.surfaceContainerHighest,
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      child: Row(
        children: [
          const Icon(Icons.reply, size: 18),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              r.text ?? (r.isVideo ? '🎞 Video' : '📷 Photo'),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          GestureDetector(
            onTap: () => viewModel.setReply(null),
            child: const Icon(Icons.close, size: 18),
          ),
        ],
      ),
    );
  }

  Widget _composer() {
    return SafeArea(
      top: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(8, 6, 8, 6),
        child: Row(
          children: [
            IconButton(
              icon: const Icon(Icons.add_circle_outline),
              onPressed: viewModel.sending ? null : _attach,
            ),
            Expanded(
              child: TextField(
                controller: _controller,
                minLines: 1,
                maxLines: 5,
                textInputAction: TextInputAction.send,
                onSubmitted: (_) => _sendText(),
                decoration: const InputDecoration(
                  hintText: 'Message…',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
              ),
            ),
            IconButton(
              icon: viewModel.sending
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.send),
              onPressed: viewModel.sending ? null : _sendText,
            ),
          ],
        ),
      ),
    );
  }

  @override
  ChatViewModel createViewModel() => ref.read(chatViewModelProvider);

  @override
  String screenName() => 'CHAT';
}
