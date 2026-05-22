import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../network/dto/post_models.dart';
import '../../../common_widgets/user_avatar.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state_aware.dart';
import 'comments_view_model.dart';

class CommentsScreen extends ConsumerStatefulWidget {
  const CommentsScreen({super.key, required this.postId});

  final int postId;

  @override
  ConsumerState<CommentsScreen> createState() => _CommentsScreenState();
}

class _CommentsScreenState
    extends BaseConsumerState<CommentsScreen, CommentsViewModel> {
  final _controller = TextEditingController();

  @override
  void onModelReady(CommentsViewModel model) => model.load(widget.postId);

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final ok = await viewModel.add(widget.postId, _controller.text);
    if (ok) _controller.clear();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Comments')),
      body: ListenableBuilder(
        listenable: viewModel,
        builder: (context, _) => Column(
          children: [
            Expanded(
              child: ScreenStateAware(
                state: viewModel.screenState,
                empty: const Center(child: Text('No comments yet — be the first.')),
                builder: (context) => viewModel.comments.isEmpty
                    ? const Center(child: Text('No comments yet — be the first.'))
                    : ListView.builder(
                        padding: const EdgeInsets.symmetric(vertical: 8),
                        itemCount: viewModel.comments.length,
                        itemBuilder: (_, i) => _commentBlock(viewModel.comments[i]),
                      ),
              ),
            ),
            _composer(),
          ],
        ),
      ),
    );
  }

  Widget _commentBlock(CommentDto c) {
    final replies = viewModel.replies[c.id] ?? const [];
    final expanded = viewModel.expanded.contains(c.id);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _CommentTile(
          comment: c,
          onLike: () => viewModel.toggleLike(c),
          onReply: () => viewModel.startReply(c),
          onDelete: () => viewModel.delete(c.id),
        ),
        if (c.repliesCount > 0)
          Padding(
            padding: const EdgeInsets.only(left: 56, bottom: 4),
            child: GestureDetector(
              onTap: () => viewModel.toggleReplies(c.id),
              child: Text(
                expanded
                    ? 'Hide replies'
                    : 'View ${c.repliesCount} ${c.repliesCount == 1 ? "reply" : "replies"}',
                style: TextStyle(
                  color: Theme.of(context).hintColor,
                  fontWeight: FontWeight.w600,
                  fontSize: 12,
                ),
              ),
            ),
          ),
        if (expanded)
          for (final r in replies)
            Padding(
              padding: const EdgeInsets.only(left: 40),
              child: _CommentTile(
                comment: r,
                onLike: () => viewModel.toggleLike(r),
                onReply: () => viewModel.startReply(r),
                onDelete: () => viewModel.delete(r.id, parentId: c.id),
              ),
            ),
      ],
    );
  }

  Widget _composer() {
    return SafeArea(
      top: false,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (viewModel.replyingToId != null)
            Container(
              width: double.infinity,
              color: Theme.of(context).colorScheme.surfaceContainerHighest,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
              child: Row(
                children: [
                  Expanded(
                    child: Text('Replying to @${viewModel.replyingToUsername}'),
                  ),
                  GestureDetector(
                    onTap: viewModel.cancelReply,
                    child: const Icon(Icons.close, size: 18),
                  ),
                ],
              ),
            ),
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 8, 8, 8),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _controller,
                    minLines: 1,
                    maxLines: 4,
                    decoration: const InputDecoration(
                      hintText: 'Add a comment…',
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
                  onPressed: viewModel.sending ? null : _send,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  @override
  CommentsViewModel createViewModel() => ref.read(commentsViewModelProvider);

  @override
  String screenName() => 'COMMENTS';
}

class _CommentTile extends StatelessWidget {
  const _CommentTile({
    required this.comment,
    required this.onLike,
    required this.onReply,
    required this.onDelete,
  });

  final CommentDto comment;
  final VoidCallback onLike;
  final VoidCallback onReply;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 8, 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          UserAvatar(
            avatarUrl: comment.authorAvatarUrl,
            name: comment.authorDisplayName ?? comment.authorUsername,
            radius: 16,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(comment.authorUsername,
                        style: theme.textTheme.bodyMedium
                            ?.copyWith(fontWeight: FontWeight.w600)),
                    if (comment.authorIsBlueTick) ...[
                      const SizedBox(width: 4),
                      const Icon(Icons.verified, size: 13, color: Colors.blue),
                    ],
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  comment.isRemoved ? '[deleted]' : comment.text,
                  style: comment.isRemoved
                      ? theme.textTheme.bodyMedium
                          ?.copyWith(fontStyle: FontStyle.italic, color: theme.hintColor)
                      : theme.textTheme.bodyMedium,
                ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    if (!comment.isReply)
                      GestureDetector(
                        onTap: onReply,
                        child: Text('Reply',
                            style: TextStyle(
                                fontSize: 12,
                                color: theme.hintColor,
                                fontWeight: FontWeight.w600)),
                      ),
                    if (!comment.isReply) const SizedBox(width: 16),
                    GestureDetector(
                      onTap: onDelete,
                      child: Text('Delete',
                          style: TextStyle(fontSize: 12, color: theme.hintColor)),
                    ),
                  ],
                ),
              ],
            ),
          ),
          Column(
            children: [
              IconButton(
                visualDensity: VisualDensity.compact,
                icon: Icon(
                  comment.likedByMe ? Icons.favorite : Icons.favorite_border,
                  size: 18,
                  color: comment.likedByMe ? Colors.red : null,
                ),
                onPressed: onLike,
              ),
              if (comment.likeCount > 0)
                Text('${comment.likeCount}',
                    style: theme.textTheme.bodySmall),
            ],
          ),
        ],
      ),
    );
  }
}
