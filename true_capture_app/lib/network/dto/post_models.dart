// DTOs for the Create Post module — posts, media pipeline, feed and engagement.
// Wire-compatible with the backend `Modules.Social` records (camelCase JSON).

int _i(Object? v) => v is int ? v : (v is num ? v.toInt() : int.tryParse('$v') ?? 0);
DateTime? _d(Object? v) => v == null ? null : DateTime.tryParse(v.toString());

/// Post type discriminator.
class PostType {
  PostType._();
  static const String normal     = 'normal';
  static const String fakeVsReal = 'fakeVsReal';
}

/// A signed upload slot returned by `POST /api/media/uploads`.
class UploadTicket {
  const UploadTicket({required this.uploadId, required this.putUrl, this.expiresAtUtc});

  final int uploadId;
  final String putUrl;
  final DateTime? expiresAtUtc;

  factory UploadTicket.fromJson(Map<String, dynamic> j) => UploadTicket(
        uploadId: _i(j['uploadId']),
        putUrl: j['putUrl']?.toString() ?? '',
        expiresAtUtc: _d(j['expiresAtUtc']),
      );
}

/// A finalized media asset (`POST /api/media/finalize`).
class MediaAssetDto {
  const MediaAssetDto({
    required this.id,
    required this.kind,
    required this.status,
    required this.url,
    this.thumbnailUrl,
    this.durationSeconds,
  });

  final int id;
  final String kind;   // "photo" | "video"
  final String status; // "pending" | "ready" | "failed"
  final String url;
  final String? thumbnailUrl;
  final int? durationSeconds;

  bool get isVideo => kind == 'video';

  factory MediaAssetDto.fromJson(Map<String, dynamic> j) => MediaAssetDto(
        id: _i(j['id']),
        kind: j['kind']?.toString() ?? 'photo',
        status: j['status']?.toString() ?? 'pending',
        url: j['url']?.toString() ?? '',
        thumbnailUrl: j['thumbnailUrl']?.toString(),
        durationSeconds: j['durationSeconds'] == null ? null : _i(j['durationSeconds']),
      );
}

/// One media item inside a post.
class PostMediaDto {
  const PostMediaDto({
    required this.id,
    required this.kind,
    required this.url,
    this.thumbnailUrl,
    this.durationSeconds,
    this.position = 0,
  });

  final int id;
  final String kind; // "photo" | "video"
  final String url;
  final String? thumbnailUrl;
  final int? durationSeconds;
  final int position;

  bool get isVideo => kind == 'video';

  factory PostMediaDto.fromJson(Map<String, dynamic> j) => PostMediaDto(
        id: _i(j['id']),
        kind: j['kind']?.toString() ?? 'photo',
        url: j['url']?.toString() ?? '',
        thumbnailUrl: j['thumbnailUrl']?.toString(),
        durationSeconds: j['durationSeconds'] == null ? null : _i(j['durationSeconds']),
        position: _i(j['position']),
      );
}

/// The post author summary embedded in a [PostDto].
class PostAuthorDto {
  const PostAuthorDto({
    required this.id,
    required this.username,
    this.displayName,
    this.avatarUrl,
    this.isBlueTick = false,
    this.followState = 'none',
  });

  final int id;
  final String username;
  final String? displayName;
  final String? avatarUrl;
  final bool isBlueTick;
  final String followState; // "none" | "following" | "requested"

  bool get isFollowing => followState == 'following' || followState == 'requested';

  factory PostAuthorDto.fromJson(Map<String, dynamic> j) => PostAuthorDto(
        id: _i(j['id']),
        username: j['username']?.toString() ?? '',
        displayName: j['displayName']?.toString(),
        avatarUrl: j['avatarUrl']?.toString(),
        isBlueTick: j['isBlueTick'] == true,
        followState: j['followState']?.toString() ?? 'none',
      );
}

/// A full post — backs the feed list, the post card and the detail screen.
class PostDto {
  PostDto({
    required this.id,
    required this.type,
    required this.kind,
    required this.status,
    required this.isAdminPost,
    required this.shareId,
    required this.author,
    this.caption,
    this.media = const [],
    this.references = const [],
    this.createdAtUtc,
    this.viewCount = 0,
    this.likeCount = 0,
    this.commentCount = 0,
    this.shareCount = 0,
    this.trueVotes = 0,
    this.falseVotes = 0,
    this.likedByMe = false,
    this.savedByMe = false,
    this.myVote,
  });

  final int id;
  final String type;   // "normal" | "fakeVsReal"
  final String kind;   // "photo" | "carousel" | "video"
  final String status; // "live" | "pendingReview" | "removed"
  final bool isAdminPost;
  final String shareId;
  final PostAuthorDto author;
  final String? caption;
  final List<PostMediaDto> media;
  final List<String> references;
  final DateTime? createdAtUtc;
  int viewCount;
  int likeCount;
  int commentCount;
  int shareCount;
  int trueVotes;
  int falseVotes;
  bool likedByMe;
  bool savedByMe;
  bool? myVote;

