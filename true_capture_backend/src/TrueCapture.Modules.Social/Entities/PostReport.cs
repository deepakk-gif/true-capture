using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Social.Entities;

/// <summary>Why a post was reported. <see cref="Other"/> carries free text.</summary>
public enum ReportReason
{
    Spam            = 1,
    Misinformation  = 2,
    HateOrHarassment = 3,
    NudityOrSexual  = 4,
    ViolenceOrDanger = 5,
    Other           = 99,
}

/// <summary>Lifecycle of a report from the admin's queue.</summary>
public enum ReportStatus
{
    Open     = 1,
    Resolved = 2,
}

/// <summary>A user's report against a post. Notifies the admin moderation queue.</summary>
public class PostReport : BaseEntity
{
    public long         PostId       { get; set; }
    public long         ReporterId   { get; set; }
    public ReportReason Reason       { get; set; }
    public string?      OtherText    { get; set; }   // required when Reason = Other
    public ReportStatus Status       { get; set; } = ReportStatus.Open;
    public string?      Resolution   { get; set; }   // action taken, e.g. "post removed"
    public long?        ResolvedById { get; set; }
    public DateTime?    ResolvedAtUtc { get; set; }

    public Post Post     { get; set; } = null!;
    public User Reporter { get; set; } = null!;
}
