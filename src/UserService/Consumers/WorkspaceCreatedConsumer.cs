using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;
using UserService.Services;

namespace UserService.Consumers;

public class WorkspaceCreatedConsumer : IConsumer<WorkspaceCreatedEvent>
{
    
    private readonly ILogger<WorkspaceCreatedConsumer> _logger;
    private readonly UserManager _userManager;
    
    public WorkspaceCreatedConsumer(ILogger<WorkspaceCreatedConsumer> logger, UserManager userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceCreatedEvent> context)
    {
        var workspaceCreatedEvent = context.Message;
        
        await _userManager.IncrementUserWorkspaceCountAsync(workspaceCreatedEvent.OwnerId);
        
        _logger.LogInformation("Incremented workspace count for user ID: {UserId}", workspaceCreatedEvent.OwnerId);
    }
}