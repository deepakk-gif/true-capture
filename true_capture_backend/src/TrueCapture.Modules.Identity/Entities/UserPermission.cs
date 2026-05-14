using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Identity.Entities;

/// <summary>
/// Per-user permission grant. Supplements role-based permissions: a user's
/// effective set is the union of (role → role-permissions) and these rows.
/// Used by the super-admin "Create admin with permissions" flow to grant
/// fine-grained powers on a per-admin basis.
/// </summary>
public class UserPermission : BaseEntity
{
    public long UserId       { get; set; }
    public long PermissionId { get; set; }

    public User       User       { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
