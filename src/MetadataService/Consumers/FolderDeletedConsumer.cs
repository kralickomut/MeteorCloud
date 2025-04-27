using MassTransit;
using MetadataService.Services;
using MeteorCloud.Messaging.Events;
using MeteorCloud.Messaging.Events.File;

namespace MetadataService.Consumers;

public class FolderDeletedConsumer : IConsumer<FolderDeletedEvent>
{
    private readonly ILogger<FolderDeletedConsumer> _logger;
    private readonly IFileMetadataManager _metadataManager;
    
    public FolderDeletedConsumer(ILogger<FolderDeletedConsumer> logger, IFileMetadataManager metadataManager)
    {
        _logger = logger;
        _metadataManager = metadataManager;
    }
    
    public async Task Consume(ConsumeContext<FolderDeletedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received FolderDeletedEvent: {EventId}", message.Id);
        
        var decodedPath = Uri.UnescapeDataString(message.Path);
        
        // Process the event
        var result = await _metadataManager.DeleteFolderAsync(decodedPath, context.CancellationToken);
        
        if (result)
        {
            _logger.LogInformation("Successfully processed FolderDeletedEvent: {EventId}", message.Id);
        }
        else
        {
            _logger.LogError("Failed to process FolderDeletedEvent: {EventId}", message.Id);
        }
    }
}