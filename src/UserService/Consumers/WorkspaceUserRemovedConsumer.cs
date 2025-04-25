using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;

namespace UserService.Consumers;

public class WorkspaceUserRemovedConsumer : IConsumer<WorkspaceUserRemovedEvent>
{
    private readonly Services.UserService _userService;
    private readonly ILogger<WorkspaceUserRemovedConsumer> _logger;
    
    public WorkspaceUserRemovedConsumer(Services.UserService userService, ILogger<WorkspaceUserRemovedConsumer> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceUserRemovedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received WorkspaceUserRemovedEvent: {WorkspaceId}, {UserId}", message.WorkspaceId, message.UserId);
        
        var user = await _userService.GetUserByIdAsync(message.UserId);
        
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found.", message.UserId);
            throw new Exception("USER NOT FOUND");
        }

        user.InTotalWorkspaces--;

        await _userService.UpdateUserAsync(user);
    }
}