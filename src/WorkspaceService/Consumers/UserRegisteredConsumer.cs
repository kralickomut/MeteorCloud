using MassTransit;
using MeteorCloud.Messaging.Events;
using MeteorCloud.Messaging.Events.Workspace;
using WorkspaceService.Services;

namespace WorkspaceService.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private readonly WorkspaceManager _workspaceManager;
    private readonly IPublishEndpoint _publishEndpoint;
    
    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger, WorkspaceManager workspaceManager, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _workspaceManager = workspaceManager;
        _publishEndpoint = publishEndpoint;
    }
    
    
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;

        var matchInvitation = await _workspaceManager.GetInvitationByEmailAsync(message.Email);

        if (matchInvitation is null) return;

        var ws = await _workspaceManager.GetWorkspaceByIdAsync(matchInvitation.WorkspaceId);
        
        if (ws is null) throw new Exception("Workspace not found and should be found!!! Consumer at user registered.");
        
        await _publishEndpoint.Publish(new WorkspaceInvitationMatchOnRegisterEvent(message.UserId, ws.Name, matchInvitation.Token.ToString(), ws.Id));
        _logger.LogInformation("User {UserId} registered and matched with workspace invitation for email {Email}.", message.UserId, message.Email);
        
        
    }
}