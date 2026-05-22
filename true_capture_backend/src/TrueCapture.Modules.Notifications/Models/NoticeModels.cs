using TrueCapture.Modules.Notifications.Entities;

namespace TrueCapture.Modules.Notifications.Models;

/// <summary>An in-app notice as shown in the user's inbox.</summary>
public sealed record NoticeItem(
    long     Id,
    string   Title,
    string   Body,
    bool     IsRead,
    DateTime CreatedAtUtc)
{
    public static NoticeItem From(AppNotice n) => new(n.Id, n.Title, n.Body, n.IsRead, n.CreatedAtUtc);
}

public sealed record NoticeListResult(IReadOnlyList<NoticeItem> Items, string? NextCursor);

public sealed record UnreadCountResult(int Count);

// ---- Admin per-user messaging request bodies ----------------------------

/// <summary>Body of `POST /api/admin/users/{id}/notify` — an FCM push.</summary>
public sealed record SendUserNotificationDto(string Title, string Body);

/// <summary>Body of `POST /api/admin/users/{id}/notice` — an in-app notice.</summary>
public sealed record SendUserNoticeDto(string Title, string Body);

/// <summary>Body of `POST /api/admin/users/{id}/email`.</summary>
public sealed record SendUserEmailDto(string Subject, string Body);
