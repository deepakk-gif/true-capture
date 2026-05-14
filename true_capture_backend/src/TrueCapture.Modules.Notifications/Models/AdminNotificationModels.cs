namespace TrueCapture.Modules.Notifications.Models;

public sealed record SendTopicDto(
    string                              Topic,
    string                              Title,
    string                              Body,
    IReadOnlyDictionary<string, string>? Data);

public sealed record SendUsersDto(
    IReadOnlyList<long>                  UserIds,
    string                               Title,
    string                               Body,
    IReadOnlyDictionary<string, string>? Data);

public sealed record SendFilteredDto(
    string?                              Search,
    bool?                                IsActive,
    bool?                                IsAdmin,
    bool?                                IsVerified,
    bool?                                HasGoogle,
    string                               Title,
    string                               Body,
    IReadOnlyDictionary<string, string>? Data);

public sealed record SendNotificationResultDto(
    int                   SentCount,
    int                   FailedCount,
    int                   InvalidTokensPruned,
    int                   TargetedDeviceCount);
