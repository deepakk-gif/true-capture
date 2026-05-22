import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../config/app_config.dart';
import '../../../../core/router/app_router.dart';
import '../../../../network/dto/activity_models.dart';
import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/user_avatar.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state_aware.dart';
import 'notification_feed_view_model.dart';

class NotificationFeedScreen extends ConsumerStatefulWidget {
  const NotificationFeedScreen({super.key});

  @override
  ConsumerState<NotificationFeedScreen> createState() => _NotificationFeedScreenState();
}

class _NotificationFeedScreenState
    extends BaseConsumerState<NotificationFeedScreen, NotificationFeedViewModel> {
  @override
  void onModelReady(NotificationFeedViewModel model) => model.load();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: const CustomAppBar(title: 'Notifications'),
      body: SafeArea(
        child: ScreenStateAware(
          state: viewModel.screenState,
          empty: const Center(child: Text('No notifications yet.')),
          builder: (context) {
            final groups = _grouped(viewModel.items);
            return ListView(
              children: [
                for (final group in groups) ...[
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
                    child: Text(group.key,
                        style: Theme.of(context).textTheme.titleSmall),
                  ),
                  ...group.value.map(_tile),
                ],
              ],
            );
          },
        ),
      ),
    );
  }

  Widget _tile(NotificationItem n) {
    final thumb = AppConfig.resolveUrl(n.postImageUrl);
    return ListTile(
      onTap: () => _onTap(n),
      leading: n.actorUserId != null
          ? UserAvatar(
              avatarUrl: n.actorAvatarUrl,
              name: n.actorDisplayName ?? n.actorUsername,
              radius: 22)
          : CircleAvatar(radius: 22, child: Icon(_icon(n.type))),
      title: Text(_text(n)),
      subtitle: Text(_relative(n.createdAtUtc),
          style: Theme.of(context).textTheme.bodySmall),
      trailing: thumb != null
          ? ClipRRect(
              borderRadius: BorderRadius.circular(4),
              child: Image.network(thumb, width: 44, height: 44, fit: BoxFit.cover),
            )
          : null,
    );
  }

  void _onTap(NotificationItem n) {
    switch (n.type) {
      case 'FollowRequest':
        AppRouter.push(context, ScreenPath.routeFollowRequests);
      case 'FollowAccepted':
      case 'NewFollower':
      case 'StoryMention':
        if (n.actorUserId != null) {
          AppRouter.push(context, ScreenPath.routeUserProfile,
              extra: {'userId': n.actorUserId});
        }
      case 'PostLiked':
      case 'Commented':
      case 'Mentioned':
        if (n.postId != null) {
          AppRouter.push(context, ScreenPath.routePostDetail,
              extra: {'postId': n.postId});
        }
    }
  }

  static IconData _icon(String type) => switch (type) {
        'AccountSuspended' => Icons.block,
        'AdminNotice'      => Icons.campaign_outlined,
        _                  => Icons.notifications,
      };

  String _text(NotificationItem n) {
    final who = n.actorDisplayName ?? n.actorUsername ?? 'Someone';
    return switch (n.type) {
      'FollowRequest'    => '$who requested to follow you',
      'FollowAccepted'   => '$who accepted your follow request',
      'NewFollower'      => '$who started following you',
      'PostLiked'        => '$who liked your post',
      'Commented'        => '$who commented on your post',
      'Mentioned'        => '$who mentioned you in a comment',
      'StoryMention'     => '$who mentioned you in their story',
      'AccountSuspended' => n.text ?? 'Your account was suspended',
      'AdminNotice'      => n.text ?? 'You have a new notice',
      _                  => n.text ?? 'New activity',
    };
  }

  /// Buckets the (newest-first) feed into Today / Yesterday / Last 7 days / Earlier.
  static List<MapEntry<String, List<NotificationItem>>> _grouped(
      List<NotificationItem> items) {
    final order = ['Today', 'Yesterday', 'Last 7 days', 'Earlier'];
    final map = <String, List<NotificationItem>>{};
    for (final n in items) {
      map.putIfAbsent(_bucket(n.createdAtUtc), () => []).add(n);
    }
    return [
      for (final key in order)
        if (map[key] != null) MapEntry(key, map[key]!),
    ];
  }

  static String _bucket(DateTime? utc) {
    if (utc == null) return 'Earlier';
    final d = utc.toLocal();
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final day = DateTime(d.year, d.month, d.day);
    final diff = today.difference(day).inDays;
    if (diff <= 0) return 'Today';
    if (diff == 1) return 'Yesterday';
    if (diff <= 7) return 'Last 7 days';
    return 'Earlier';
  }

  static String _relative(DateTime? utc) {
    if (utc == null) return '';
    final diff = DateTime.now().difference(utc.toLocal());
    if (diff.inMinutes < 1) return 'just now';
    if (diff.inHours < 1) return '${diff.inMinutes}m ago';
    if (diff.inDays < 1) return '${diff.inHours}h ago';
    if (diff.inDays < 7) return '${diff.inDays}d ago';
    return '${(diff.inDays / 7).floor()}w ago';
  }

  @override
  NotificationFeedViewModel createViewModel() =>
      ref.read(notificationFeedViewModelProvider);

  @override
  String screenName() => 'NOTIFICATIONS';
}
