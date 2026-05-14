using TrueCapture.Modules.Notifications.Models;
using TrueCapture.Shared.Services;

namespace TrueCapture.Modules.Notifications.Services;

public interface IAdminNotificationService
{
    Task<Result<SendNotificationResultDto>> SendToTopicAsync   (SendTopicDto    dto, CancellationToken ct = default);
    Task<Result<SendNotificationResultDto>> SendToUsersAsync   (SendUsersDto    dto, CancellationToken ct = default);
    Task<Result<SendNotificationResultDto>> SendToFilteredAsync(SendFilteredDto dto, CancellationToken ct = default);
}
