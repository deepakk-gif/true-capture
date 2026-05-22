using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Modules.Social.Services;
using TrueCapture.Modules.Users.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Users.Services;

public sealed class AdminUsersService(
    AppDbContext         db,
    IBaseService         baseService,
    INotificationService notifications) : IAdminUsersService
{
    public Task<Result<AdminUserListResult>> ListAsync(AdminUserListQuery q, CancellationToken ct)
        => baseService.ExecuteAsync("AdminUsers.List", async () =>
        {
            var limit = q.NormalizedLimit();
            var query = db.Set<User>().AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var term = q.Search.Trim().ToLowerInvariant();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(term) ||
                    u.Username.ToLower().Contains(term));
            }
            if (q.IsActive   is bool a) query = query.Where(u => u.IsActive   == a);
            if (q.IsAdmin    is bool b) query = query.Where(u => u.IsAdmin    == b);
            if (q.IsVerified is bool c) query = query.Where(u => u.IsVerified == c);
            if (q.HasGoogle  is bool g) query = g
                ? query.Where(u => u.GoogleSubject != null && u.GoogleSubject != "")
                : query.Where(u => u.GoogleSubject == null || u.GoogleSubject == "");

            var total = await query.CountAsync(ct);

            // Cursor is `<createdAtUtcTicks>:<id>` base64 — paginates by (CreatedAtUtc DESC, Id DESC)
            // so newest-first lists stay stable across page boundaries.
            if (TryDecodeCursor(q.Cursor, out var cursorTicks, out var cursorId))
            {
                var cursorDate = new DateTime(cursorTicks, DateTimeKind.Utc);
                query = query.Where(u =>
                    u.CreatedAtUtc < cursorDate ||
                    (u.CreatedAtUtc == cursorDate && u.Id < cursorId));
            }

            var rows = await query
                .OrderByDescending(u => u.CreatedAtUtc)
                .ThenByDescending(u => u.Id)
                .Take(limit + 1)
                .Select(u => new AdminUserListItem(
                    u.Id,
                    u.Email,
                    u.Username,
                    u.DisplayName,
                    u.AvatarUrl,
                    u.EmailVerified,
                    u.IsActive,
                    u.IsAdmin,
                    u.IsVerified,
                    u.GoogleSubject != null && u.GoogleSubject != "",
                    u.LastLoginAtUtc,
                    u.CreatedAtUtc))
                .ToListAsync(ct);

            string? nextCursor = null;
            if (rows.Count > limit)
            {
                var last = rows[limit - 1];   // last row of current page
                nextCursor = EncodeCursor(last.CreatedAtUtc.Ticks, last.Id);
                rows.RemoveAt(rows.Count - 1);
            }

            return Result<AdminUserListResult>.Success(
                new AdminUserListResult(rows, nextCursor, total));
        }, ct);

    public Task<Result<AdminUserDetail>> GetDetailAsync(long id, CancellationToken ct)
        => baseService.ExecuteAsync("AdminUsers.GetDetail", async () =>
        {
            var user = await db.Set<User>().AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, ct);
            return user is null
                ? Result<AdminUserDetail>.NotFound("User not found.")
                : Result<AdminUserDetail>.Success(await BuildDetailAsync(user, ct));
        }, ct);

    public Task<Result<AdminUserDetail>> UpdateAsync(long id, AdminUpdateUserRequest req, CancellationToken ct)
        => baseService.ExecuteAsync("AdminUsers.Update", async () =>
        {
            var displayName = Normalize(req.DisplayName);
            var bio         = Normalize(req.Bio);

            if (displayName is { Length: > 80 })
                return Result<AdminUserDetail>.Validation(["Display name must be 80 characters or fewer."]);
            if (bio is { Length: > 500 })
                return Result<AdminUserDetail>.Validation(["Bio must be 500 characters or fewer."]);

            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null) return Result<AdminUserDetail>.NotFound("User not found.");

            user.DisplayName = displayName;
            user.Bio         = bio;
            await db.SaveChangesAsync(ct);

            return Result<AdminUserDetail>.Success(await BuildDetailAsync(user, ct));
        }, ct, useTransaction: true);

    public Task<Result<AdminUserDetail>> SetStatusAsync(long id, bool isActive, CancellationToken ct)
        => baseService.ExecuteAsync("AdminUsers.SetStatus", async () =>
        {
            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null) return Result<AdminUserDetail>.NotFound("User not found.");

            user.IsActive = isActive;
            if (!isActive)
                await notifications.EmitAsync(user.Id, NotificationType.AccountSuspended,
                    text: "Your account has been suspended by an administrator.", ct: ct);
            await db.SaveChangesAsync(ct);

            return Result<AdminUserDetail>.Success(await BuildDetailAsync(user, ct));
        }, ct, useTransaction: true);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Builds the admin detail with live follow/post counts (privacy is not applied — admin sees all).</summary>
    private async Task<AdminUserDetail> BuildDetailAsync(User u, CancellationToken ct)
    {
        var followers = await db.Set<Follow>()
            .CountAsync(f => f.FolloweeId == u.Id && f.Status == FollowStatus.Accepted, ct);
        var following = await db.Set<Follow>()
            .CountAsync(f => f.FollowerId == u.Id && f.Status == FollowStatus.Accepted, ct);
        var posts = await db.Set<Post>().CountAsync(p => p.AuthorId == u.Id, ct);

        return new AdminUserDetail(
            u.Id, u.Email, u.Username, u.DisplayName, u.AvatarUrl, u.Bio,
            u.EmailVerified, u.IsActive, u.IsAdmin, u.IsVerified,
            u.GoogleSubject != null && u.GoogleSubject != "",
            u.LastLoginAtUtc, u.CreatedAtUtc,
            AccountType:    u.AccountType.ToString().ToLowerInvariant(),
            FollowersCount: followers,
            FollowingCount: following,
            PostsCount:     posts);
    }

    private static string EncodeCursor(long ticks, long id)
    {
        var raw = $"{ticks}:{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private static bool TryDecodeCursor(string? cursor, out long ticks, out long id)
    {
        ticks = 0; id = 0;
        if (string.IsNullOrWhiteSpace(cursor)) return false;
        try
        {
            var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(':');
            if (parts.Length != 2) return false;
            return long.TryParse(parts[0], out ticks) && long.TryParse(parts[1], out id);
        }
        catch
        {
            return false;
        }
    }
}
