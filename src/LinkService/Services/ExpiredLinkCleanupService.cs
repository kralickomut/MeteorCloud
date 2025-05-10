using LinkService.Persistence;
using MassTransit;
using MeteorCloud.Messaging.Events.FastLink;

namespace LinkService.Services;

public class ExpiredLinkCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExpiredLinkCleanupService> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public ExpiredLinkCleanupService(IServiceProvider services, ILogger<ExpiredLinkCleanupService> logger, IPublishEndpoint publishEndpoint)
    {
        _services = services;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = DateTime.UtcNow.Date.AddDays(1); // midnight
            var delay = nextRun - now;

            _logger.LogInformation("ExpiredLinkCleanupService sleeping for {Delay}", delay);

            await Task.Delay(delay, stoppingToken);
            
            using var scope = _services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<FastLinkManager>();

            var rawList = await manager.DeleteExpiredLinksAsync(DateTime.UtcNow);
            var list = rawList.Select(x => new ExpiredFastLinkDto
            {
                UserId = x.UserId,
                FileId = x.FileId
            }).ToList();

            await _publishEndpoint.Publish(new FastLinkExpireCleanupEvent
            {
                ExpiredLinks = list
            });

            _logger.LogInformation("ExpiredLinkCleanupService deleted {Count} expired links", list.Count);
        }
    }
}