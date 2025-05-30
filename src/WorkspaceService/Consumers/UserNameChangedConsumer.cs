using MassTransit;
using MeteorCloud.Messaging.Events;
using WorkspaceService.Services;

namespace WorkspaceService.Consumers;

public class UserNameChangedConsumer : IConsumer<UserNameChangedEvent>
{
    private readonly ILogger<UserNameChangedConsumer> _logger;
    private readonly WorkspaceManager _workspaceManager;
    
    public UserNameChangedConsumer(
        ILogger<UserNameChangedConsumer> logger,
        WorkspaceManager workspaceManager)
    {
        _logger = logger;
        _workspaceManager = workspaceManager;
    }
    
    public async Task Consume(ConsumeContext<UserNameChangedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("UserNameChangedEvent received: {UserId}, {NewName}", message.UserId, message.NewName);
        var workspaces = await _workspaceManager.GetWorkspacesWhereUserIsOwnerAsync(message.UserId);

        foreach (var workspace in workspaces)
        {
            workspace.OwnerName = message.NewName;
            await _workspaceManager.UpdateWorkspaceAsync(workspace);
        }
    }
}