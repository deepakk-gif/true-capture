using Microsoft.Extensions.Logging;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Services;

public sealed class AuditLogger(ICurrentUser currentUser, ILogger<AuditLogger> log) : IAuditLogger
{
    public Task LogAsync(string action, long? entityId = null, string? detail = null, CancellationToken ct = default)
    {
        log.LogInformation("AUDIT user={UserId} action={Action} entity={EntityId} detail={Detail}",
            currentUser.UserId, action, entityId, detail);
        return Task.CompletedTask;
    }
}
