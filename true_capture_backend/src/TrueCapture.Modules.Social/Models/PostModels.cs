namespace TrueCapture.Modules.Social.Models;

// ─────────────────────────────── Media pipeline ───────────────────────────────

/// <summary>Body of `POST /api/media/uploads` — requests a signed upload slot.</summary>
public sealed record RequestUploadDto(string MimeType, long ByteSize, string Kind);

/// <summary>Response of `POST /api/media/uploads` — where the client PUTs the bytes.</summary>
public sealed record UploadTicket(long UploadId, string PutUrl, DateTime ExpiresAtUtc);

/// <summary>Body of `POST /api/media/finalize` — confirms the bytes were uploaded.</summary>
public sealed record FinalizeUploadDto(long UploadId, string? CaptureMetadata);

/// <summary>A media asset as returned to clients.</summary>
public sealed record MediaAssetDto(
    long    Id,
    string  Kind,            // "photo" | "video"
    string  Status,          // "pending" | "ready" | "failed"
    string  Url,
    string? ThumbnailUrl,
    int?    DurationSeconds,
    int?    Width,
    int?    Height);

// ─────────────────────────────── Post create ──────────────────────────────────

/// <summary>Body of `POST /api/posts` — create a Normal or Fake-vs-Real post.</summary>
public sealed record CreatePostRequest(
    string        Type,            // "normal" | "fakeVsReal"
    List<long>    MediaAssetIds,
    string?       Caption,
    List<string>? References);     // Fake-vs-Real only — ≥ 1 required

/// <summary>Body of `POST /api/admin/posts` — admin publishes a Fake-vs-Real post.</summary>
public sealed record AdminCreatePostRequest(
    List<long>    MediaAssetIds,
    string?       Caption,
    List<string>? References);

// ─────────────────────────────── Post read DTOs ───────────────────────────────

public sealed record PostAuthorDto(
    long    Id,
    string  Username,
    string? DisplayName,
    string? AvatarUrl,
    bool    IsBlueTick,
    string  FollowState);        // viewer -> author: "none" | "following" | "requested"

public sealed record PostMediaDto(
    long    Id,
    string  Kind,                // "photo" | "video"
    string  Url,
    string? ThumbnailUrl,
    int?    DurationSeconds,
    int     Position);

/// <summary>The full post object — backs both the feed list and the detail screen.</summary>
public sealed record PostDto(
    long          Id,
    string        Type,          // "normal" | "fakeVsReal"
    string        Kind,          // "photo" | "carousel" | "video"
    string        Status,        // "live" | "pendingReview" | "removed"
    bool          IsAdminPost,
    string        ShareId,
    PostAuthorDto Author,
    string?       Caption,
    IReadOnlyList<PostMediaDto> Media,
    IReadOnlyList<string>       References,
    DateTime      CreatedAtUtc,
    int           ViewCount,
    int           LikeCount,
    int           CommentCount,
    int           ShareCount,
    int           TrueVotes,
    int           FalseVotes,
    bool          LikedByMe,
    bool          SavedByMe,
    bool?         MyVote);       // Fake-vs-Real: null = not voted, true = real, false = fake

public sealed record FeedResult(IReadOnlyList<PostDto> Items, string? NextCursor);

// ─────────────────────────────── Engagement DTOs ──────────────────────────────

public sealed record LikeResult(bool Liked, int Count);
public sealed record SaveResult(bool Saved);
public sealed record ShareResult(string Url);
public sealed record VoteResult(int TrueVotes, int FalseVotes, bool? MyVote);

/// <summary>Body of `POST /api/posts/{id}/vote`.</summary>
public sealed record VoteRequest(bool Value);

/// <summary>Body of `POST /api/posts/{id}/report`.</summary>
public sealed record ReportPostRequest(string Reason, string? OtherText);

// ─────────────────────────────── Admin DTOs ───────────────────────────────────

public sealed record PostReportDto(
    long     Id,
    long     PostId,
    string   PostCoverUrl,
    long     ReporterId,
    string   ReporterUsername,
    string   Reason,
    string?  OtherText,
    string   Status,
    string?  Resolution,
    DateTime CreatedAtUtc);

public sealed record PostReportListResult(IReadOnlyList<PostReportDto> Items, string? NextCursor);

/// <summary>Body of `PATCH /api/admin/post-reports/{id}` — `dismiss | removePost |
/// withholdAccount | sendNotice`.</summary>
public sealed record ResolveReportRequest(string Action, string? Reason);

/// <summary>Body of `POST /api/admin/users/{id}/fake-vs-real-access`.</summary>
public sealed record GrantFvrAccessRequest(bool Granted);

/// <summary>A user surfaced as a Fake-vs-Real access candidate.</summary>
public sealed record FvrCandidateDto(
    long     Id,
    string   Username,
    string?  DisplayName,
    string?  AvatarUrl,
    int      FollowersCount,
    int      CreatorScore,
    bool     CanPostFakeVsReal,
    DateTime JoinedAtUtc);

public sealed record FvrCandidateListResult(IReadOnlyList<FvrCandidateDto> Items, string? NextCursor);
