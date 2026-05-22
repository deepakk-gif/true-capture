import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:share_plus/share_plus.dart';

import '../../../../core/router/app_router.dart';
import '../../../../network/dto/post_models.dart';
import '../../../../repositories/feed_repository.dart';
import '../../../common_widgets/post_card.dart';
import '../../../providers/repo_provider.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state_aware.dart';
import 'feed_view_model.dart';

/// Scrollable post feed shared by the Home tab and the Fake vs Real tab.
/// Pull-to-refresh + cursor-based infinite scroll.
class FeedView extends ConsumerStatefulWidget {
  const FeedView({super.key, required this.channel});

  /// One of [FeedChannel.home] / [FeedChannel.fakeVsReal].
  final String channel;

  @override
  ConsumerState<FeedView> createState() => _FeedViewState();
}

class _FeedViewState extends BaseConsumerState<FeedView, FeedViewModel> {
  final _scrollController = ScrollController();

  @override
  void onModelReady(FeedViewModel model) {
    _scrollController.addListener(_onScroll);
    model.load();
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 400) {
      viewModel.loadMore();
    }
  }

  Future<void> _share(PostDto post) async {
    final url = await viewModel.share(post);
    if (url != null && url.isNotEmpty) {
      await Share.share(url);
    }
  }

  Future<bool> _follow(int userId) async {
    try {
      await ref.read(socialRepo).follow(userId);
      return true;
    } catch (_) {
      return false;
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: viewModel,
      builder: (context, _) => ScreenStateAware(
        state: viewModel.screenState,
        empty: _empty(context),
        error: (_) => _ErrorRetry(onRetry: viewModel.load),
        builder: (context) => RefreshIndicator(
          onRefresh: viewModel.refresh,
          child: ListView.builder(
            controller: _scrollController,
            physics: const AlwaysScrollableScrollPhysics(),
            itemCount: viewModel.posts.length + (viewModel.hasMore ? 1 : 0),
            itemBuilder: (context, index) {
              if (index >= viewModel.posts.length) {
                return const Padding(
                  padding: EdgeInsets.all(16),
                  child: Center(child: CircularProgressIndicator()),
                );
              }
              final post = viewModel.posts[index];
              return PostCard(
                post: post,
                onTapAuthor: () => AppRouter.push(
                  context,
                  ScreenPath.routeUserProfile,
                  extra: {'userId': post.author.id},
                ),
                onTapPost: () => AppRouter.push(
                  context,
                  ScreenPath.routePostDetail,
                  extra: {'postId': post.id},
                ),
                onLike: () => viewModel.toggleLike(post),
                onSave: () => viewModel.toggleSave(post),
                onShare: () => _share(post),
                onComment: () => AppRouter.push(
                  context,
                  ScreenPath.routeComments,
                  extra: {'postId': post.id},
                ),
                onVote: (v) => viewModel.vote(post, v),
                onFollow: () => _follow(post.author.id),
                onReport: (reason, other) async {
                  final ok = await viewModel.report(post.id, reason, other);
                  if (!ok) {
                    // The error is surfaced by the base error listener.
                  }
                },
              );
            },
          ),
        ),
      ),
    );
  }

  Widget _empty(BuildContext context) {
    final isHome = widget.channel == FeedChannel.home;
    return RefreshIndicator(
      onRefresh: viewModel.refresh,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          SizedBox(
            height: MediaQuery.of(context).size.height * 0.6,
            child: Center(
              child: Text(
                isHome
                    ? 'No posts yet — follow people or check back later.'
                    : 'No Fake vs Real posts yet.',
                style: TextStyle(color: Theme.of(context).hintColor),
              ),
            ),
          ),
        ],
      ),
    );
  }

  @override
  FeedViewModel createViewModel() => ref.read(feedViewModelProvider(widget.channel));

  @override
  String screenName() => 'FEED_${widget.channel}';
}

class _ErrorRetry extends StatelessWidget {
  const _ErrorRetry({required this.onRetry});
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Text('Could not load the feed.'),
          const SizedBox(height: 8),
          OutlinedButton(onPressed: onRetry, child: const Text('Retry')),
        ],
      ),
    );
  }
}
