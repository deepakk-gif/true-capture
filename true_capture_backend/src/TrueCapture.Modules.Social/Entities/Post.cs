using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>Which feed a post belongs to.</summary>
public enum PostType
{
    Normal     = 1,   // everyday post — Home tab
    FakeVsReal = 2,   // credibility post — Fake vs Real tab; needs reference links + caption
}

/// <summary>Derived from a post's media composition.</summary>
public enum PostKind
{
    Photo    = 1,   // exactly one photo
    Carousel = 2,   // two or more photos
    Video    = 3,   // one or more videos
}

/// <summary>Moderation state. Only <see cref="Live"/> posts appear in public feeds.</summary>
public enum PostStatus
{
    Live          = 1,
    PendingReview = 2,
    Removed       = 3,
}

/// <summary>A post authored by a user. Media is attached via <see cref="PostMedia"/>.</summary>
public class Post : BaseEntity
{
    public long       AuthorId      { get; set; }
    public PostType   Type          { get; set; } = PostType.Normal;
    public PostKind   Kind          { get; set; } = PostKind.Photo;
    public bool       IsAdminPost   { get; set; }
    public PostStatus Status        { get; set; } = PostStatus.Live;
    public string?    RemovalReason { get; set; }

    public string?    Caption       { get; set; }   // mandatory for FakeVsReal, optional for Normal
    public string     CoverUrl      { get; set; } = "";  // thumbnail of the first media — grid / notifications
    public string     ShareId       { get; set; } = "";  // public slug for domain/post/{shareId}

    // Denormalized counters — maintained transactionally on each engagement write.
    public int        ViewCount       { get; set; }
    public int        LikesCount      { get; set; }
    public int        CommentsCount   { get; set; }
    public int        SharesCount     { get; set; }
    public int        TrueVotesCount  { get; set; }   // Fake vs Real: "real" votes
    public int        FalseVotesCount { get; set; }   // Fake vs Real: "fake" votes

    public User                Author     { get; set; } = null!;
    public List<PostMedia>     Media      { get; set; } = [];
    public List<PostReference> References { get; set; } = [];
}
