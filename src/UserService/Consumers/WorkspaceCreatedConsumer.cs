using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;

namespace UserService.Consumers;

public class WorkspaceCreatedConsumer : IConsumer<WorkspaceCreatedEvent>
{
    private readonly ILogger<WorkspaceCreatedEvent> _logger;
    private readonly Services.UserService _userService;
    
    public WorkspaceCreatedConsumer(ILogger<WorkspaceCreatedEvent> logger, Services.UserService userService)
    {
        _logger = logger;
        _userService = userService;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceCreatedEvent> context)
    {
        var message = context.Message;

        var user = await _userService.GetUserByIdAsync(message.OwnerId);
        
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", message.OwnerId);
            return;
        }

        user.InTotalWorkspaces++;
        await _userService.UpdateUserAsync(user);
        
        _logger.LogInformation("User {UserId} has created a new workspace with ID {WorkspaceId}.", message.OwnerId, message.Id);
    }
}