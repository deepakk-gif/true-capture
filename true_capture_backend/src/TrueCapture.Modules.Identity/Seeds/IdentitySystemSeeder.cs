using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Seeds;

[DataSeederOrder(10)]
public sealed class IdentitySystemSeeder(AppDbContext db) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct)
    {
        var seedPerms = new (string Code, string Module, string Description)[]
        {
            ("Users.View",      "Users",  "View user profiles"),
            ("Users.Manage",    "Users",  "Edit / suspend users"),
            ("Posts.View",      "Posts",  "View posts"),
            ("Posts.Create",    "Posts",  "Create posts"),
            ("Posts.Moderate",  "Posts",  "Moderate posts"),
            ("FakeVsReal.Publish", "FakeVsReal", "Publish admin Fake-vs-Real posts"),
            ("Cms.Manage",      "Cms",    "Manage CMS pages"),
        };

        foreach (var (code, module, desc) in seedPerms)
        {
            if (!await db.Set<Permission>().AnyAsync(p => p.Code == code, ct))
                db.Set<Permission>().Add(new Permission { Code = code, Module = module, Description = desc });
        }

        var seedRoles = new (string Code, string Name, string Description, bool IsSystem)[]
        {
            ("admin", "Administrator", "Platform administrator", true),
            ("user",  "User",          "Standard end user",      true),
        };

        foreach (var (code, name, desc, isSys) in seedRoles)
        {
            if (!await db.Set<Role>().AnyAsync(r => r.Code == code, ct))
                db.Set<Role>().Add(new Role { Code = code, Name = name, Description = desc, IsSystem = isSys });
        }

        await db.SaveChangesAsync(ct);

        // Grant every permission to the admin role
        var adminRole = await db.Set<Role>().FirstAsync(r => r.Code == "admin", ct);
        var allPerms  = await db.Set<Permission>().ToListAsync(ct);
        foreach (var p in allPerms)
        {
            var exists = await db.Set<RolePermission>()
                .AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == p.Id, ct);
            if (!exists)
                db.Set<RolePermission>().Add(new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id });
        }
        await db.SaveChangesAsync(ct);
    }
}
