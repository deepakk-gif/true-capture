using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TrueCapture.Modules.Messaging.Hubs;

/// <summary>
/// Realtime chat channel. The hub is push-only: clients never invoke methods on
/// it — sending goes through the REST API, which then broadcasts here via
/// <c>IHubContext&lt;ChatHub, IChatClient&gt;</c>. Each connection joins the group
/// <c>user:{userId}</c> so the server can address a user across all their devices.
/// </summary>
[Authorize]
public sealed class ChatHub : Hub<IChatClient>
{
    /// <summary>Group name carrying every live connection of one user.</summary>
    public static string UserGroup(long userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId();
        if (userId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    private long CurrentUserId()
    {
        var raw = Context.User?.FindFirst("sub")?.Value
                  ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(raw, out var id) ? id : 0;
    }
}
