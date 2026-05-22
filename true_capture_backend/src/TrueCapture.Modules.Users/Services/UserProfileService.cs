using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Users.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Users.Services;

public sealed class UserProfileService(
    AppDbContext db,
    IBaseService baseService,
    IFileStorage fileStorage) : IUserProfileService
{
    private const int MaxDisplayNameLength = 80;
    private const int MaxBioLength         = 500;

    public Task<Result<UserProfileResponse>> GetAsync(long userId, CancellationToken ct)
        => baseService.ExecuteAsync("UserProfile.Get", async () =>
        {
            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
            return user is null
                ? Result<UserProfileResponse>.NotFound("User not found.")
                : Result<UserProfileResponse>.Success(await BuildAsync(user, ct));
        }, ct);

    public Task<Result<UserProfileResponse>> UpdateAsync(long userId, UpdateProfileRequest req, CancellationToken ct)
        => baseService.ExecuteAsync("UserProfile.Update", async () =>
        {
            var displayName = Normalize(req.DisplayName);
            var bio         = Normalize(req.Bio);

            if (displayName is { Length: > MaxDisplayNameLength })
                return Result<UserProfileResponse>.Validation(
                    [$"Display name must be {MaxDisplayNameLength} characters or fewer."]);
            if (bio is { Length: > MaxBioLength })
                return Result<UserProfileResponse>.Validation(
                    [$"Bio must be {MaxBioLength} characters or fewer."]);

            var (genderOk, gender) = ParseEnum<Gender>(req.Gender);
            if (!genderOk)
                return Result<UserProfileResponse>.Validation(
                    ["Gender must be one of: male, female, other."]);

            var (accountOk, accountType) = ParseEnum<AccountType>(req.AccountType);
            if (!accountOk)
                return Result<UserProfileResponse>.Validation(
                    ["Account type must be one of: public, private."]);

            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Result<UserProfileResponse>.NotFound("User not found.");

            user.DisplayName = displayName;
            user.Bio         = bio;
            user.Gender      = gender;                       // null clears it
            if (accountType is not null)                     // null leaves it unchanged
                user.AccountType = accountType.Value;
            await db.SaveChangesAsync(ct);

            return Result<UserProfileResponse>.Success(await BuildAsync(user, ct));
        }, ct, useTransaction: true);

    public Task<Result<UserProfileResponse>> SetAvatarAsync(long userId, AvatarUpload upload, CancellationToken ct)
        => baseService.ExecuteAsync("UserProfile.SetAvatar", async () =>
        {
            var errors = AvatarRules.Validate(upload.ContentType, upload.Length);
            if (errors.Count > 0)
                return Result<UserProfileResponse>.Validation(errors);

            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Result<UserProfileResponse>.NotFound("User not found.");

            var previousAvatar = user.AvatarUrl;

            var stored = await fileStorage.SaveAsync(
                upload.Content, upload.FileName, upload.ContentType, AvatarRules.Folder, ct);

            user.AvatarUrl = stored.Url;
            await db.SaveChangesAsync(ct);

            // Best-effort cleanup of the replaced file — never fail the request on this.
            if (!string.IsNullOrWhiteSpace(previousAvatar) && previousAvatar != stored.Url)
                await fileStorage.DeleteAsync(previousAvatar, ct);

            return Result<UserProfileResponse>.Success(await BuildAsync(user, ct));
        }, ct, useTransaction: true);

    public Task<Result<UserProfileResponse>> RemoveAvatarAsync(long userId, CancellationToken ct)
        => baseService.ExecuteAsync("UserProfile.RemoveAvatar", async () =>
        {
            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Result<UserProfileResponse>.NotFound("User not found.");

            var previousAvatar = user.AvatarUrl;
            user.AvatarUrl = null;
            await db.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(previousAvatar))
                await fileStorage.DeleteAsync(previousAvatar, ct);

            return Result<UserProfileResponse>.Success(await BuildAsync(user, ct));
        }, ct, useTransaction: true);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Parses a case-insensitive enum name. Blank input yields <c>(true, null)</c>;
    /// an unrecognised value yields <c>(false, null)</c> so the caller can 422.
    /// </summary>
    private static (bool ok, TEnum? value) ParseEnum<TEnum>(string? raw)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(raw)) return (true, null);
        return Enum.TryParse<TEnum>(raw.Trim(), ignoreCase: true, out var parsed)
               && Enum.IsDefined(parsed)
            ? (true, parsed)
            : (false, null);
    }

    /// <summary>
    /// Builds the response from the user's denormalized social counters
    /// (maintained transactionally by <c>SocialService</c> / <c>PostService</c>).
    /// </summary>
    private Task<UserProfileResponse> BuildAsync(User u, CancellationToken ct)
    {
        return Task.FromResult(new UserProfileResponse(
            u.Id, u.Email, u.Username, u.DisplayName, u.AvatarUrl, u.Bio,
            JoinedAtUtc:    u.CreatedAtUtc,
            FollowersCount: u.FollowersCount,
            FollowingCount: u.FollowingCount,
            PostsCount:     u.PostsCount,
            IsSuspended:    !u.IsActive,
            IsBlueTick:     u.IsVerified,
            AccountType:    u.AccountType.ToString().ToLowerInvariant(),
            Gender:         u.Gender?.ToString().ToLowerInvariant(),
            EmailVerified:  u.EmailVerified,
            IsAdmin:        u.IsAdmin));
    }
}
