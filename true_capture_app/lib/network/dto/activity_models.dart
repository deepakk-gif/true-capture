// DTOs for the activity feed, post engagement (likes/comments) and stories.
// Wire-compatible with the backend `Modules.Social` records (camelCase JSON).

int _i(Object? v) => v is int ? v : (v is num ? v.toInt() : int.tryParse('$v') ?? 0);
DateTime? _d(Object? v) => v == null ? null : DateTime.tryParse(v.toString());

/// An activity-feed entry. [type] is one of: FollowRequest, FollowAccepted,
/// NewFollower, PostLiked, Commented, Mentioned, StoryMention,
/// AccountSuspended, AdminNotice.
class NotificationItem {
  const NotificationItem({
    required this.id,
    required this.type,
    this.actorUserId,
    this.actorUsername,
    this.actorDisplayName,
    this.actorAvatarUrl,
    this.postId,
    this.postImageUrl,
    this.text,
    this.isRead = false,
    this.createdAtUtc,
  });

  final int id;
  final String type;
  final int? actorUserId;
  final String? actorUsername;
  final String? actorDisplayName;
  final String? actorAvatarUrl;
  final int? postId;
  final String? postImageUrl;
  final String? text;
  final bool isRead;
  final DateTime? createdAtUtc;

  factory NotificationItem.fromJson(Map<String, dynamic> j) => NotificationItem(
        id: _i(j['id']),
        type: j['type']?.toString() ?? '',
        actorUserId: j['actorUserId'] == null ? null : _i(j['actorUserId']),
        actorUsername: j['actorUsername']?.toString(),
        actorDisplayName: j['actorDisplayName']?.toString(),
        actorAvatarUrl: j['actorAvatarUrl']?.toString(),
        postId: j['postId'] == null ? null : _i(j['postId']),
        postImageUrl: j['postImageUrl']?.toString(),
        text: j['text']?.toString(),
        isRead: j['isRead'] == true,
        createdAtUtc: _d(j['createdAtUtc']),
      );
}

/// A post with engagement counts (post-detail screen).
class PostDetail {
  const PostDetail({
    required this.id,
    required this.authorId,
    required this.authorUsername,
    this.authorDisplayName,
    this.authorAvatarUrl,
    this.authorIsBlueTick = false,
    required this.imageUrl,
    this.caption,
    this.createdAtUtc,
    this.likeCount = 0,
    this.commentCount = 0,
    this.likedByMe = false,
  });

  final int id;
  final int authorId;
  final String authorUsername;
  final String? authorDisplayName;
  final String? authorAvatarUrl;
  final bool authorIsBlueTick;
  final String imageUrl;
  final String? caption;
  final DateTime? createdAtUtc;
  final int likeCount;
  final int commentCount;
  final bool likedByMe;

  factory PostDetail.fromJson(Map<String, dynamic> j) => PostDetail(
        id: _i(j['id']),
        authorId: _i(j['authorId']),
        authorUsername: j['authorUsername']?.toString() ?? '',
        authorDisplayName: j['authorDisplayName']?.toString(),
        authorAvatarUrl: j['authorAvatarUrl']?.toString(),
        authorIsBlueTick: j['authorIsBlueTick'] == true,
        imageUrl: j['imageUrl']?.toString() ?? '',
        caption: j['caption']?.toString(),
        createdAtUtc: _d(j['createdAtUtc']),
        likeCount: _i(j['likeCount']),
        commentCount: _i(j['commentCount']),
        likedByMe: j['likedByMe'] == true,
      );

  PostDetail copyWith({int? likeCount, int? commentCount, bool? likedByMe}) => PostDetail(
        id: id,
        authorId: authorId,
        authorUsername: authorUsername,
        authorDisplayName: authorDisplayName,
        authorAvatarUrl: authorAvatarUrl,
        authorIsBlueTick: authorIsBlueTick,
        imageUrl: imageUrl,
        caption: caption,
        createdAtUtc: createdAtUtc,
        likeCount: likeCount ?? this.likeCount,
        commentCount: commentCount ?? this.commentCount,
        likedByMe: likedByMe ?? this.likedByMe,
      );
}

class CommentItem {
  const CommentItem({
    required this.id,
    required this.authorId,
    required this.authorUsername,
    this.authorDisplayName,
    this.authorAvatarUrl,
    required this.text,
    this.createdAtUtc,
  });

  final int id;
  final int authorId;
  final String authorUsername;
  final String? authorDisplayName;
  final String? authorAvatarUrl;
  final String text;
  final DateTime? createdAtUtc;

  factory CommentItem.fromJson(Map<String, dynamic> j) => CommentItem(
        id: _i(j['id']),
        authorId: _i(j['authorId']),
        authorUsername: j['authorUsername']?.toString() ?? '',
        authorDisplayName: j['authorDisplayName']?.toString(),
        authorAvatarUrl: j['authorAvatarUrl']?.toString(),
        text: j['text']?.toString() ?? '',
        createdAtUtc: _d(j['createdAtUtc']),
      );
}

class StoryItem {
  const StoryItem({
    required this.id,
    required this.authorId,
    required this.imageUrl,
    this.caption,
    this.createdAtUtc,
    this.expiresAtUtc,
  });

  final int id;
  final int authorId;
  final String imageUrl;
  final String? caption;
  final DateTime? createdAtUtc;
  final DateTime? expiresAtUtc;

  factory StoryItem.fromJson(Map<String, dynamic> j) => StoryItem(
        id: _i(j['id']),
        authorId: _i(j['authorId']),
        imageUrl: j['imageUrl']?.toString() ?? '',
        caption: j['caption']?.toString(),
        createdAtUtc: _d(j['createdAtUtc']),
        expiresAtUtc: _d(j['expiresAtUtc']),
      );
}

/// An author and their currently-active stories — one ring in the story tray.
class UserStories {
  const UserStories({
    required this.authorId,
    required this.username,
    this.displayName,
    this.avatarUrl,
    this.stories = const [],
  });

  final int authorId;
  final String username;
  final String? displayName;
  final String? avatarUrl;
  final List<StoryItem> stories;

  factory UserStories.fromJson(Map<String, dynamic> j) => UserStories(
        authorId: _i(j['authorId']),
        username: j['username']?.toString() ?? '',
        displayName: j['displayName']?.toString(),
        avatarUrl: j['avatarUrl']?.toString(),
        stories: (j['stories'] as List?)
                ?.map((e) => StoryItem.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
      );
}