  bool get isFakeVsReal => type == PostType.fakeVsReal;

  factory PostDto.fromJson(Map<String, dynamic> j) => PostDto(
        id: _i(j['id']),
        type: j['type']?.toString() ?? 'normal',
        kind: j['kind']?.toString() ?? 'photo',
        status: j['status']?.toString() ?? 'live',
        isAdminPost: j['isAdminPost'] == true,
        shareId: j['shareId']?.toString() ?? '',
        author: PostAuthorDto.fromJson(
            (j['author'] as Map?)?.cast<String, dynamic>() ?? const {}),
        caption: j['caption']?.toString(),
        media: (j['media'] as List?)
                ?.map((e) => PostMediaDto.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
        references: (j['references'] as List?)
                ?.map((e) => e.toString())
                .toList() ??
            const [],
        createdAtUtc: _d(j['createdAtUtc']),
        viewCount: _i(j['viewCount']),
        likeCount: _i(j['likeCount']),
        commentCount: _i(j['commentCount']),
        shareCount: _i(j['shareCount']),
        trueVotes: _i(j['trueVotes']),
        falseVotes: _i(j['falseVotes']),
        likedByMe: j['likedByMe'] == true,
        savedByMe: j['savedByMe'] == true,
        myVote: j['myVote'] is bool ? j['myVote'] as bool : null,
      );
}

/// A page of posts (`GET /api/feed`).
class FeedResult {
  const FeedResult({this.items = const [], this.nextCursor});
  final List<PostDto> items;
  final String? nextCursor;

  factory FeedResult.fromJson(Map<String, dynamic> j) => FeedResult(
        items: (j['items'] as List?)
                ?.map((e) => PostDto.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
        nextCursor: j['nextCursor']?.toString(),
      );
}

/// A comment or 1-level reply.
class CommentDto {
  CommentDto({
    required this.id,
    required this.postId,
    this.parentCommentId,
    required this.authorId,
    required this.authorUsername,
    this.authorDisplayName,
    this.authorAvatarUrl,
    this.authorIsBlueTick = false,
    required this.text,
    this.likeCount = 0,
    this.likedByMe = false,
    this.repliesCount = 0,
    this.isRemoved = false,
    this.createdAtUtc,
  });

  final int id;
  final int postId;
  final int? parentCommentId;
  final int authorId;
  final String authorUsername;
  final String? authorDisplayName;
  final String? authorAvatarUrl;
  final bool authorIsBlueTick;
  final String text;
  int likeCount;
  bool likedByMe;
  final int repliesCount;
  final bool isRemoved;
  final DateTime? createdAtUtc;

  bool get isReply => parentCommentId != null;

  factory CommentDto.fromJson(Map<String, dynamic> j) => CommentDto(
        id: _i(j['id']),
        postId: _i(j['postId']),
        parentCommentId:
            j['parentCommentId'] == null ? null : _i(j['parentCommentId']),
        authorId: _i(j['authorId']),
        authorUsername: j['authorUsername']?.toString() ?? '',
        authorDisplayName: j['authorDisplayName']?.toString(),
        authorAvatarUrl: j['authorAvatarUrl']?.toString(),
        authorIsBlueTick: j['authorIsBlueTick'] == true,
        text: j['text']?.toString() ?? '',
        likeCount: _i(j['likeCount']),
        likedByMe: j['likedByMe'] == true,
        repliesCount: _i(j['repliesCount']),
        isRemoved: j['isRemoved'] == true,
        createdAtUtc: _d(j['createdAtUtc']),
      );
}

class CommentListResult {
  const CommentListResult({this.items = const [], this.nextCursor});
  final List<CommentDto> items;
  final String? nextCursor;

  factory CommentListResult.fromJson(Map<String, dynamic> j) => CommentListResult(
        items: (j['items'] as List?)
                ?.map((e) => CommentDto.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
        nextCursor: j['nextCursor']?.toString(),
      );
}

/// `{ liked, count }` from a like toggle.
class LikeResult {
  const LikeResult(this.liked, this.count);
  final bool liked;
  final int count;
  factory LikeResult.fromJson(Map<String, dynamic> j) =>
      LikeResult(j['liked'] == true, _i(j['count']));
}

/// `{ trueVotes, falseVotes, myVote }` from a Fake-vs-Real vote.
class VoteResult {
  const VoteResult(this.trueVotes, this.falseVotes, this.myVote);
  final int trueVotes;
  final int falseVotes;
  final bool? myVote;
  factory VoteResult.fromJson(Map<String, dynamic> j) => VoteResult(
        _i(j['trueVotes']),
        _i(j['falseVotes']),
        j['myVote'] is bool ? j['myVote'] as bool : null,
      );
}
