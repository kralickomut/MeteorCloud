using EmailService.Persistence;
using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;

namespace EmailService.Consumers.Workspace;

public class WorkspaceInvitationMatchOnRegisterConsumer : IConsumer<WorkspaceInvitationMatchOnRegisterEvent>
{
    private readonly ILogger<WorkspaceInvitationMatchOnRegisterConsumer> _logger;
    private readonly NotificationRepository _notificationRepository;
    
    public WorkspaceInvitationMatchOnRegisterConsumer(
        ILogger<WorkspaceInvitationMatchOnRegisterConsumer> logger, 
        NotificationRepository notificationRepository)
    {
        _logger = logger;
        _notificationRepository = notificationRepository;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceInvitationMatchOnRegisterEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received WorkspaceInvitationMatchOnRegisterEvent: {UserId}, {WorkspaceName}, {Token}, {WorkspaceId}", 
            message.UserId, message.WorkspaceName, message.Token, message.WorkspaceId);

        var notification = await _notificationRepository.CreateAsync(new Notification
        {
            UserId = message.UserId,
            Message = $"{message.Token}-You've been invited to join a workspace {message.WorkspaceName}!",
            Title = "Workspace Invitation",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            WorkspaceId = message.WorkspaceId,
            
        });
        
        if (notification is null)
        {
            _logger.LogError("Failed to create notification for user {UserId}.", message.UserId);
        }
        
    }
}