import 'package:flutter/material.dart';

import '../../../config/app_config.dart';
import '../../../network/dto/message_models.dart';
import 'chat_time.dart';

const _quickEmojis = ['👍', '❤️', '😂', '😮', '😢', '🙏'];

/// One chat message — text / image / video bubble with reply quote, reactions
/// and time. Double-tap reacts with ❤️; long-press opens the emoji bar + Reply.
class MessageBubble extends StatelessWidget {
  const MessageBubble({
    super.key,
    required this.message,
    required this.isMine,
    required this.onReact,
    required this.onReply,
  });

  final MessageDto message;
  final bool isMine;
  final void Function(String? emoji) onReact;
  final VoidCallback onReply;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final bubbleColor = isMine
        ? theme.colorScheme.primary
        : theme.colorScheme.surfaceContainerHighest;
    final textColor =
        isMine ? theme.colorScheme.onPrimary : theme.colorScheme.onSurface;

    return Align(
      alignment: isMine ? Alignment.centerRight : Alignment.centerLeft,
      child: ConstrainedBox(
        constraints: BoxConstraints(
            maxWidth: MediaQuery.of(context).size.width * 0.78),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
          child: Column(
            crossAxisAlignment:
                isMine ? CrossAxisAlignment.end : CrossAxisAlignment.start,
            children: [
              GestureDetector(
                onDoubleTap: () => onReact('❤️'),
                onLongPress: () => _openActions(context),
                child: Container(
                  decoration: BoxDecoration(
                    color: bubbleColor,
                    borderRadius: BorderRadius.circular(16),
                  ),
                  padding: const EdgeInsets.all(8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (message.replyTo != null) _replyQuote(theme, textColor),
                      _content(theme, textColor),
                    ],
                  ),
                ),
              ),
              if (message.reactions.isNotEmpty) _reactionChips(theme),
              Padding(
                padding: const EdgeInsets.only(top: 2, left: 4, right: 4),
                child: Text(
                  chatTimeRelative(message.createdAtUtc),
                  style: theme.textTheme.bodySmall
                      ?.copyWith(color: theme.hintColor, fontSize: 11),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _replyQuote(ThemeData theme, Color textColor) {
    final r = message.replyTo!;
    return Container(
      margin: const EdgeInsets.only(bottom: 4),
      padding: const EdgeInsets.fromLTRB(8, 4, 8, 4),
      decoration: BoxDecoration(
        color: textColor.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(8),
        border: Border(left: BorderSide(color: textColor, width: 3)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(r.senderName,
              style: TextStyle(
                  color: textColor, fontWeight: FontWeight.w600, fontSize: 12)),
          Text(r.preview,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(color: textColor, fontSize: 12)),
        ],
      ),
    );
  }

  Widget _content(ThemeData theme, Color textColor) {
    if (message.isImage) {
      return _media(AppConfig.resolveUrl(message.thumbnailUrl ?? message.mediaUrl),
          isVideo: false);
    }
    if (message.isVideo) {
      return _media(AppConfig.resolveUrl(message.thumbnailUrl), isVideo: true);
    }
    return Text(message.text ?? '', style: TextStyle(color: textColor));
  }

  Widget _media(String? url, {required bool isVideo}) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(10),
      child: Stack(
        alignment: Alignment.center,
        children: [
          SizedBox(
            width: 220,
            height: 220,
            child: url != null
                ? Image.network(url, fit: BoxFit.cover,
                    errorBuilder: (_, _, _) =>
                        const ColoredBox(color: Colors.black12))
                : const ColoredBox(color: Colors.black12),
          ),
          if (isVideo)
            const Icon(Icons.play_circle_fill, size: 52, color: Colors.white70),
        ],
      ),
    );
  }

  Widget _reactionChips(ThemeData theme) {
    return Padding(
      padding: const EdgeInsets.only(top: 2),
      child: Wrap(
        spacing: 4,
        children: message.reactions
            .map((r) => Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                  decoration: BoxDecoration(
                    color: r.mine
                        ? theme.colorScheme.primaryContainer
                        : theme.colorScheme.surfaceContainerHighest,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text('${r.emoji} ${r.count}',
                      style: const TextStyle(fontSize: 12)),
                ))
            .toList(),
      ),
    );
  }

  void _openActions(BuildContext context) {
    showModalBottomSheet<void>(
      context: context,
      builder: (sheet) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 12),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                children: _quickEmojis
                    .map((e) => GestureDetector(
                          onTap: () {
                            Navigator.pop(sheet);
                            onReact(e);
                          },
                          child: Text(e, style: const TextStyle(fontSize: 28)),
                        ))
                    .toList(),
              ),
            ),
            const Divider(height: 1),
            ListTile(
              leading: const Icon(Icons.reply),
              title: const Text('Reply'),
              onTap: () {
                Navigator.pop(sheet);
                onReply();
              },
            ),
          ],
        ),
      ),
    );
  }
}
