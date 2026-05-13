using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Shared.Constants;

namespace TrueCapture.Modules.Identity.Infrastructure;

public sealed class IdentityModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("User", schema: Schemas.Identity);
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Username).HasMaxLength(64).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(128);
            e.Property(x => x.AvatarUrl).HasMaxLength(512);
            e.Property(x => x.Bio).HasMaxLength(1000);
            e.Property(x => x.GoogleSubject).HasMaxLength(128);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.GoogleSubject);
        });

        b.Entity<Role>(e =>
        {
            e.ToTable("Role", schema: Schemas.Identity);
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<Permission>(e =>
        {
            e.ToTable("Permission", schema: Schemas.Identity);
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(128).IsRequired();
            e.Property(x => x.Module).HasMaxLength(64).IsRequired();
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<UserRole>(e =>
        {
            e.ToTable("UserRole", schema: Schemas.Identity);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
        });

        b.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermission", schema: Schemas.Identity);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshToken", schema: Schemas.Identity);
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.ReplacedByHash).HasMaxLength(256);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId);
            e.Ignore(x => x.IsActive);
        });

        b.Entity<OtpCode>(e =>
        {
            e.ToTable("OtpCode", schema: Schemas.Identity);
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.CodeHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.Purpose).HasConversion<int>().IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.Email, x.Purpose });
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            e.Ignore(x => x.IsActive);
        });
    }
}
