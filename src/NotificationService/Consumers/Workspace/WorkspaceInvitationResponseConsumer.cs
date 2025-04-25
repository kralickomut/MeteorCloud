using EmailService.Persistence;
using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;

namespace EmailService.Consumers.Workspace;

public class WorkspaceInvitationResponseConsumer : IConsumer<WorkspaceInvitationResponseEvent>
{
    private readonly ILogger<WorkspaceInvitationResponseConsumer> _logger;
    private readonly NotificationRepository _notificationRepository;
    
    public WorkspaceInvitationResponseConsumer(
        ILogger<WorkspaceInvitationResponseConsumer> logger,
        NotificationRepository notificationRepository)
    {
        _logger = logger;
        _notificationRepository = notificationRepository;
    }
    
    
    public async Task Consume(ConsumeContext<WorkspaceInvitationResponseEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received WorkspaceInvitationResponseEvent: UserId={UserId}, WorkspaceId={WorkspaceId} Accept={IsAccepted}", message.UserId, message.WorkspaceId, message.IsAccepted);

        await _notificationRepository.UpdateWorkspaceInvitationResponseAsync(message.UserId, message.WorkspaceId,
            message.IsAccepted);
    }
}