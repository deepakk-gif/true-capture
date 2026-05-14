import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../core/router/app_router.dart';
import '../../../../../network/dto/request/auth/refresh_token_request.dart';
import '../../../../../services/social_login_service.dart';
import '../../../../common_widgets/custom_app_bar.dart';
import '../../../../providers/local_storage_provider.dart';
import '../../../../providers/repo_provider.dart';
import '../../../../providers/theme_provider.dart';
import '../../../../providers/user_data_provider.dart';

class TabSettings extends ConsumerWidget {
  const TabSettings({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final mode = ref.watch(themeProvider);
    return Scaffold(
      appBar: const CustomAppBar(title: 'Settings'),
      body: ListView(
        children: [
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
              final fcmToken =
                  await storage.read(StorageKeys.fcmTokenKey);

              // Tell the backend to revoke the refresh token + drop the
              // device row so this device stops receiving topic pushes.
              // Best-effort — if this fails (offline, server down), we still
              // clear local state so the user can sign back in.
              if (refreshToken != null && refreshToken.isNotEmpty) {
                try {
                  await ref.read(authRepo).signOut(
                        RefreshTokenRequest(
                          refreshToken: refreshToken,
                          fcmToken:     fcmToken,
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
