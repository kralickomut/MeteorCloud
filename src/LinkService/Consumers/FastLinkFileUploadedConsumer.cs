using LinkService.Persistence.Entities;
using LinkService.Services;
using MassTransit;
using MeteorCloud.Messaging.Events.FastLink;

namespace LinkService.Consumers;

public class FastLinkFileUploadedConsumer : IConsumer<FastLinkFileUploadedEvent>
{
    
    private readonly ILogger<FastLinkFileUploadedConsumer> _logger;
    private readonly FastLinkManager _fastLinkManager;
    
    public FastLinkFileUploadedConsumer(ILogger<FastLinkFileUploadedConsumer> logger, FastLinkManager fastLinkManager)
    {
        _logger = logger;
        _fastLinkManager = fastLinkManager;
    }
    
    public async Task Consume(ConsumeContext<FastLinkFileUploadedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation($"Received FastLinkFileUploadedEvent: {message.Id}");
        
        try
        {
            var link = new FastLink
            {
                AccessCount = 0,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = message.ExpiresAt,
                FileId = message.FileId,
                Name = message.Name,
                CreatedByUserId = message.UserId,
                FileName = message.FileName,
                FileSize = message.FileSize,
                Token = Guid.NewGuid().ToString()  // Generate a unique token for the link
            };
            
            var newLink = await _fastLinkManager.CreateLinkAsync(link);
            
            if (newLink is null)
            {
                _logger.LogError($"Failed to create FastLink: {message.Id}");
                throw new Exception("Failed to create FastLink");
            }
            _logger.LogInformation($"FastLink file created successfully: {message.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing FastLinkFileUploadedEvent: {message.Id}");
            throw;
        }
    }
}