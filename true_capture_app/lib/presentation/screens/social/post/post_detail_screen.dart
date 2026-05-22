import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:share_plus/share_plus.dart';

import '../../../../core/router/app_router.dart';
import '../../../common_widgets/post_card.dart';
import '../../../providers/repo_provider.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state_aware.dart';
import 'post_detail_view_model.dart';

class PostDetailScreen extends ConsumerStatefulWidget {
  const PostDetailScreen({super.key, required this.postId});

  final int postId;

  @override
  ConsumerState<PostDetailScreen> createState() => _PostDetailScreenState();
}

class _PostDetailScreenState
    extends BaseConsumerState<PostDetailScreen, PostDetailViewModel> {
  @override
  void onModelReady(PostDetailViewModel model) => model.load(widget.postId);

  Future<void> _share() async {
    final url = await viewModel.share();
    if (url != null && url.isNotEmpty) await Share.share(url);
  }

  Future<bool> _follow() async {
    final p = viewModel.post;
    if (p == null) return false;
    try {
      await ref.read(socialRepo).follow(p.author.id);
      return true;
    } catch (_) {
      return false;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Post')),
      body: ListenableBuilder(
        listenable: viewModel,
        builder: (context, _) => ScreenStateAware(
          state: viewModel.screenState,
          builder: (context) {
            final post = viewModel.post;
            if (post == null) {
              return const SizedBox.shrink();
            }
            return SingleChildScrollView(
              child: Column(
                children: [
                  PostCard(
                    post: post,
                    onTapAuthor: () => AppRouter.push(
                      context,
                      ScreenPath.routeUserProfile,
                      extra: {'userId': post.author.id},
                    ),
                    onLike: viewModel.toggleLike,
                    onSave: viewModel.toggleSave,
                    onShare: _share,
                    onComment: () => AppRouter.push(
                      context,
                      ScreenPath.routeComments,
                      extra: {'postId': post.id},
                    ),
                    onVote: viewModel.vote,
                    onFollow: _follow,
                    onReport: (reason, other) => viewModel.report(reason, other),
                  ),
                  ListTile(
                    leading: const Icon(Icons.mode_comment_outlined),
                    title: Text('View all ${post.commentCount} comments'),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => AppRouter.push(
                      context,
                      ScreenPath.routeComments,
                      extra: {'postId': post.id},
                    ),
                  ),
                  if (post.isFakeVsReal && post.references.isNotEmpty)
                    _references(post.references),
                ],
              ),
            );
          },
        ),
      ),
    );
  }

  Widget _references(List<String> refs) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('References', style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 4),
          for (final r in refs)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 2),
              child: Row(
                children: [
                  const Icon(Icons.link, size: 16),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(r,
                        style: const TextStyle(color: Colors.blue),
                        overflow: TextOverflow.ellipsis),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }

  @override
  PostDetailViewModel createViewModel() => ref.read(postDetailViewModelProvider);

  @override
  String screenName() => 'POST DETAIL';
}
