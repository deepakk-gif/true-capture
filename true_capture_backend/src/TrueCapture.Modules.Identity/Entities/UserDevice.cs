using TrueCapture.Shared.Data;

namespace TrueCapture.Modules.Identity.Entities;

public class UserDevice : BaseEntity
{
    public long     UserId        { get; set; }
    public string   FcmToken      { get; set; } = "";
    public string?  DeviceType    { get; set; }   // "ios" | "android" | "web"
    public DateTime LastUsedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
