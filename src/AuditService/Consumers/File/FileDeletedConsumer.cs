using AuditService.Persistence;
using AuditService.Persistence.Entities;
using MassTransit;
using MeteorCloud.Messaging.Events.File;

namespace AuditService.Consumers.File;

public class FileDeletedConsumer : IConsumer<FileDeletedEvent>
{
    
    private readonly ILogger<FileDeletedConsumer> _logger;
    private readonly AuditRepository _repository;
    
    public FileDeletedConsumer(ILogger<FileDeletedConsumer> logger, AuditRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    public async Task Consume(ConsumeContext<FileDeletedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("📦 Received FileDeletedEvent: {FileId} in workspace {WorkspaceId}", message.Id, message.WorkspaceId);
        
        var audit = new AuditEvent
        {
            EntityType = "File",
            EntityId = message.FileId,
            Action = "Deleted",
            PerformedByUserId = message.DeletedBy,
            WorkspaceId = message.WorkspaceId,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                { "FileName", message.FileName }
            }
        };

        try
        {
            await _repository.InsertAsync(audit);
            _logger.LogInformation("Successfully inserted audit record for file {FileId} in workspace {WorkspaceId}", message.FileId, message.WorkspaceId);
        } 
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert audit record for file {FileId} in workspace {WorkspaceId}", message.FileId, message.WorkspaceId);
        }
    }
}