using Microsoft.AspNetCore.SignalR;

namespace EmailService.Hubs;

public class NotificationHub : Hub
{
    
}

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("id")?.Value;
    }
}