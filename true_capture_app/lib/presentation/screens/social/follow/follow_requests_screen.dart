import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/router/app_router.dart';
import '../../../../network/dto/social_models.dart';
import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/user_list_row.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state_aware.dart';
import 'follow_requests_view_model.dart';

class FollowRequestsScreen extends ConsumerStatefulWidget {
  const FollowRequestsScreen({super.key});

  @override
  ConsumerState<FollowRequestsScreen> createState() => _FollowRequestsScreenState();
}

class _FollowRequestsScreenState
    extends BaseConsumerState<FollowRequestsScreen, FollowRequestsViewModel> {
  @override
  void onModelReady(FollowRequestsViewModel model) => model.load();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: const CustomAppBar(title: 'Follow requests'),
      body: SafeArea(
        child: ScreenStateAware(
          state: viewModel.screenState,
          empty: const Center(child: Text('No pending follow requests.')),
          builder: (context) => ListenableBuilder(
            listenable: viewModel,
            builder: (context, _) => ListView(
              children: viewModel.rows
                  .map((row) => UserListRow(
                        username: row.user.username,
                        displayName: row.user.displayName,
                        avatarUrl: row.user.avatarUrl,
                        isBlueTick: row.user.isBlueTick,
                        subtitle: row.accepted ? 'Request accepted' : null,
                        onTap: () => AppRouter.push(
                          context,
                          ScreenPath.routeUserProfile,
                          extra: {'userId': row.user.id},
                        ),
                        trailing: _trailing(row),
                      ))
                  .toList(),
            ),
          ),
        ),
      ),
    );
  }

  Widget _trailing(FollowRequestRow row) {
    final id = row.user.id;
    if (viewModel.isBusy(id)) {
      return const SizedBox(
        height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2));
    }

    // Accepted — offer "Follow back" (or the resulting follow-state).
    if (row.accepted) {
      final (label, filled) = switch (row.followState) {
        FollowState.following => ('Following', false),
        FollowState.requested => ('Requested', false),
        _                     => ('Follow back', true),
      };
      return filled
          ? FilledButton(
              style: FilledButton.styleFrom(visualDensity: VisualDensity.compact),
              onPressed: () => viewModel.toggleFollowBack(id),
              child: Text(label),
            )
          : OutlinedButton(
              style: OutlinedButton.styleFrom(visualDensity: VisualDensity.compact),
              onPressed: () => viewModel.toggleFollowBack(id),
              child: Text(label),
            );
    }

    // Pending — accept or cancel.
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        FilledButton(
          style: FilledButton.styleFrom(visualDensity: VisualDensity.compact),
          onPressed: () => viewModel.accept(id),
          child: const Text('Accept'),
        ),
        const SizedBox(width: 6),
        OutlinedButton(
          style: OutlinedButton.styleFrom(visualDensity: VisualDensity.compact),
          onPressed: () => viewModel.cancel(id),
          child: const Text('Cancel'),
        ),
      ],
    );
  }

  @override
  FollowRequestsViewModel createViewModel() =>
      ref.read(followRequestsViewModelProvider);

  @override
  String screenName() => 'FOLLOW REQUESTS';
}
