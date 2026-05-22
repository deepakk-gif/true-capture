using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Notifications.Entities;

/// <summary>
/// A persistent in-app notice addressed to one user — the message body of the
/// in-app inbox. Created by an admin; read by the recipient.
/// </summary>
public class AppNotice : BaseEntity
{
    public long   RecipientUserId { get; set; }
    public string Title           { get; set; } = "";
    public string Body            { get; set; } = "";
    public bool   IsRead          { get; set; }
}
