using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Identity.Seeds;

[DataSeederOrder(10)]
public sealed class IdentitySystemSeeder(AppDbContext db) : IDataSeeder
{
    // Permissions that ONLY the super-admin role holds. Standard admins
    // never inherit these from the `admin` role and can only obtain them
    // through an explicit per-user grant by another super-admin.
    private static readonly string[] SuperAdminOnlyPermissions =
    {
        "Users.CreateAdmin",
        "Users.AssignPermissions",
    };

    public async Task SeedAsync(CancellationToken ct)
    {
        var seedPerms = new (string Code, string Module, string Description)[]
        {
            ("Users.View",            "Users",  "View user profiles"),
            ("Users.Manage",           "Users",  "Edit / suspend users"),
            ("Users.CreateAdmin",      "Users",  "Create new admin accounts (super-admin only)"),
            ("Users.AssignPermissions","Users",  "Grant / revoke per-user permissions (super-admin only)"),
            ("Posts.View",             "Posts",  "View posts"),
            ("Posts.Create",           "Posts",  "Create posts"),
            ("Posts.Moderate",         "Posts",  "Moderate posts"),
            ("FakeVsReal.Publish",     "FakeVsReal", "Publish admin Fake-vs-Real posts"),
            ("Cms.Manage",             "Cms",    "Manage CMS pages"),
        };

        foreach (var (code, module, desc) in seedPerms)
        {
            if (!await db.Set<Permission>().AnyAsync(p => p.Code == code, ct))
                db.Set<Permission>().Add(new Permission { Code = code, Module = module, Description = desc });
        }

        var seedRoles = new (string Code, string Name, string Description, bool IsSystem)[]
        {
            ("super-admin", "Super administrator", "Platform owner — can create other admins", true),
            ("admin",       "Administrator",       "Platform administrator",                  true),
            ("user",        "User",                "Standard end user",                       true),
        };

        foreach (var (code, name, desc, isSys) in seedRoles)
        {
            if (!await db.Set<Role>().AnyAsync(r => r.Code == code, ct))
                db.Set<Role>().Add(new Role { Code = code, Name = name, Description = desc, IsSystem = isSys });
        }

        await db.SaveChangesAsync(ct);

        // Grant every permission to the super-admin role.
        await GrantRolePermissionsAsync("super-admin", p => true, ct);

        // Grant a *base* operational set to the `admin` role. Anything beyond
        // this — including the right to create more admins — is granted per
        // user via UserPermission rows, controlled by a super-admin.
        await GrantRolePermissionsAsync(
            "admin",
            p => !SuperAdminOnlyPermissions.Contains(p.Code),
            ct);

        // Seed the platform owner exactly once.
        await EnsureSuperAdminAsync(
            email:    "deepak@gmail.com",
            username: "deepak",
            password: "deepak@1234",
            ct);
    }

    private async Task GrantRolePermissionsAsync(
        string roleCode,
        Func<Permission, bool> filter,
        CancellationToken ct)
    {
        var role = await db.Set<Role>().FirstAsync(r => r.Code == roleCode, ct);
        var perms = await db.Set<Permission>().ToListAsync(ct);

        foreach (var p in perms.Where(filter))
        {
            var exists = await db.Set<RolePermission>()
                .AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == p.Id, ct);
            if (!exists)
                db.Set<RolePermission>().Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureSuperAdminAsync(string email, string username, string password, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await db.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (existing is null)
        {
            existing = new User
            {
                Email         = normalizedEmail,
                Username      = username,
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
                DisplayName   = "Deepak",
                EmailVerified = true,
                IsActive      = true,
                IsAdmin       = true,
                IsVerified    = true,
            };
            db.Set<User>().Add(existing);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            // Idempotent: re-seed runs do NOT reset the password (the owner may
            // have rotated it). They only repair the flags that gate admin access.
            var dirty = false;
            if (!existing.IsAdmin)   { existing.IsAdmin   = true; dirty = true; }
            if (!existing.IsActive)  { existing.IsActive  = true; dirty = true; }
            if (!existing.EmailVerified) { existing.EmailVerified = true; dirty = true; }
            if (dirty) await db.SaveChangesAsync(ct);
        }

        // Attach to the super-admin role if not already.
        var superAdminRole = await db.Set<Role>().FirstAsync(r => r.Code == "super-admin", ct);
        var alreadyAssigned = await db.Set<UserRole>()
            .AnyAsync(ur => ur.UserId == existing.Id && ur.RoleId == superAdminRole.Id, ct);
        if (!alreadyAssigned)
        {
            db.Set<UserRole>().Add(new UserRole { UserId = existing.Id, RoleId = superAdminRole.Id });
            await db.SaveChangesAsync(ct);
        }
    }
}
