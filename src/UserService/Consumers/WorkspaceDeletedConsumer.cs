using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;
using UserService.Services;

namespace UserService.Consumers;

public class WorkspaceDeletedConsumer : IConsumer<WorkspaceDeletedEvent>
{
    private readonly UserManager _userManager;
    private readonly ILogger<WorkspaceDeletedConsumer> _logger;
    
    public WorkspaceDeletedConsumer(UserManager userManager, ILogger<WorkspaceDeletedConsumer> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceDeletedEvent> context)
    {
        var workspaceEvent = context.Message;

        await _userManager.DecrementWorkspaceCountForUsers(workspaceEvent.UserIds);
        
        _logger.LogInformation($"Decremented workspace count for {workspaceEvent.UserIds.Count()} users");
    }
}