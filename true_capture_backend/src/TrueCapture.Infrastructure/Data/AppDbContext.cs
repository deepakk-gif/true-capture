using Microsoft.EntityFrameworkCore;
using TrueCapture.Shared.Data;

namespace TrueCapture.Infrastructure.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext>          options,
    IEnumerable<IEntityModelConfigurator>   modelConfigurators)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var c in modelConfigurators)
            c.Configure(modelBuilder);

        // Soft-delete query filter for every BaseEntity-derived entity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var param  = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var prop   = System.Linq.Expressions.Expression.Property(param, nameof(BaseEntity.IsDeleted));
                var notDel = System.Linq.Expressions.Expression.Equal(prop, System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(notDel, param);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
