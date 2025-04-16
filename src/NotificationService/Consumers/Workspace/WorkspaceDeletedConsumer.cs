using EmailService.Persistence;
using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;

namespace EmailService.Consumers.Workspace;

public class WorkspaceDeletedConsumer : IConsumer<WorkspaceDeletedEvent>
{
    private readonly ILogger<WorkspaceDeletedConsumer> _logger;
    private readonly NotificationRepository _notificationRepository;
    
    public WorkspaceDeletedConsumer(
        ILogger<WorkspaceDeletedConsumer> logger,
        NotificationRepository notificationRepository)
    {
        _logger = logger;
        _notificationRepository = notificationRepository;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceDeletedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received WorkspaceDeletedEvent: {WorkspaceId}", message.Id);

        await _notificationRepository.DeleteByWorkspaceIdAsync(message.Id);
        
        _logger.LogInformation("Deleted notifications for WorkspaceId: {WorkspaceId}", message.Id);

    }
}