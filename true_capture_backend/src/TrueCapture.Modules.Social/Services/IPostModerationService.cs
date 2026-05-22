using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

/// <summary>
/// Post reporting (user side) and the admin moderation queue: resolving reports and
/// granting Fake-vs-Real upload access.
/// </summary>
public interface IPostModerationService
{
    /// <summary>A user reports a post — opens a moderation queue entry.</summary>
    Task<Result<bool>> ReportPostAsync(
        long reporterId, long postId, string reason, string? otherText, CancellationToken ct = default);

    /// <summary>Admin: lists reports, optionally filtered by status ("open" | "resolved").</summary>
    Task<Result<PostReportListResult>> ListReportsAsync(
        string? status, string? cursor, CancellationToken ct = default);

    /// <summary>Admin: resolves a report — `dismiss | removePost | withholdAccount | sendNotice`.</summary>
    Task<Result<bool>> ResolveReportAsync(
        long adminId, long reportId, string action, string? reason, CancellationToken ct = default);

    /// <summary>Admin: lists users flagged as Fake-vs-Real access candidates.</summary>
    Task<Result<FvrCandidateListResult>> ListFvrCandidatesAsync(
        string? cursor, CancellationToken ct = default);

    /// <summary>Admin: grants or revokes a user's Fake-vs-Real upload access (and blue tick).</summary>
    Task<Result<bool>> GrantFvrAccessAsync(
        long adminId, long userId, bool granted, CancellationToken ct = default);
}
