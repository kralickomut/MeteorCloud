using AuditService.Persistence;
using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;

namespace AuditService.Consumers.Workspace;

public class WorkspaceDeletedConsumer : IConsumer<WorkspaceDeletedEvent>
{
    private readonly AuditRepository _repository;
    private readonly ILogger<WorkspaceDeletedConsumer> _logger;
    
    public WorkspaceDeletedConsumer(AuditRepository repository, ILogger<WorkspaceDeletedConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceDeletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Workspace Deleted Event Received: {WorkspaceId}", message.Id.ToString());

        try
        {
            await _repository.DeleteAllByWorkspaceIdAsync(message.WorkspaceId);
            _logger.LogInformation("Successfully deleted all audit records for workspace {WorkspaceId}", message.WorkspaceId);
        } 
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete audit records for workspace {WorkspaceId}", message.WorkspaceId);
        }
    }
}