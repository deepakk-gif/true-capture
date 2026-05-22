import 'package:flutter/material.dart';

import '../../config/app_config.dart';
import '../../network/dto/post_models.dart';
import '../screens/social/post/report_sheet.dart';
import 'user_avatar.dart';

/// The reusable post card — renders one [PostDto] in the feed, the detail
/// screen and the saved list. Engagement is delegated to callbacks so the card
/// stays stateless about networking.
///
/// Video playback (with a mute control) is a documented follow-up; videos
/// currently render their thumbnail with a play badge and open the detail
/// screen on tap.
class PostCard extends StatefulWidget {
  const PostCard({
    super.key,
    required this.post,
    this.onTapAuthor,
    this.onTapPost,
    this.onLike,
    this.onComment,
    this.onShare,
    this.onSave,
    this.onVote,
    this.onFollow,
    this.onReport,
  });

  final PostDto post;
  final VoidCallback? onTapAuthor;
  final VoidCallback? onTapPost;
  final VoidCallback? onLike;
  final VoidCallback? onComment;
  final VoidCallback? onShare;
  final VoidCallback? onSave;
  final void Function(bool value)? onVote;
  final Future<bool> Function()? onFollow;
  final Future<void> Function(String reason, String? otherText)? onReport;

  @override
  State<PostCard> createState() => _PostCardState();
}

class _PostCardState extends State<PostCard> {
  final _pageController = PageController();
  int _mediaIndex = 0;
  bool _captionExpanded = false;
  bool _followBusy = false;
  bool _followedLocally = false;

  PostDto get post => widget.post;

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  Future<void> _follow() async {
    if (_followBusy || widget.onFollow == null) return;
    setState(() => _followBusy = true);
    final ok = await widget.onFollow!.call();
    if (mounted) {
      setState(() {
        _followBusy = false;
        _followedLocally = ok;
      });
    }
  }

