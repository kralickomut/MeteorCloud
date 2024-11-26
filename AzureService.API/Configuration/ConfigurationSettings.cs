using AzureService.Application.Abstraction;
using AzureService.Infrastructure.Services;
using Messaging.Base.Abstraction;
using Messaging.Base.Extensions;
using Messaging.Base.Services;

namespace AzureService.API.Configuration;

public static class ConfigurationSettings
{
    public static void AddProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register RabbitMQ
        services.AddMassTransitWithRabbitMQ(configuration["RabbitMq:Host"]);
        
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IEventBus, EventBus>();
    }
}