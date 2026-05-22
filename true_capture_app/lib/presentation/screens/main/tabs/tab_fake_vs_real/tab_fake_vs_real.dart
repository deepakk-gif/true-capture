import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../../repositories/feed_repository.dart';
import '../../../../common_widgets/custom_app_bar.dart';
import '../feed_view.dart';

/// Tab 2 — Fake vs Real: the dedicated credibility feed. Posts here come from
/// admins and access-granted users and are always public.
class TabFakeVsReal extends ConsumerWidget {
  const TabFakeVsReal({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return const Scaffold(
      appBar: CustomAppBar(title: 'Fake vs Real'),
      body: FeedView(channel: FeedChannel.fakeVsReal),
    );
  }
}
