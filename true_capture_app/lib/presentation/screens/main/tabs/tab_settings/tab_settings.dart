import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../core/router/app_router.dart';
import '../../../../common_widgets/custom_app_bar.dart';
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
