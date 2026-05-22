import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/router/app_router.dart';
import '../../../../network/dto/activity_models.dart';
import '../../../common_widgets/user_avatar.dart';
import '../../../providers/repo_provider.dart';

/// Horizontal story tray shown at the top of the home tab — an "add story"
/// tile followed by a ring per author with active stories.
class StoryTray extends ConsumerStatefulWidget {
  const StoryTray({super.key});

  @override
  ConsumerState<StoryTray> createState() => _StoryTrayState();
}

class _StoryTrayState extends ConsumerState<StoryTray> {
  List<UserStories> _stories = const [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final stories = await ref.read(storyRepo).feed();
      if (mounted) setState(() {
        _stories = stories;
        _loading = false;
      });
    } catch (_) {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 104,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 8),
        children: [
          _addTile(context),
          if (_loading)
            const Padding(
              padding: EdgeInsets.all(32),
              child: SizedBox(
                  height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2)),
            ),
          for (final s in _stories) _ring(context, s),
        ],
      ),
    );
  }

  Widget _addTile(BuildContext context) {
    return _cell(
      label: 'Add story',
      onTap: () async {
        await AppRouter.push(context, ScreenPath.routeCreateStory);
        await _load();
      },
      child: CircleAvatar(
        radius: 30,
        backgroundColor: Theme.of(context).colorScheme.surfaceContainerHighest,
        child: const Icon(Icons.add, size: 28),
      ),
    );
  }

  Widget _ring(BuildContext context, UserStories s) {
    return _cell(
      label: s.displayName ?? s.username,
      onTap: () => AppRouter.push(context, ScreenPath.routeStoryViewer,
          extra: {'userStories': s}),
      child: Container(
        padding: const EdgeInsets.all(2.5),
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          border: Border.all(color: Theme.of(context).colorScheme.primary, width: 2),
        ),
        child: UserAvatar(
          avatarUrl: s.avatarUrl,
          name: s.displayName ?? s.username,
          radius: 28,
        ),
      ),
    );
  }

  Widget _cell({
    required String label,
    required Widget child,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: SizedBox(
        width: 76,
        child: Column(
          children: [
            const SizedBox(height: 6),
            child,
            const SizedBox(height: 4),
            Text(label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ),
    );
  }
}
