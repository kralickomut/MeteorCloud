using Microsoft.AspNetCore.SignalR;

namespace EmailService.Hubs;

public class NotificationHub : Hub
{
    
}

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Adjust this if your JWT uses a different claim name (e.g., "id" or "sub")
        return connection.User?.FindFirst("id")?.Value;
    }
}