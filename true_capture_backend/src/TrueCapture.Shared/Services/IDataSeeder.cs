namespace TrueCapture.Shared.Services;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class DataSeederOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
