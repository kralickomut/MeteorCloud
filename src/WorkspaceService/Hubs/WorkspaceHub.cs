using Microsoft.AspNetCore.SignalR;

namespace WorkspaceService.Hubs;

public class WorkspaceHub : Hub
{
    
}

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("id")?.Value;
    }
}