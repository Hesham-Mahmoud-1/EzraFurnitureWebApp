using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Ezura.Web.Hubs;

/// <summary>
/// Real-time notification hub. Authenticated users join a personal group
/// so notifications can be pushed without broadcasting to all clients.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogDebug("User {UserId} connected to NotificationHub", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task MarkNotificationRead(int notificationId)
    {
        // Client-callable method to update read state in real time
        var userId = Context.UserIdentifier;
        if (userId != null)
            await Clients.Group($"user-{userId}")
                .SendAsync("NotificationRead", notificationId);
    }
}
