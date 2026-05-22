using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Notifications.Services;

/// <summary>Admin-initiated, per-user messaging — push / in-app notice / email.</summary>
public interface IAdminUserMessagingService
{
    Task<Result<bool>> NotifyAsync(long userId, string title, string body, CancellationToken ct = default);
    Task<Result<bool>> NoticeAsync(long userId, string title, string body, CancellationToken ct = default);
    Task<Result<bool>> EmailAsync(long userId, string subject, string body, CancellationToken ct = default);
}
