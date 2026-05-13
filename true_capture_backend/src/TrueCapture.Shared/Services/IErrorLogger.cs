namespace TrueCapture.Shared.Services;

public interface IErrorLogger
{
    Task LogAsync(string operationName, Exception ex, CancellationToken ct = default);
}
