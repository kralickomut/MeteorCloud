using MassTransit;
using MetadataService.Services;
using MeteorCloud.Messaging.Events.File;

namespace MetadataService.Consumers;

public class FileMovedConsumer : IConsumer<FileMovedEvent>
{
    private readonly ILogger<FileMovedConsumer> _logger;
    private readonly IFileMetadataManager _metadataManager;
    
    public FileMovedConsumer(ILogger<FileMovedConsumer> logger, IFileMetadataManager metadataManager)
    {
        _logger = logger;
        _metadataManager = metadataManager;
    }
    
    
    public async Task Consume(ConsumeContext<FileMovedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("File moved event received: {SourcePath} -> {TargetFolder}", message.SourcePath, message.TargetFolder);
        
        // Update metadata in the database
        var metadata = await _metadataManager.GetByIdAsync(message.FileId);
        
        if (metadata != null)
        {
            if (metadata.Path == message.TargetFolder)
            {
                return;
            }
            
            metadata.Path = message.TargetFolder;
            await _metadataManager.UpdateAsync(metadata);
            
            _logger.LogInformation("File metadata updated for file ID: {FileId}", message.FileId);
        }
        else
        {
            _logger.LogWarning("File metadata not found for file ID: {FileId}", message.FileId);
        }
    }
}