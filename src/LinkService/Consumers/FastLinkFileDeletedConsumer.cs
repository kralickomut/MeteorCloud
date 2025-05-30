using LinkService.Services;
using MassTransit;
using MeteorCloud.Messaging.Events.FastLink;

namespace LinkService.Consumers;

public class FastLinkFileDeletedConsumer : IConsumer<FastLinkFileDeletedEvent>
{
    
    private readonly ILogger<FastLinkFileDeletedConsumer> _logger;
    private readonly FastLinkManager _fastLinkManager;
    
    public FastLinkFileDeletedConsumer(ILogger<FastLinkFileDeletedConsumer> logger, FastLinkManager fastLinkManager)
    {
        _logger = logger;
        _fastLinkManager = fastLinkManager;
    }
    
    public async Task Consume(ConsumeContext<FastLinkFileDeletedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("FastLinkFileDeletedConsumer: {Message}", message.Id);
        
        try
        {
            await _fastLinkManager.DeleteLinkAsync(message.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileId}", message.FileId);
        }
    }
}