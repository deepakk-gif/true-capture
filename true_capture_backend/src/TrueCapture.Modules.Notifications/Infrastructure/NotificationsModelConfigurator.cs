using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Notifications.Entities;
using TrueCapture.Shared.Constants;

namespace TrueCapture.Modules.Notifications.Infrastructure;

public sealed class NotificationsModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder b)
    {
        b.Entity<AppNotice>(e =>
        {
            e.ToTable("AppNotice", schema: Schemas.Notifications);
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Body).HasMaxLength(2000).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.RecipientUserId, x.Id });
            e.HasOne<User>().WithMany()
                .HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