  void _openMoreSheet() {
    showModalBottomSheet<void>(
      context: context,
      builder: (sheet) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.person_outline),
              title: const Text('About this account'),
              onTap: () {
                Navigator.pop(sheet);
                widget.onTapAuthor?.call();
              },
            ),
            ListTile(
              leading: const Icon(Icons.flag_outlined),
              title: const Text('Report post'),
              onTap: () async {
                Navigator.pop(sheet);
                final sel = await showReportSheet(context);
                if (sel != null) {
                  await widget.onReport?.call(sel.reason, sel.otherText);
                  if (mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Thanks — your report was sent.')),
                    );
                  }
                }
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _header(theme),
        _media(theme),
        _actionRow(theme),
        if (post.isFakeVsReal) _voteBar(theme),
        _caption(theme),
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 2, 12, 12),
          child: Text(
            '${_count(post.viewCount)} views · ${_relativeTime(post.createdAtUtc)}',
            style: theme.textTheme.bodySmall?.copyWith(color: theme.hintColor),
          ),
        ),
        const Divider(height: 1),
      ],
    );
  }

  Widget _header(ThemeData theme) {
    final showFollow = !post.author.isFollowing && !_followedLocally;
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 10, 4, 8),
      child: Row(
        children: [
          GestureDetector(
            onTap: widget.onTapAuthor,
            child: UserAvatar(
              avatarUrl: post.author.avatarUrl,
              name: post.author.displayName ?? post.author.username,
              radius: 18,
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: GestureDetector(
              onTap: widget.onTapAuthor,
              child: Row(
                children: [
                  Flexible(
                    child: Text(
                      post.author.username,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.titleSmall
                          ?.copyWith(fontWeight: FontWeight.w600),
                    ),
                  ),
                  if (post.author.isBlueTick) ...[
                    const SizedBox(width: 4),
                    const Icon(Icons.verified, size: 15, color: Colors.blue),
                  ],
                  if (post.isAdminPost) ...[
                    const SizedBox(width: 6),
                    _chip(theme, 'Fake vs Real'),
                  ],
                ],
              ),
            ),
          ),
          if (showFollow)
            TextButton(
              onPressed: _followBusy ? null : _follow,
              child: Text(_followBusy ? '…' : 'Follow'),
            ),
          IconButton(
            icon: const Icon(Icons.more_vert),
            onPressed: _openMoreSheet,
          ),
        ],
      ),
    );
  }

  Widget _media(ThemeData theme) {
    if (post.media.isEmpty) return const SizedBox.shrink();
    return AspectRatio(
      aspectRatio: 1,
      child: Stack(
        children: [
          PageView.builder(
            controller: _pageController,
            itemCount: post.media.length,
            onPageChanged: (i) => setState(() => _mediaIndex = i),
            itemBuilder: (_, i) => _mediaItem(theme, post.media[i]),
          ),
          if (post.media.length > 1)
            Positioned(
              bottom: 8,
              left: 0,
              right: 0,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(post.media.length, (i) {
                  final active = i == _mediaIndex;
                  return Container(
                    width: active ? 7 : 6,
                    height: active ? 7 : 6,
                    margin: const EdgeInsets.symmetric(horizontal: 2),
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: active ? Colors.white : Colors.white54,
                    ),
                  );
                }),
              ),
            ),
        ],
      ),
    );
  }

  Widget _mediaItem(ThemeData theme, PostMediaDto m) {
    final url = AppConfig.resolveUrl(m.isVideo ? (m.thumbnailUrl ?? m.url) : m.url);
    return GestureDetector(
      onTap: widget.onTapPost,
      child: Container(
        color: theme.colorScheme.surfaceContainerHighest,
        width: double.infinity,
        child: Stack(
          fit: StackFit.expand,
          children: [
            if (url != null)
              Image.network(
                url,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) =>
                    const Center(child: Icon(Icons.broken_image_outlined)),
              ),
            if (m.isVideo)
              const Center(
                child: Icon(Icons.play_circle_fill, size: 56, color: Colors.white70),
              ),
          ],
        ),
      ),
    );
  }

  Widget _actionRow(ThemeData theme) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 4),
      child: Row(
        children: [
          _action(
            icon: post.likedByMe ? Icons.favorite : Icons.favorite_border,
            color: post.likedByMe ? Colors.red : null,
            label: _count(post.likeCount),
            onTap: widget.onLike,
          ),
          _action(
            icon: Icons.mode_comment_outlined,
            label: _count(post.commentCount),
            onTap: widget.onComment,
          ),
          _action(
            icon: Icons.send_outlined,
            label: _count(post.shareCount),
            onTap: widget.onShare,
          ),
          const Spacer(),
          IconButton(
            icon: Icon(post.savedByMe ? Icons.bookmark : Icons.bookmark_border),
            onPressed: widget.onSave,
          ),
        ],
      ),
    );
  }

  Widget _action({
    required IconData icon,
    required String label,
    VoidCallback? onTap,
    Color? color,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(20),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
        child: Row(
          children: [
            Icon(icon, size: 24, color: color),
            const SizedBox(width: 4),
            Text(label),
          ],
        ),
      ),
    );
  }

  Widget _voteBar(ThemeData theme) {
    final total = post.trueVotes + post.falseVotes;
    final realPct = total == 0 ? 0 : (post.trueVotes * 100 / total).round();
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 4, 12, 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Is this real or fake?',
              style: theme.textTheme.bodySmall?.copyWith(color: theme.hintColor)),
          const SizedBox(height: 6),
          Row(
            children: [
              Expanded(
                child: _voteButton(
                  label: 'Real (${post.trueVotes})',
                  selected: post.myVote == true,
                  onTap: () => widget.onVote?.call(true),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _voteButton(
                  label: 'Fake (${post.falseVotes})',
                  selected: post.myVote == false,
                  onTap: () => widget.onVote?.call(false),
                ),
              ),
            ],
          ),
          if (total > 0)
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Text('$realPct% voted real · $total votes',
                  style: theme.textTheme.bodySmall),
            ),
        ],
      ),
    );
  }

  Widget _voteButton({
    required String label,
    required bool selected,
    required VoidCallback onTap,
  }) {
    return OutlinedButton(
      onPressed: onTap,
      style: OutlinedButton.styleFrom(
        backgroundColor: selected
            ? Theme.of(context).colorScheme.primaryContainer
            : null,
      ),
      child: Text(label),
    );
  }

  Widget _caption(ThemeData theme) {
    final caption = post.caption?.trim();
    if (caption == null || caption.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 4, 12, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          RichText(
            maxLines: _captionExpanded ? null : 1,
            overflow: _captionExpanded ? TextOverflow.clip : TextOverflow.ellipsis,
            text: TextSpan(
              style: theme.textTheme.bodyMedium,
              children: [
                TextSpan(
                  text: '${post.author.username} ',
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
                TextSpan(text: caption),
              ],
            ),
          ),
          if (!_captionExpanded && caption.length > 40)
            GestureDetector(
              onTap: () => setState(() => _captionExpanded = true),
              child: Padding(
                padding: const EdgeInsets.only(top: 2),
                child: Text('show more',
                    style: TextStyle(color: theme.hintColor)),
              ),
            ),
        ],
      ),
    );
  }

  Widget _chip(ThemeData theme, String label) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
        decoration: BoxDecoration(
          color: theme.colorScheme.secondaryContainer,
          borderRadius: BorderRadius.circular(6),
        ),
        child: Text(label,
            style: theme.textTheme.labelSmall
                ?.copyWith(color: theme.colorScheme.onSecondaryContainer)),
      );

  static String _count(int n) {
    if (n >= 1000000) return '${(n / 1000000).toStringAsFixed(1)}M';
    if (n >= 1000) return '${(n / 1000).toStringAsFixed(1)}K';
    return '$n';
  }

  static String _relativeTime(DateTime? utc) {
    if (utc == null) return '';
    final diff = DateTime.now().toUtc().difference(utc);
    if (diff.inSeconds < 60) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    if (diff.inDays < 7) return '${diff.inDays}d ago';
    if (diff.inDays < 365) return '${(diff.inDays / 7).floor()}w ago';
    return '${(diff.inDays / 365).floor()}y ago';
  }
}
