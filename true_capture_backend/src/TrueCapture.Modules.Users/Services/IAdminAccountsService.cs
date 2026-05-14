using TrueCapture.Modules.Users.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Users.Services;

public interface IAdminAccountsService
{
    /// <summary>
    /// Lists every <see cref="TrueCapture.Modules.Identity.Entities.Permission"/>
    /// in the system — fuel for the super-admin permission-picker UI.
    /// </summary>
    Task<Result<IReadOnlyList<PermissionDescriptor>>> ListPermissionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new admin user, attaches the <c>admin</c> role, and adds the
    /// requested permission codes via per-user <c>UserPermission</c> rows.
    /// </summary>
    Task<Result<CreatedAdminResponse>> CreateAdminAsync(CreateAdminRequest req, CancellationToken ct = default);
}
