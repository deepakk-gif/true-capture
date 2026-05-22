import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/router/app_router.dart';
import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/user_list_row.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state_aware.dart';
import 'follow_list_view_model.dart';

class FollowListScreen extends ConsumerStatefulWidget {
  const FollowListScreen({super.key, required this.userId, required this.type});

  final int userId;
  final String type; // "followers" | "following"

  @override
  ConsumerState<FollowListScreen> createState() => _FollowListScreenState();
}

class _FollowListScreenState
    extends BaseConsumerState<FollowListScreen, FollowListViewModel> {
  @override
  void onModelReady(FollowListViewModel model) => model.load(widget.userId, widget.type);

  @override
  Widget build(BuildContext context) {
    final title = widget.type == 'following' ? 'Following' : 'Followers';
    return Scaffold(
      appBar: CustomAppBar(title: title),
      body: SafeArea(
        child: ScreenStateAware(
          state: viewModel.screenState,
          empty: Center(child: Text('No $title.'.toLowerCase())),
          builder: (context) => ListView(
            children: viewModel.items
                .map((u) => UserListRow(
                      username: u.username,
                      displayName: u.displayName,
                      avatarUrl: u.avatarUrl,
                      isBlueTick: u.isBlueTick,
                      onTap: () => AppRouter.push(
                        context,
                        ScreenPath.routeUserProfile,
                        extra: {'userId': u.id},
                      ),
                    ))
                .toList(),
          ),
        ),
      ),
    );
  }

  @override
  FollowListViewModel createViewModel() => ref.read(followListViewModelProvider);

  @override
  String screenName() => 'FOLLOW LIST';
}
