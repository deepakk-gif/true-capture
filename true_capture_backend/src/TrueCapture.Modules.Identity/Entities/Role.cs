using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Identity.Entities;

public class Role : BaseEntity
{
    public string  Code        { get; set; } = "";
    public string  Name        { get; set; } = "";
    public string? Description { get; set; }
    public bool    IsSystem    { get; set; }

    public List<UserRole>       UserRoles       { get; set; } = [];
    public List<RolePermission> RolePermissions { get; set; } = [];
}
