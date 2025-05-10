using FileService.Services;
using MassTransit;
using MeteorCloud.Messaging.Events.FastLink;

namespace FileService.Consumers;

public class FastLinkExpireCleanupConsumer : IConsumer<FastLinkExpireCleanupEvent>
{
    private readonly ILogger<FastLinkExpireCleanupConsumer> _logger;
    private readonly BlobStorageService _blobStorageService;
    private const string _storagePath = "fast-link-files";
    
    public FastLinkExpireCleanupConsumer(ILogger<FastLinkExpireCleanupConsumer> logger, BlobStorageService blobStorageService)
    {
        _logger = logger;
        _blobStorageService = blobStorageService;
    }
    
    public async Task Consume(ConsumeContext<FastLinkExpireCleanupEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("FastLinkExpireCleanupConsumer received {Count} expired links", message.ExpiredLinks.Count);
        
        foreach (var expired in message.ExpiredLinks)
        {
            var path = $"{_storagePath}/{expired.UserId}/{expired.FileId}";
            var (result, _, _) = await _blobStorageService.DeleteFileAsync(path, context.CancellationToken);

            if (result)
            {
                _logger.LogInformation("Deleted expired link for user {UserId} and file {FileId}", expired.UserId, expired.FileId);
            }
            else
            {
                _logger.LogWarning("Failed to delete expired link for user {UserId} and file {FileId}", expired.UserId, expired.FileId);
            }
        }
    }
}