using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Identity.Entities;

public class RolePermission : BaseEntity
{
    public long RoleId       { get; set; }
    public long PermissionId { get; set; }

    public Role       Role       { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
