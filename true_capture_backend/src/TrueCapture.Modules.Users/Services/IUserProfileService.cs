using TrueCapture.Modules.Users.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Users.Services;

/// <summary>
/// An uploaded avatar file, decoupled from ASP.NET's <c>IFormFile</c> so the
/// service layer stays controller-agnostic and unit-testable.
/// </summary>
public sealed record AvatarUpload(
    Stream Content,
    string FileName,
    string ContentType,
    long   Length);

/// <summary>
/// Profile read/update + avatar management for a single user. The methods take an
/// explicit <c>userId</c> so they serve both the self endpoints (current user) and
/// the admin endpoints (any user) without duplication.
/// </summary>
public interface IUserProfileService
{
    Task<Result<UserProfileResponse>> GetAsync(long userId, CancellationToken ct = default);

    Task<Result<UserProfileResponse>> UpdateAsync(long userId, UpdateProfileRequest req, CancellationToken ct = default);

    Task<Result<UserProfileResponse>> SetAvatarAsync(long userId, AvatarUpload upload, CancellationToken ct = default);

    Task<Result<UserProfileResponse>> RemoveAvatarAsync(long userId, CancellationToken ct = default);
}
