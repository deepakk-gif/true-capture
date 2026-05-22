// DTOs for the social feature (search, follow graph, profiles), posts and
// in-app notices. Wire-compatible with the backend `Modules.Social` /
// `Modules.Notifications` records (camelCase JSON).

int _int(Object? v) => v is int ? v : (v is num ? v.toInt() : int.tryParse('$v') ?? 0);
DateTime? _date(Object? v) => v == null ? null : DateTime.tryParse(v.toString());

/// follow-state of the viewer towards another user.
class FollowState {
  FollowState._();
  static const String none      = 'none';
  static const String following = 'following';
  static const String requested = 'requested';
}

/// A row in user-search results.
class UserSearchItem {
  const UserSearchItem({
    required this.id,
    required this.username,
    this.displayName,
    this.avatarUrl,
    this.isBlueTick = false,
    this.mutualFollowers = const [],
    this.mutualFollowersCount = 0,
    this.followState = FollowState.none,
  });

  final int id;
  final String username;
  final String? displayName;
  final String? avatarUrl;
  final bool isBlueTick;
  final List<String> mutualFollowers;
  final int mutualFollowersCount;
  final String followState;

  factory UserSearchItem.fromJson(Map<String, dynamic> j) => UserSearchItem(
        id: _int(j['id']),
        username: j['username']?.toString() ?? '',
        displayName: j['displayName']?.toString(),
        avatarUrl: j['avatarUrl']?.toString(),
        isBlueTick: j['isBlueTick'] == true,
        mutualFollowers: (j['mutualFollowers'] as List?)
                ?.map((e) => e.toString())
                .toList() ??
            const [],
        mutualFollowersCount: _int(j['mutualFollowersCount']),
        followState: j['followState']?.toString() ?? FollowState.none,
      );
}

/// Another user's profile as seen by the viewer.
class UserProfileView {
  const UserProfileView({
    required this.id,
    required this.username,
    this.displayName,
    this.avatarUrl,
    this.bio,
    this.isBlueTick = false,
    this.accountType = 'public',
    this.joinedAtUtc,
    this.followersCount = 0,
    this.followingCount = 0,
    this.postsCount = 0,
    this.followState = FollowState.none,
    this.followsMe = false,
    this.isMe = false,
    this.canViewContent = true,
  });

  final int id;
  final String username;
  final String? displayName;
  final String? avatarUrl;
  final String? bio;
  final bool isBlueTick;
  final String accountType; // "public" | "private"
  final DateTime? joinedAtUtc;
  final int followersCount;
  final int followingCount;
  final int postsCount;
  final String followState;
  final bool followsMe;
  final bool isMe;
  final bool canViewContent;

  bool get isPrivate => accountType == 'private';

  factory UserProfileView.fromJson(Map<String, dynamic> j) => UserProfileView(
        id: _int(j['id']),
        username: j['username']?.toString() ?? '',
        displayName: j['displayName']?.toString(),
        avatarUrl: j['avatarUrl']?.toString(),
        bio: j['bio']?.toString(),
        isBlueTick: j['isBlueTick'] == true,
        accountType: j['accountType']?.toString() ?? 'public',
        joinedAtUtc: _date(j['joinedAtUtc']),
        followersCount: _int(j['followersCount']),
        followingCount: _int(j['followingCount']),
        postsCount: _int(j['postsCount']),
        followState: j['followState']?.toString() ?? FollowState.none,
        followsMe: j['followsMe'] == true,
        isMe: j['isMe'] == true,
        canViewContent: j['canViewContent'] == true,
      );
}

/// Result of follow / unfollow — the new follow-state.
class FollowActionResult {
  const FollowActionResult(this.followState);
  final String followState;
  factory FollowActionResult.fromJson(Map<String, dynamic> j) =>
      FollowActionResult(j['followState']?.toString() ?? FollowState.none);
}

/// A row in a followers / following / follow-requests list.
class FollowUserItem {
  const FollowUserItem({
    required this.id,
    required this.username,
    this.displayName,
    this.avatarUrl,
    this.isBlueTick = false,
    this.followState = FollowState.none,
  });

  final int id;
  final String username;
  final String? displayName;
  final String? avatarUrl;
  final bool isBlueTick;
  final String followState;

  factory FollowUserItem.fromJson(Map<String, dynamic> j) => FollowUserItem(
        id: _int(j['id']),
        username: j['username']?.toString() ?? '',
        displayName: j['displayName']?.toString(),
        avatarUrl: j['avatarUrl']?.toString(),
        isBlueTick: j['isBlueTick'] == true,
        followState: j['followState']?.toString() ?? FollowState.none,
      );
}

class FollowListResult {
  const FollowListResult({this.items = const [], this.nextCursor});
  final List<FollowUserItem> items;
  final String? nextCursor;
  factory FollowListResult.fromJson(Map<String, dynamic> j) => FollowListResult(
        items: (j['items'] as List?)
                ?.map((e) => FollowUserItem.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
        nextCursor: j['nextCursor']?.toString(),
      );
}

/// A post thumbnail in a profile / saved grid.
class PostItem {
  const PostItem({
    required this.id,
    required this.authorId,
    required this.coverUrl,
    this.type = 'normal',
    this.kind = 'photo',
    this.caption,
    this.createdAtUtc,
  });

  final int id;
  final int authorId;
  final String coverUrl;
  final String type; // "normal" | "fakeVsReal"
  final String kind; // "photo" | "carousel" | "video"
  final String? caption;
  final DateTime? createdAtUtc;

  bool get isVideo => kind == 'video';
  bool get isCarousel => kind == 'carousel';

  factory PostItem.fromJson(Map<String, dynamic> j) => PostItem(
        id: _int(j['id']),
        authorId: _int(j['authorId']),
        coverUrl: (j['coverUrl'] ?? j['imageUrl'])?.toString() ?? '',
        type: j['type']?.toString() ?? 'normal',
        kind: j['kind']?.toString() ?? 'photo',
        caption: j['caption']?.toString(),
        createdAtUtc: _date(j['createdAtUtc']),
      );
}

class PostListResult {
  const PostListResult({this.items = const [], this.nextCursor});
  final List<PostItem> items;
  final String? nextCursor;
  factory PostListResult.fromJson(Map<String, dynamic> j) => PostListResult(
        items: (j['items'] as List?)
                ?.map((e) => PostItem.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
        nextCursor: j['nextCursor']?.toString(),
      );
}

/// An in-app notice from the admin.
class NoticeItem {
  const NoticeItem({
    required this.id,
    required this.title,
    required this.body,
    this.isRead = false,
    this.createdAtUtc,
  });

  final int id;
  final String title;
  final String body;
  final bool isRead;
  final DateTime? createdAtUtc;

  factory NoticeItem.fromJson(Map<String, dynamic> j) => NoticeItem(
        id: _int(j['id']),
        title: j['title']?.toString() ?? '',
        body: j['body']?.toString() ?? '',
        isRead: j['isRead'] == true,
        createdAtUtc: _date(j['createdAtUtc']),
      );
}

class NoticeListResult {
  const NoticeListResult({this.items = const [], this.nextCursor});
  final List<NoticeItem> items;
  final String? nextCursor;
  factory NoticeListResult.fromJson(Map<String, dynamic> j) => NoticeListResult(
        items: (j['items'] as List?)
                ?.map((e) => NoticeItem.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
        nextCursor: j['nextCursor']?.toString(),
      );
}
