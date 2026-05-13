namespace TrueCapture.Shared.Services;

public interface IAuditLogger
{
    Task LogAsync(string action, long? entityId = null, string? detail = null, CancellationToken ct = default);
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class AuditableAttribute : Attribute { }
