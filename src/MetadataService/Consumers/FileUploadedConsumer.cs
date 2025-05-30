using MassTransit;
using MetadataService.Persistence.Entities;
using MetadataService.Services;
using MeteorCloud.Messaging.Events.File;

namespace MetadataService.Consumers;

public class FileUploadedConsumer : IConsumer<FileUploadedEvent>
{
    private readonly ILogger<FileUploadedConsumer> _logger;
    private readonly IFileMetadataManager _fileMetadataManager;
    
    public FileUploadedConsumer(
        ILogger<FileUploadedConsumer> logger,
        IFileMetadataManager fileMetadataManager)
    {
        _logger = logger;
        _fileMetadataManager = fileMetadataManager;
    }
    
    public async Task Consume(ConsumeContext<FileUploadedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("File uploaded event consumed: {EventId}", message.Id);
        
        if (!Guid.TryParse(message.FileId, out var fileId))
        {
            _logger.LogError("Invalid file ID format: {FileId}", message.FileId);
            throw new ArgumentException("Invalid file ID format", nameof(message.FileId));
        }
        
        // Check if the file already exists in the database
        var existingFile = await _fileMetadataManager.GetByIdAsync(fileId);
        if (existingFile != null)
        {
            _logger.LogWarning("File metadata already exists for file: {FileId}", message.FileId);
            return;
        }
        
        var fileMetadata = new FileMetadata
        {
            Id = fileId,
            FileName = message.FileName,
            WorkspaceId = message.WorkspaceId,
            Size = message.Size,
            Path = message.FolderPath,
            ContentType = message.ContentType,
            UploadedAt = message.UploadedAt,
            UploadedBy = message.UploadedBy,
        };
        
        try
        {
            await _fileMetadataManager.CreateAsync(fileMetadata);
            _logger.LogInformation("File metadata saved successfully for file: {FileName}", message.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file metadata for file: {FileName}", message.FileName);
            throw;
        }
    }
}