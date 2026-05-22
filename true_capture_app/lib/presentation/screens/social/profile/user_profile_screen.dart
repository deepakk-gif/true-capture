import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../config/app_config.dart';
import '../../../../core/router/app_router.dart';
import '../../../../network/dto/social_models.dart';
import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/user_avatar.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state_aware.dart';
import 'user_profile_view_model.dart';

class UserProfileScreen extends ConsumerStatefulWidget {
  const UserProfileScreen({super.key, required this.userId});

  final int userId;

  @override
  ConsumerState<UserProfileScreen> createState() => _UserProfileScreenState();
}

class _UserProfileScreenState
    extends BaseConsumerState<UserProfileScreen, UserProfileViewModel> {
  @override
  void onModelReady(UserProfileViewModel model) => model.load(widget.userId);

  void _openFollowList(String type) => AppRouter.push(
        context,
        ScreenPath.routeFollowList,
        extra: {'userId': widget.userId, 'type': type},
      );

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: const CustomAppBar(title: 'Profile'),
      body: SafeArea(
        child: ScreenStateAware(
          state: viewModel.screenState,
          builder: (context) => ListenableBuilder(
            listenable: viewModel,
            builder: (context, _) {
              final p = viewModel.profile;
              if (p == null) return const SizedBox.shrink();
              return ListView(
                padding: const EdgeInsets.only(bottom: 24),
                children: [
                  _header(context, p),
                  const Divider(height: 32),
                  if (p.canViewContent)
                    _postsGrid(context)
                  else
                    _privateLock(context),
                ],
              );
            },
          ),
        ),
      ),
    );
  }

  Widget _header(BuildContext context, UserProfileView p) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              UserAvatar(avatarUrl: p.avatarUrl, name: p.displayName ?? p.username, radius: 40),
              const SizedBox(width: 20),
              Expanded(
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                  children: [
                    _stat('Posts', p.postsCount, null),
                    _stat('Followers', p.followersCount,
                        p.canViewContent ? () => _openFollowList('followers') : null),
                    _stat('Following', p.followingCount,
                        p.canViewContent ? () => _openFollowList('following') : null),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              Flexible(
                child: Text(
                  p.displayName ?? p.username,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context)
                      .textTheme
                      .titleMedium
                      ?.copyWith(fontWeight: FontWeight.bold),
                ),
              ),
              if (p.isBlueTick) ...[
                const SizedBox(width: 4),
                const Icon(Icons.verified, size: 18, color: Colors.blue),
              ],
            ],
          ),
          Text('@${p.username}', style: Theme.of(context).textTheme.bodySmall),
          if (p.bio != null && p.bio!.isNotEmpty) ...[
            const SizedBox(height: 8),
            Text(p.bio!, style: Theme.of(context).textTheme.bodyMedium),
          ],
          if (!p.isMe) ...[
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(child: _followButton(p)),
                const SizedBox(width: 12),
                Expanded(
                  child: OutlinedButton(
                    onPressed: () => AppRouter.push(
                      context,
                      ScreenPath.routeChat,
                      extra: {'userId': p.id, 'title': p.displayName ?? p.username},
                    ),
                    child: const Text('Message'),
                  ),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  Widget _followButton(UserProfileView p) {
    final (label, filled) = switch (p.followState) {
      FollowState.following => ('Following', false),
      FollowState.requested => ('Requested', false),
      _                     => ('Follow', true),
    };
    final child = viewModel.busyFollow
        ? const SizedBox(
            height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
        : Text(label);
    final onPressed = viewModel.busyFollow ? null : viewModel.toggleFollow;
    return filled
        ? ElevatedButton(onPressed: onPressed, child: child)
        : OutlinedButton(onPressed: onPressed, child: child);
  }

  Widget _stat(String label, int count, VoidCallback? onTap) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text('$count',
                style: Theme.of(context)
                    .textTheme
                    .titleMedium
                    ?.copyWith(fontWeight: FontWeight.bold)),
            Text(label, style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ),
    );
  }

  Widget _postsGrid(BuildContext context) {
    final posts = viewModel.posts;
    if (posts.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 48),
        child: Center(child: Text('No posts yet.')),
      );
    }
    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      padding: const EdgeInsets.symmetric(horizontal: 2),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 3, mainAxisSpacing: 2, crossAxisSpacing: 2),
      itemCount: posts.length,
      itemBuilder: (context, i) {
        final url = AppConfig.resolveUrl(posts[i].coverUrl);
        return GestureDetector(
          onTap: () => AppRouter.push(context, ScreenPath.routePostDetail,
              extra: {'postId': posts[i].id}),
          child: Container(
            color: Theme.of(context).colorScheme.surfaceContainerHighest,
            child: url == null
                ? const Icon(Icons.image_outlined)
                : Image.network(url, fit: BoxFit.cover),
          ),
        );
      },
    );
  }

  Widget _privateLock(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 48, horizontal: 24),
      child: Column(
        children: [
          const Icon(Icons.lock_outline, size: 40),
          const SizedBox(height: 12),
          Text('This account is private',
              style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 4),
          const Text(
            'Follow this account to see their posts, followers and following.',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }

  @override
  UserProfileViewModel createViewModel() => ref.read(userProfileViewModelProvider);

  @override
  String screenName() => 'USER PROFILE';
}
