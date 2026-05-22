import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../core/router/app_router.dart';
import '../../../../../repositories/feed_repository.dart';
import '../../../../common_widgets/custom_app_bar.dart';
import '../../../social/story/story_tray.dart';
import '../feed_view.dart';

/// Tab 1 — Home: the Normal-post feed plus the story tray.
class TabHome extends ConsumerWidget {
  const TabHome({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      appBar: CustomAppBar(
        title: 'Home',
        actions: [
          IconButton(
            icon: const Icon(Icons.search),
            tooltip: 'Search users',
            onPressed: () =>
                AppRouter.push(context, ScreenPath.routeUserSearch),
          ),
        ],
      ),
      body: Column(
        children: const [
          StoryTray(),
          Divider(height: 1),
          Expanded(child: FeedView(channel: FeedChannel.home)),
        ],
      ),
    );
  }
}
