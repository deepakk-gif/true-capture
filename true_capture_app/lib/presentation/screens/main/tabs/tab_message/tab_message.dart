import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../core/router/app_router.dart';
import '../../../../../network/dto/message_models.dart';
import '../../../../common_widgets/custom_app_bar.dart';
import '../../../../common_widgets/user_avatar.dart';
import '../../../../providers/vm_provider.dart';
import '../../../base/base_consumer_state.dart';
import '../../../base/screen_state_aware.dart';
import '../../../messaging/chat_time.dart';
import '../../../messaging/conversation_list_view_model.dart';

/// Tab 4 — Messages: the 1-to-1 conversation list.
class TabMessage extends ConsumerStatefulWidget {
  const TabMessage({super.key});

  @override
  ConsumerState<TabMessage> createState() => _TabMessageState();
}

class _TabMessageState
    extends BaseConsumerState<TabMessage, ConversationListViewModel> {
  @override
  void onModelReady(ConversationListViewModel model) => model.load();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: const CustomAppBar(title: 'Messages'),
      floatingActionButton: FloatingActionButton(
        onPressed: () => AppRouter.push(context, ScreenPath.routeUserSearch),
        tooltip: 'New message',
        child: const Icon(Icons.edit_outlined),
      ),
      body: ListenableBuilder(
        listenable: viewModel,
        builder: (context, _) => ScreenStateAware(
          state: viewModel.screenState,
          empty: const Center(child: Text('No conversations yet.')),
          builder: (context) => RefreshIndicator(
            onRefresh: viewModel.refresh,
            child: ListView.separated(
              physics: const AlwaysScrollableScrollPhysics(),
              itemCount: viewModel.sorted.length,
              separatorBuilder: (_, _) => const Divider(height: 1, indent: 76),
              itemBuilder: (context, i) => _tile(viewModel.sorted[i]),
            ),
          ),
        ),
      ),
    );
  }

  Widget _tile(ConversationDto conv) {
    final theme = Theme.of(context);
    final unread = conv.hasUnread;
    final previewStyle = theme.textTheme.bodyMedium?.copyWith(
      color: unread ? theme.colorScheme.onSurface : theme.hintColor,
      fontWeight: unread ? FontWeight.bold : FontWeight.normal,
    );

    return ListTile(
      onTap: () {
        viewModel.markConversationRead(conv.id);
        AppRouter.push(context, ScreenPath.routeChat, extra: {'conversation': conv});
      },
      onLongPress: () => _pinSheet(conv),
      leading: UserAvatar(
        avatarUrl: conv.otherUser.avatarUrl,
        name: conv.otherUser.name,
        radius: 26,
      ),
      title: Row(
        children: [
          if (conv.isPinned)
            const Padding(
              padding: EdgeInsets.only(right: 4),
              child: Icon(Icons.push_pin, size: 14),
            ),
          Expanded(
            child: Text(
              conv.otherUser.name,
              overflow: TextOverflow.ellipsis,
              style: theme.textTheme.titleSmall?.copyWith(
                fontWeight: unread ? FontWeight.bold : FontWeight.w600,
              ),
            ),
          ),
          if (conv.otherUser.isBlueTick)
            const Icon(Icons.verified, size: 14, color: Colors.blue),
        ],
      ),
      subtitle: Text(
        conv.lastMessagePreview ?? 'Tap to start chatting',
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: previewStyle,
      ),
      trailing: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Text(chatTimeShort(conv.lastMessageAtUtc),
              style: theme.textTheme.bodySmall?.copyWith(color: theme.hintColor)),
          const SizedBox(height: 4),
          if (unread)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
              decoration: BoxDecoration(
                color: theme.colorScheme.primary,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text('${conv.unreadCount}',
                  style: TextStyle(
                      color: theme.colorScheme.onPrimary, fontSize: 11)),
            ),
        ],
      ),
    );
  }

  void _pinSheet(ConversationDto conv) {
    showModalBottomSheet<void>(
      context: context,
      builder: (sheet) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading:
                  Icon(conv.isPinned ? Icons.push_pin_outlined : Icons.push_pin),
              title: Text(conv.isPinned ? 'Unpin chat' : 'Pin chat'),
              onTap: () {
                Navigator.pop(sheet);
                viewModel.togglePin(conv);
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  ConversationListViewModel createViewModel() =>
      ref.read(conversationListViewModelProvider);

  @override
  String screenName() => 'CONVERSATION LIST';
}
