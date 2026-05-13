import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../common_widgets/custom_app_bar.dart';
import '../../../../providers/user_data_provider.dart';

class TabProfile extends ConsumerWidget {
  const TabProfile({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authStateNotifierProvider);
    return Scaffold(
      appBar: const CustomAppBar(title: 'Profile'),
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const CircleAvatar(radius: 40, child: Icon(Icons.person, size: 40)),
            const SizedBox(height: 12),
            Text(user?.name ?? 'Guest',
                style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 4),
            Text(user?.email ?? '',
                style: Theme.of(context).textTheme.bodyMedium),
          ],
        ),
      ),
    );
  }
}
