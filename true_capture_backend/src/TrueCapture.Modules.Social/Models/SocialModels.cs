using TrueCapture.Modules.Social.Entities;

namespace TrueCapture.Modules.Social.Models;

/// <summary>follow-state of the viewer towards another user: "none" | "following" | "requested".</summary>
public static class FollowStates
{
    public const string None      = "none";
    public const string Following = "following";
    public const string Requested = "requested";
}

/// <summary>A row in user-search results.</summary>
public sealed record UserSearchItem(
    long                  Id,
    string                Username,
    string?               DisplayName,
    string?               AvatarUrl,
    bool                  IsBlueTick,
    IReadOnlyList<string> MutualFollowers,      // up to 2 display names/usernames
    int                   MutualFollowersCount, // total mutual count
    string                FollowState);

public sealed record UserSearchResult(IReadOnlyList<UserSearchItem> Items);

/// <summary>Another user's profile as seen by the viewer.</summary>
public sealed record UserProfileView(
    long      Id,
    string    Username,
    string?   DisplayName,
    string?   AvatarUrl,
    string?   Bio,
    bool      IsBlueTick,
    string    AccountType,      // "public" | "private"
    DateTime  JoinedAtUtc,
    int       FollowersCount,
    int       FollowingCount,
    int       PostsCount,
    string    FollowState,      // viewer -> this user
    bool      FollowsMe,        // this user -> viewer (accepted)
    bool      IsMe,
    bool      CanViewContent);  // posts / followers / following visible to the viewer

/// <summary>Returned by follow / unfollow / accept / reject — the resulting follow-state.</summary>
public sealed record FollowActionResult(string FollowState);

/// <summary>A row in a followers / following / follow-requests list.</summary>
public sealed record FollowUserItem(
    long    Id,
    string  Username,
    string? DisplayName,
    string? AvatarUrl,
    bool    IsBlueTick,
    string  FollowState);   // viewer -> this user

public sealed record FollowListResult(IReadOnlyList<FollowUserItem> Items, string? NextCursor);

/// <summary>A post in a profile grid — thumbnail only.</summary>
public sealed record PostItem(
    long     Id,
    long     AuthorId,
    string   Type,           // "normal" | "fakeVsReal"
    string   Kind,           // "photo" | "carousel" | "video"
    string   CoverUrl,
    string?  Caption,
    DateTime CreatedAtUtc)
{
    public static PostItem From(Post p) => new(
        p.Id, p.AuthorId,
        p.Type == PostType.FakeVsReal ? "fakeVsReal" : "normal",
        p.Kind switch { PostKind.Carousel => "carousel", PostKind.Video => "video", _ => "photo" },
        p.CoverUrl, p.Caption, p.CreatedAtUtc);
}

public sealed record PostListResult(IReadOnlyList<PostItem> Items, string? NextCursor);
