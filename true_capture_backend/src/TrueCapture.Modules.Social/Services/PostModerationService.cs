using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Social.Services;

public sealed class PostModerationService(
    AppDbContext         db,
    IBaseService         baseService,
    INotificationService notifications,
    IFcmSender           fcm) : IPostModerationService
{
    public Task<Result<bool>> ReportPostAsync(
        long reporterId, long postId, string reason, string? otherText, CancellationToken ct)
        => baseService.ExecuteAsync("Moderation.Report", async () =>
        {
            if (!TryParseReason(reason, out var parsed))
                return Result<bool>.Validation(["Unknown report reason."]);

            var trimmed = otherText?.Trim();
            if (parsed == ReportReason.Other && string.IsNullOrWhiteSpace(trimmed))
                return Result<bool>.Validation(["Please describe the issue for an 'Other' report."]);

            var post = await db.Set<Post>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null) return Result<bool>.NotFound("Post not found.");

            db.Set<PostReport>().Add(new PostReport
            {
                PostId     = postId,
                ReporterId = reporterId,
                Reason     = parsed,
                OtherText  = string.IsNullOrWhiteSpace(trimmed) ? null : trimmed,
                Status     = ReportStatus.Open,
            });
            await db.SaveChangesAsync(ct);

            // Best-effort alert to the admin moderation channel.
            try
            {
                await fcm.SendToTopicAsync(CreatorScoreService.AdminTopic, new NotificationPayload(
                    "Post reported",
                    $"A post was reported ({ReasonName(parsed)}).",
                    new Dictionary<string, string> { ["type"] = "post_report", ["postId"] = postId.ToString() }),
                    ct);
            }
            catch { /* best-effort */ }

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<PostReportListResult>> ListReportsAsync(
        string? status, string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Moderation.ListReports", async () =>
        {
            var q = db.Set<PostReport>().AsNoTracking();
            if (status?.Trim().ToLowerInvariant() == "open")     q = q.Where(r => r.Status == ReportStatus.Open);
            if (status?.Trim().ToLowerInvariant() == "resolved") q = q.Where(r => r.Status == ReportStatus.Resolved);
            if (Paging.DecodeCursor(cursor) is long c) q = q.Where(r => r.Id < c);

            var rows = await q.OrderByDescending(r => r.Id).Take(Paging.PageSize + 1)
                .Join(db.Set<Post>(), r => r.PostId, p => p.Id, (r, p) => new { r, p.CoverUrl })
                .Join(db.Set<User>(), x => x.r.ReporterId, u => u.Id,
                    (x, u) => new { x.r, x.CoverUrl, ReporterUsername = u.Username })
                .ToListAsync(ct);

            string? next = null;
            if (rows.Count > Paging.PageSize)
            {
                next = rows[Paging.PageSize - 1].r.Id.ToString();
                rows.RemoveAt(rows.Count - 1);
            }

            var items = rows.Select(x => new PostReportDto(
                x.r.Id, x.r.PostId, x.CoverUrl, x.r.ReporterId, x.ReporterUsername,
                ReasonName(x.r.Reason), x.r.OtherText,
                x.r.Status == ReportStatus.Resolved ? "resolved" : "open",
                x.r.Resolution, x.r.CreatedAtUtc)).ToList();

            return Result<PostReportListResult>.Success(new PostReportListResult(items, next));
        }, ct);

    public Task<Result<bool>> ResolveReportAsync(
        long adminId, long reportId, string action, string? reason, CancellationToken ct)
        => baseService.ExecuteAsync("Moderation.ResolveReport", async () =>
        {
            var report = await db.Set<PostReport>().FirstOrDefaultAsync(r => r.Id == reportId, ct);
            if (report is null) return Result<bool>.NotFound("Report not found.");
            if (report.Status == ReportStatus.Resolved)
                return Result<bool>.Validation(["This report is already resolved."]);

            var post = await db.Set<Post>().FirstOrDefaultAsync(p => p.Id == report.PostId, ct);

            string resolution;
            switch (action?.Trim().ToLowerInvariant())
            {
                case "dismiss":
                    resolution = "Dismissed — no action taken.";
                    break;

                case "removepost":
                    if (post is null) return Result<bool>.NotFound("Post not found.");
                    post.Status        = PostStatus.Removed;
                    post.RemovalReason = reason?.Trim();
                    resolution = "Post removed.";
                    if (post.AuthorId != adminId)
                        await notifications.EmitAsync(post.AuthorId, NotificationType.AdminNotice,
                            text: reason?.Trim() ?? "One of your posts was removed for violating our guidelines.",
                            ct: ct);
                    break;

                case "withholdaccount":
                    if (post is null) return Result<bool>.NotFound("Post not found.");
                    await db.Set<User>().Where(u => u.Id == post.AuthorId)
                        .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false), ct);
                    resolution = "Author account withheld.";
                    await notifications.EmitAsync(post.AuthorId, NotificationType.AccountSuspended,
                        text: reason?.Trim() ?? "Your account has been withheld pending review.", ct: ct);
                    break;

                case "sendnotice":
                    if (post is null) return Result<bool>.NotFound("Post not found.");
                    if (string.IsNullOrWhiteSpace(reason))
                        return Result<bool>.Validation(["A notice message is required."]);
                    await notifications.EmitAsync(post.AuthorId, NotificationType.AdminNotice,
                        text: reason.Trim(), ct: ct);
                    resolution = "Notice sent to the author.";
                    break;

                default:
                    return Result<bool>.Validation(
                        ["Action must be 'dismiss', 'removePost', 'withholdAccount', or 'sendNotice'."]);
            }

            report.Status        = ReportStatus.Resolved;
            report.Resolution    = resolution;
            report.ResolvedById  = adminId;
            report.ResolvedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<FvrCandidateListResult>> ListFvrCandidatesAsync(string? cursor, CancellationToken ct)
        => baseService.ExecuteAsync("Moderation.ListFvrCandidates", async () =>
        {
            // Users the milestone has flagged, highest score first.
            var q = db.Set<User>().AsNoTracking()
                .Where(u => u.FvrCandidateNotifiedAtUtc != null && !u.IsAdmin);
            if (Paging.DecodeCursor(cursor) is long c) q = q.Where(u => u.Id < c);

            var rows = await q.OrderByDescending(u => u.CreatorScore).ThenByDescending(u => u.Id)
                .Take(Paging.PageSize + 1).ToListAsync(ct);

            string? next = null;
            if (rows.Count > Paging.PageSize)
            {
                next = rows[Paging.PageSize - 1].Id.ToString();
                rows.RemoveAt(rows.Count - 1);
            }

            var items = rows.Select(u => new FvrCandidateDto(
                u.Id, u.Username, u.DisplayName, u.AvatarUrl,
                u.FollowersCount, u.CreatorScore, u.CanPostFakeVsReal, u.CreatedAtUtc)).ToList();

            return Result<FvrCandidateListResult>.Success(new FvrCandidateListResult(items, next));
        }, ct);

    public Task<Result<bool>> GrantFvrAccessAsync(long adminId, long userId, bool granted, CancellationToken ct)
        => baseService.ExecuteAsync("Moderation.GrantFvrAccess", async () =>
        {
            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Result<bool>.NotFound("User not found.");

            user.CanPostFakeVsReal = granted;
            user.IsVerified        = granted;   // the blue tick tracks Fake-vs-Real access
            await db.SaveChangesAsync(ct);

            await notifications.EmitAsync(userId, NotificationType.AdminNotice,
                text: granted
                    ? "You can now post Fake vs Real content. Your account is verified."
                    : "Your Fake vs Real upload access has been revoked.",
                ct: ct);
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    private static bool TryParseReason(string? raw, out ReportReason reason)
    {
        switch (raw?.Trim().ToLowerInvariant())
        {
            case "spam":             reason = ReportReason.Spam; return true;
            case "misinformation":   reason = ReportReason.Misinformation; return true;
            case "hateorharassment": reason = ReportReason.HateOrHarassment; return true;
            case "nudityorsexual":   reason = ReportReason.NudityOrSexual; return true;
            case "violenceordanger": reason = ReportReason.ViolenceOrDanger; return true;
            case "other":            reason = ReportReason.Other; return true;
            default:                 reason = ReportReason.Other; return false;
        }
    }

    private static string ReasonName(ReportReason r) => r switch
    {
        ReportReason.Spam             => "spam",
        ReportReason.Misinformation   => "misinformation",
        ReportReason.HateOrHarassment => "hateOrHarassment",
        ReportReason.NudityOrSexual   => "nudityOrSexual",
        ReportReason.ViolenceOrDanger => "violenceOrDanger",
        _                             => "other",
    };
}
