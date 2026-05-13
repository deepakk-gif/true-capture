using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TrueCapture.Shared.Data;

namespace TrueCapture.Infrastructure.Data;

public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Stamp(DbContext? ctx)
    {
        if (ctx is null) return;
        var now    = DateTime.UtcNow;
        var userId = currentUser.UserId;

        foreach (EntityEntry<BaseEntity> entry in ctx.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc    = now;
                    entry.Entity.CreatedByUserId = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc    = now;
                    entry.Entity.UpdatedByUserId = userId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;     // soft delete
                    entry.Entity.IsDeleted       = true;
                    entry.Entity.DeletedAtUtc    = now;
                    entry.Entity.DeletedByUserId = userId;
                    break;
            }
        }
    }
}
