using FileService.Services;
using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;

namespace FileService.Consumers;

public class WorkspaceDeletedConsumer : IConsumer<WorkspaceDeletedEvent>
{
    private readonly ILogger<WorkspaceDeletedConsumer> _logger;
    private readonly BlobStorageService _storageService;
    
    public WorkspaceDeletedConsumer(ILogger<WorkspaceDeletedConsumer> logger, BlobStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }
    
    public async Task Consume(ConsumeContext<WorkspaceDeletedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received WorkspaceDeletedEvent: {EventId}", message.Id);
        
        // Delete all files associated with the workspace
        await _storageService.DeleteFolderAsync(message.WorkspaceId.ToString());
        
        _logger.LogInformation("Deleted files for WorkspaceId: {WorkspaceId}", message.WorkspaceId);
    }
}