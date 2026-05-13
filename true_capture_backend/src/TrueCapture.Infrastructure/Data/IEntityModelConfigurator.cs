using Microsoft.EntityFrameworkCore;

namespace TrueCapture.Infrastructure.Data;

public interface IEntityModelConfigurator
{
    void Configure(ModelBuilder modelBuilder);
}
