using TrueCapture.Modules.Users.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Users.Services;

public interface IAdminUsersService
{
    Task<Result<AdminUserListResult>> ListAsync(AdminUserListQuery query, CancellationToken ct = default);

    /// <summary>Full user record for the admin detail page.</summary>
    Task<Result<AdminUserDetail>> GetDetailAsync(long id, CancellationToken ct = default);

    /// <summary>Admin edit of a user's display name + bio.</summary>
    Task<Result<AdminUserDetail>> UpdateAsync(long id, AdminUpdateUserRequest req, CancellationToken ct = default);

    /// <summary>Activate or suspend a user account.</summary>
    Task<Result<AdminUserDetail>> SetStatusAsync(long id, bool isActive, CancellationToken ct = default);
}
