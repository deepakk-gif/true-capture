using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Users.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Users.Services;

public sealed class AdminAccountsService(
    AppDbContext db,
    IBaseService baseService) : IAdminAccountsService
{
    public Task<Result<IReadOnlyList<PermissionDescriptor>>> ListPermissionsAsync(CancellationToken ct)
        => baseService.ExecuteAsync<IReadOnlyList<PermissionDescriptor>>("AdminAccounts.ListPermissions", async () =>
        {
            var rows = await db.Set<Permission>()
                .AsNoTracking()
                .OrderBy(p => p.Module).ThenBy(p => p.Code)
                .Select(p => new PermissionDescriptor(p.Code, p.Module, p.Description))
                .ToListAsync(ct);

            return Result<IReadOnlyList<PermissionDescriptor>>.Success(rows);
        }, ct);

    public Task<Result<CreatedAdminResponse>> CreateAdminAsync(CreateAdminRequest req, CancellationToken ct)
        => baseService.ExecuteAsync("AdminAccounts.CreateAdmin", async () =>
        {
            // Normalize inputs.
            var email    = (req.Email ?? "").Trim().ToLowerInvariant();
            var username = (req.Username ?? "").Trim();
            var password = req.Password ?? "";

            // Basic validation.
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(email))    errors.Add("Email is required.");
            if (string.IsNullOrWhiteSpace(username)) errors.Add("Username is required.");
            if (username.Length < 3)                 errors.Add("Username must be at least 3 characters.");
            if (password.Length < 8)                 errors.Add("Password must be at least 8 characters.");
            if (errors.Count > 0)
                return Result<CreatedAdminResponse>.Validation(errors);

            // Uniqueness.
            if (await db.Set<User>().AnyAsync(u => u.Email    == email,    ct))
                return Result<CreatedAdminResponse>.Conflict("An account with that email already exists.");
            if (await db.Set<User>().AnyAsync(u => u.Username == username, ct))
                return Result<CreatedAdminResponse>.Conflict("That username is taken.");

            // Resolve permission codes to ids. Unknown codes fail the whole request
            // so the super-admin doesn't accidentally grant a typo'd power.
            var requested = (req.PermissionCodes ?? Array.Empty<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var perms = await db.Set<Permission>()
                .Where(p => requested.Contains(p.Code))
                .ToListAsync(ct);

            if (perms.Count != requested.Count)
            {
                var found = perms.Select(p => p.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var unknown = requested.Where(c => !found.Contains(c)).ToList();
                return Result<CreatedAdminResponse>.Validation(
                    [$"Unknown permission code(s): {string.Join(", ", unknown)}."]);
            }

            // Create the user.
            var user = new User
            {
                Email         = email,
                Username      = username,
                DisplayName   = string.IsNullOrWhiteSpace(req.DisplayName) ? null : req.DisplayName!.Trim(),
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
                EmailVerified = true,        // super-admin vouches for them
                IsActive      = true,
                IsAdmin       = true,        // panel gate
            };
            db.Set<User>().Add(user);
            await db.SaveChangesAsync(ct);

            // Attach the `admin` role (so JWT role claim is "Admin" — middleware gate).
            var adminRole = await db.Set<Role>().FirstAsync(r => r.Code == "admin", ct);
            db.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });

            // Per-user permission grants.
            foreach (var p in perms)
            {
                db.Set<UserPermission>().Add(new UserPermission
                {
                    UserId       = user.Id,
                    PermissionId = p.Id,
                });
            }

            await db.SaveChangesAsync(ct);

            return Result<CreatedAdminResponse>.Success(new CreatedAdminResponse(
                user.Id,
                user.Email,
                user.Username,
                user.DisplayName,
                perms.Select(p => p.Code).ToList()));
        }, ct, useTransaction: true);
}
