using TrueCapture.Modules.Users.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Users.Services;

public interface IAdminUsersService
{
    Task<Result<AdminUserListResult>> ListAsync(AdminUserListQuery query, CancellationToken ct = default);
}
