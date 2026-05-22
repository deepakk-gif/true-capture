import 'package:flutter/material.dart';

import '../../../../config/app_config.dart';
import '../../../../network/dto/activity_models.dart';

/// Full-screen story viewer. Tap the right half to advance, the left half to
/// go back; advancing past the last story closes the viewer.
class StoryViewerScreen extends StatefulWidget {
  const StoryViewerScreen({super.key, required this.userStories});

  final UserStories userStories;

  @override
  State<StoryViewerScreen> createState() => _StoryViewerScreenState();
}

class _StoryViewerScreenState extends State<StoryViewerScreen> {
  int _index = 0;

  List<StoryItem> get _stories => widget.userStories.stories;

  void _next() {
    if (_index < _stories.length - 1) {
      setState(() => _index++);
    } else {
      Navigator.of(context).maybePop();
    }
  }

  void _prev() {
    if (_index > 0) setState(() => _index--);
  }

  @override
  Widget build(BuildContext context) {
    if (_stories.isEmpty) {
      return const Scaffold(
        backgroundColor: Colors.black,
        body: Center(child: Text('No stories', style: TextStyle(color: Colors.white))),
      );
    }
    final story = _stories[_index];
    final image = AppConfig.resolveUrl(story.imageUrl);

    return Scaffold(
      backgroundColor: Colors.black,
      body: GestureDetector(
        onTapUp: (d) => d.localPosition.dx < MediaQuery.of(context).size.width / 2
            ? _prev()
            : _next(),
        child: Stack(
          fit: StackFit.expand,
          children: [
            if (image != null)
              Center(child: Image.network(image, fit: BoxFit.contain)),
            SafeArea(
              child: Column(
                children: [
                  // Segment progress bar — one bar per story.
                  Padding(
                    padding: const EdgeInsets.fromLTRB(8, 8, 8, 0),
                    child: Row(
                      children: [
                        for (var i = 0; i < _stories.length; i++)
                          Expanded(
                            child: Container(
                              height: 3,
                              margin: const EdgeInsets.symmetric(horizontal: 2),
                              color: i <= _index ? Colors.white : Colors.white24,
                            ),
                          ),
                      ],
                    ),
                  ),
                  ListTile(
                    title: Text(
                      widget.userStories.displayName ?? widget.userStories.username,
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
                    ),
                    trailing: IconButton(
                      icon: const Icon(Icons.close, color: Colors.white),
                      onPressed: () => Navigator.of(context).maybePop(),
                    ),
                  ),
                  const Spacer(),
                  if (story.caption != null && story.caption!.isNotEmpty)
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(16),
                      color: Colors.black54,
                      child: Text(story.caption!,
                          style: const TextStyle(color: Colors.white)),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
