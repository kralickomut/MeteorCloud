using MassTransit;
using MetadataService.Services;
using MeteorCloud.Messaging.Events.File;

namespace MetadataService.Consumers;

public class FileDeletedConsumer : IConsumer<FileDeletedEvent>
{
    private readonly ILogger<FileDeletedConsumer> _logger;
    private readonly IFileMetadataManager _fileMetadataManager;
    
    public FileDeletedConsumer(
        ILogger<FileDeletedConsumer> logger,
        IFileMetadataManager fileMetadataManager)
    {
        _logger = logger;
        _fileMetadataManager = fileMetadataManager;
    }
    
    public async Task Consume(ConsumeContext<FileDeletedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("File deleted event consumed: {EventId}", message.Id);

        if (!Guid.TryParse(message.FileId, out var fileId))
        {
            _logger.LogError("Invalid file ID format: {FileId}", message.FileId);
            return;
        }
        
        try
        {
            await _fileMetadataManager.DeleteAsync(fileId);
            _logger.LogInformation("File metadata deleted successfully for file: {FileId}", message.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file metadata for file: {FileId}", message.FileId);
            throw;
        }
    }
}