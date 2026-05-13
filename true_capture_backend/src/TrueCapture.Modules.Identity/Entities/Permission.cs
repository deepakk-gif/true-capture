using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Identity.Entities;

public class Permission : BaseEntity
{
    public string  Code        { get; set; } = "";   // e.g. "Posts.Create"
    public string  Module      { get; set; } = "";   // e.g. "Posts"
    public string? Description { get; set; }

    public List<RolePermission> RolePermissions { get; set; } = [];
}
