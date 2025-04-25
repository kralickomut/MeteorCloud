using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;

namespace UserService.Consumers;

public class WorkspaceInvitationResponseConsumer : IConsumer<WorkspaceInvitationResponseEvent>
{
    private readonly ILogger<WorkspaceInvitationResponseConsumer> _logger;
    private readonly Services.UserService _userService;
    
    public WorkspaceInvitationResponseConsumer(
        ILogger<WorkspaceInvitationResponseConsumer> logger,
        Services.UserService userService)
    {
        _logger = logger;
        _userService = userService;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceInvitationResponseEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received WorkspaceInvitationResponseEvent: UserId={UserId}, WorkspaceId={WorkspaceId}", message.UserId, message.WorkspaceId);
        
        if (!message.IsAccepted) return; // nothing to increment
        
        var user = await _userService.GetUserByIdAsync(message.UserId);
        
        if (user is null) throw new Exception("User not found AND SHOULD BE FOUND!!");

        user.InTotalWorkspaces++;

        await _userService.UpdateUserAsync(user);
    }
}