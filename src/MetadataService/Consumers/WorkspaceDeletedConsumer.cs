using MassTransit;
using MetadataService.Services;
using MeteorCloud.Messaging.Events.Workspace;

namespace MetadataService.Consumers;

public class WorkspaceDeletedConsumer : IConsumer<WorkspaceDeletedEvent>
{
    private readonly ILogger<WorkspaceDeletedConsumer> _logger;
    private readonly IFileMetadataManager _fileMetadataManager;
    
    public WorkspaceDeletedConsumer(ILogger<WorkspaceDeletedConsumer> logger, IFileMetadataManager fileMetadataManager)
    {
        _logger = logger;
        _fileMetadataManager = fileMetadataManager;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceDeletedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received WorkspaceDeletedEvent: {EventId}", message.Id);
        
        // Delete all files associated with the workspace
        var result = await _fileMetadataManager.DeleteByWorkspaceAsync(message.WorkspaceId, context.CancellationToken);
        
        if (result)
        {
            _logger.LogInformation("Deleted files for WorkspaceId: {WorkspaceId}", message.WorkspaceId);
        }
        else
        {
            _logger.LogWarning("Failed to delete files for WorkspaceId: {WorkspaceId}", message.WorkspaceId);
            throw new Exception("Failed to delete files for workspace.");
        }
    }
}