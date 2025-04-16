using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;

namespace UserService.Consumers;

public class WorkspaceInvitationAcceptedConsumer : IConsumer<WorkspaceInvitationAcceptedEvent>
{
    private readonly ILogger<WorkspaceInvitationAcceptedConsumer> _logger;
    private readonly Services.UserService _userService;
    
    public WorkspaceInvitationAcceptedConsumer(
        ILogger<WorkspaceInvitationAcceptedConsumer> logger,
        Services.UserService userService)
    {
        _logger = logger;
        _userService = userService;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceInvitationAcceptedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received WorkspaceInvitationAcceptedEvent: UserId={UserId}, WorkspaceId={WorkspaceId}", message.UserId, message.WorkspaceId);

        var user = await _userService.GetUserByIdAsync(message.UserId);
        
        if (user is null) throw new Exception("User not found AND SHOULD BE FOUND!!");

        user.InTotalWorkspaces++;

        await _userService.UpdateUserAsync(user);
    }
}