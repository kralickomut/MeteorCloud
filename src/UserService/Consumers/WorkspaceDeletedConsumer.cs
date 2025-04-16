using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;
using Microsoft.AspNetCore.SignalR;

namespace UserService.Consumers;

public class WorkspaceDeletedConsumer : IConsumer<WorkspaceDeletedEvent>
{
    private readonly ILogger<WorkspaceDeletedConsumer> _logger;
    private readonly Services.UserService _userService;
    
    public WorkspaceDeletedConsumer(ILogger<WorkspaceDeletedConsumer> logger, Services.UserService userService)
    {
        _logger = logger;
        _userService = userService;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceDeletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received WorkspaceDeletedEvent: {WorkspaceId}", message.Id);
        
        // Call to UserService to decrement the workspace count
        var userIds = message.UserIds;

        var result = await _userService.DecrementWorkspacesCountAsync(userIds);
        
        if (result)
        {
            _logger.LogInformation("Successfully decremented workspace count for users: {UserIds}", string.Join(", ", userIds));
        }
        else
        {
            _logger.LogWarning("Failed to decrement workspace count for users: {UserIds}", string.Join(", ", userIds));
        }
    }
}