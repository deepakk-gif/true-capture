import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/router/app_router.dart';
import '../../../../network/dto/social_models.dart';
import '../../../../services/recent_search_service.dart';
import '../../../common_widgets/user_list_row.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import 'user_search_view_model.dart';

class UserSearchScreen extends ConsumerStatefulWidget {
  const UserSearchScreen({super.key});

  @override
  ConsumerState<UserSearchScreen> createState() => _UserSearchScreenState();
}

class _UserSearchScreenState
    extends BaseConsumerState<UserSearchScreen, UserSearchViewModel> {
  final _controller = TextEditingController();

  @override
  void onModelReady(UserSearchViewModel model) => model.loadRecents();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _openUser({
    required int id,
    required String username,
    String? displayName,
    String? avatarUrl,
  }) {
    viewModel.remember(RecentSearchUser(
      id: id, username: username, displayName: displayName, avatarUrl: avatarUrl));
    AppRouter.push(context, ScreenPath.routeUserProfile, extra: {'userId': id});
  }

  static String _mutualText(UserSearchItem u) {
    if (u.mutualFollowersCount == 0) return '@${u.username}';
    final shown = u.mutualFollowers.join(', ');
    final extra = u.mutualFollowersCount - u.mutualFollowers.length;
    return extra > 0 ? 'Followed by $shown +$extra more' : 'Followed by $shown';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: TextField(
          controller: _controller,
          autofocus: true,
          textInputAction: TextInputAction.search,
          decoration: const InputDecoration(
            hintText: 'Search users',
            border: InputBorder.none,
          ),
          onChanged: viewModel.onQueryChanged,
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.clear),
            onPressed: () {
              _controller.clear();
              viewModel.onQueryChanged('');
            },
          ),
        ],
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: viewModel,
          builder: (context, _) =>
              viewModel.hasQuery ? _results(context) : _recents(context),
        ),
      ),
    );
  }

  Widget _recents(BuildContext context) {
    if (viewModel.recents.isEmpty) {
      return const Center(child: Text('Search for people to follow.'));
    }
    return ListView(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 8, 0),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Recent', style: Theme.of(context).textTheme.titleSmall),
              TextButton(
                onPressed: viewModel.clearRecents,
                child: const Text('Clear all'),
              ),
            ],
          ),
        ),
        ...viewModel.recents.map((u) => UserListRow(
              username: u.username,
              displayName: u.displayName,
              avatarUrl: u.avatarUrl,
              onTap: () => _openUser(
                  id: u.id,
                  username: u.username,
                  displayName: u.displayName,
                  avatarUrl: u.avatarUrl),
              trailing: IconButton(
                icon: const Icon(Icons.close, size: 18),
                onPressed: () => viewModel.removeRecent(u.id),
              ),
            )),
      ],
    );
  }

  Widget _results(BuildContext context) {
    if (viewModel.searching && viewModel.results.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }
    if (viewModel.results.isEmpty) {
      return const Center(child: Text('No users found.'));
    }
    return ListView(
      children: viewModel.results
          .map((u) => UserListRow(
                username: u.username,
                displayName: u.displayName,
                avatarUrl: u.avatarUrl,
                isBlueTick: u.isBlueTick,
                subtitle: _mutualText(u),
                onTap: () => _openUser(
                    id: u.id,
                    username: u.username,
                    displayName: u.displayName,
                    avatarUrl: u.avatarUrl),
              ))
          .toList(),
    );
  }

  @override
  UserSearchViewModel createViewModel() => ref.read(userSearchViewModelProvider);

  @override
  String screenName() => 'USER SEARCH';
}
