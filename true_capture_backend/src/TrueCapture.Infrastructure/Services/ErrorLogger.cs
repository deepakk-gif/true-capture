using Microsoft.Extensions.Logging;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Services;

public sealed class ErrorLogger(ILogger<ErrorLogger> log) : IErrorLogger
{
    public Task LogAsync(string operationName, Exception ex, CancellationToken ct = default)
    {
        log.LogError(ex, "Operation {Operation} failed", operationName);
        return Task.CompletedTask;
    }
}
