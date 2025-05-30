using LinkService.Consumers;
using LinkService.Features;
using LinkService.Persistence;
using LinkService.Services;
using MassTransit;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Caching.Services;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.ConsumerExtensions;
using MeteorCloud.Messaging.Events.FastLink;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace LinkService.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register IConfiguration explicitly
        services.AddSingleton(configuration);

        // Register DatabaseInitializer and pass IConfiguration
        services.AddSingleton<DatabaseInitializer>();

        // Ensure DapperContext receives IConfiguration
        services.AddSingleton<DapperContext>();

        services.AddScoped<FastLinkRepository>();
        services.AddScoped<FastLinkManager>();

        services.AddSingleton<GetUserLinksValidator>();
        services.AddScoped<GetUserLinksHandler>();
        
        services.AddSingleton<GetLinkByTokenValidator>();
        services.AddScoped<GetLinkByTokenHandler>();
        
        services.AddSingleton<RefreshLinkValidator>();
        services.AddScoped<RefreshLinkHandler>();
        
        services.AddHostedService<ExpiredLinkCleanupService>();
        
        
        // Get Redis connection details from environment variables
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
        var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
        var redisConnectionString = $"{redisHost}:{redisPort}";

        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));

        // Register Redis Cache Service from shared library
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddHttpContextAccessor();
        services.AddTransient<AuthHeaderForwardingHandler>();
        services.AddHttpClient<MSHttpClient>()
            .AddHttpMessageHandler<AuthHeaderForwardingHandler>();
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Link Service API", Version = "v1" });
        });
        
        services.AddMassTransit(busConfigurator =>
        {
            
            busConfigurator.AddConsumer<FastLinkFileUploadedConsumer>();
            busConfigurator.AddConsumer<FastLinkFileDeletedConsumer>();
            
            busConfigurator.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                rabbitCfg.Message<FastLinkExpireCleanupEvent>(x => x.SetEntityName("fastlink-expired-link-cleanup"));
                
                rabbitCfg.ReceiveEndpoint("link-service-fastlink-file-uploaded", e =>
                {
                    e.ApplyStandardSettings();
                    e.Bind("fastlink-file-uploaded", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<FastLinkFileUploadedConsumer>(context);
                });
                
                rabbitCfg.ReceiveEndpoint("link-service-fastlink-file-deleted", e =>
                {
                    e.ApplyStandardSettings();
                    e.Bind("fastlink-file-deleted", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<FastLinkFileDeletedConsumer>(context);
                });
            });
        });
        
        return services;
    }
}