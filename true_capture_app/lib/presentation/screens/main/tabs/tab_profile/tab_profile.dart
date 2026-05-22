import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../core/router/app_router.dart';
import '../../../../../network/dto/request/auth/refresh_token_request.dart';
import '../../../../../services/social_login_service.dart';
import '../../../../common_widgets/custom_app_bar.dart';
import '../../../../common_widgets/user_avatar.dart';
import '../../../../providers/local_storage_provider.dart';
import '../../../../providers/repo_provider.dart';
import '../../../../providers/theme_provider.dart';
import '../../../../providers/user_data_provider.dart';

/// Tab 5 — Profile: the signed-in user's profile + settings hub
/// (see PRD §6 Tab 4 — header, theme, sign out).
class TabProfile extends ConsumerWidget {
  const TabProfile({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authStateNotifierProvider);
    final mode = ref.watch(themeProvider);

    return Scaffold(
      appBar: CustomAppBar(
        title: 'Profile',
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_none),
            tooltip: 'Notifications',
            onPressed: () =>
                AppRouter.push(context, ScreenPath.routeNotifications),
          ),
        ],
      ),
      body: ListView(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 24, 20, 0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Avatar + posts/followers/following counts (Instagram-style).
                Row(
                  children: [
                    UserAvatar(
                      avatarUrl: user?.avatarUrl,
                      name: user?.name,
                      radius: 40,
                    ),
                    const SizedBox(width: 20),
                    Expanded(
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                        children: [
                          _StatColumn(
                              count: user?.postsCount ?? 0, label: 'Posts'),
                          _StatColumn(
                              count: user?.followersCount ?? 0,
                              label: 'Followers'),
                          _StatColumn(
                              count: user?.followingCount ?? 0,
                              label: 'Following'),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Flexible(
                      child: Text(
                        user?.name ?? 'Guest',
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(context)
                            .textTheme
                            .titleMedium
                            ?.copyWith(fontWeight: FontWeight.bold),
                      ),
                    ),
                    if (user?.isBlueTick ?? false) ...[
                      const SizedBox(width: 4),
                      const Icon(Icons.verified,
                          size: 18, color: Colors.blue),
                    ],
                  ],
                ),
                if (user?.username != null && user!.username!.isNotEmpty)
                  Text('@${user.username}',
                      style: Theme.of(context).textTheme.bodySmall),
                const SizedBox(height: 2),
                Text(user?.email ?? '',
                    style: Theme.of(context).textTheme.bodySmall),
                if (user?.bio != null && user!.bio!.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  Text(user.bio!,
                      style: Theme.of(context).textTheme.bodyMedium),
                ],
              ],
            ),
          ),
          const SizedBox(height: 24),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.edit_outlined),
            title: const Text('Edit profile'),
            onTap: () =>
                AppRouter.push(context, ScreenPath.routeEditProfile),
          ),
          ListTile(
            leading: const Icon(Icons.brightness_6_outlined),
            title: const Text('Theme'),
            subtitle: Text(mode.name),
            onTap: () {
              final next = switch (mode) {
                ThemeMode.system => ThemeMode.light,
                ThemeMode.light => ThemeMode.dark,
                ThemeMode.dark => ThemeMode.system,
              };
              ref.read(themeProvider.notifier).setMode(next);
            },
          ),
          ListTile(
            leading: const Icon(Icons.logout),
            title: const Text('Sign out'),
            onTap: () async {
              final storage = ref.read(localStorageServiceProvider);
              final refreshToken =
                  await storage.read(StorageKeys.refreshTokenKey);
              final fcmToken = await storage.read(StorageKeys.fcmTokenKey);

              // Tell the backend to revoke the refresh token + drop the
              // device row so this device stops receiving topic pushes.
              // Best-effort — if this fails (offline, server down), we still
              // clear local state so the user can sign back in.
              if (refreshToken != null && refreshToken.isNotEmpty) {
                try {
                  await ref.read(authRepo).signOut(
                        RefreshTokenRequest(
                          refreshToken: refreshToken,
                          fcmToken: fcmToken,
                        ),
                      );
                } catch (_) {
                  // ignore — proceed with local cleanup
                }
              }

              await SocialLoginService.instance.signOut();
              await storage.delete(StorageKeys.fcmTokenKey);
              await ref.read(authStateNotifierProvider.notifier).clear();

              if (!context.mounted) return;
              AppRouter.go(context, ScreenPath.routeSignIn);
            },
          ),
        ],
      ),
    );
  }
}

/// A single posts/followers/following stat in the profile header.
class _StatColumn extends StatelessWidget {
  const _StatColumn({required this.count, required this.label});

  final int count;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          '$count',
          style: Theme.of(context)
              .textTheme
              .titleMedium
              ?.copyWith(fontWeight: FontWeight.bold),
        ),
        Text(label, style: Theme.of(context).textTheme.bodySmall),
      ],
    );
  }
}
