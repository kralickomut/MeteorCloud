using Microsoft.AspNetCore.SignalR;

namespace EmailService.Hubs;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        Console.WriteLine($"🔗 SignalR connected: ConnectionId={Context.ConnectionId}, UserId={userId}");
        await base.OnConnectedAsync();
    }
}

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("id")?.Value;
    }
}

