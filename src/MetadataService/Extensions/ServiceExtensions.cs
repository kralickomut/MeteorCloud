using MassTransit;
using MetadataService.Consumers;
using MetadataService.Features;
using MetadataService.Persistence;
using MetadataService.Services;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Caching.Services;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.File;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace MetadataService.Extensions;

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

        services.AddSingleton<CreateFolderRequestValidator>();
        services.AddScoped<CreateFolderHandler>();
        
        services.AddSingleton<BuildTreeRequestValidator>();
        services.AddScoped<BuildTreeHandler>();

        services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
        services.AddScoped<IFileMetadataManager, FileMetadataManager>();
        
        
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
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Workspace Service API", Version = "v1" });
        });
        
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddConsumer<FileUploadedConsumer>();
            busConfigurator.AddConsumer<FileDeletedConsumer>();
            busConfigurator.AddConsumer<WorkspaceDeletedConsumer>();
            busConfigurator.AddConsumer<FolderDeletedConsumer>();
            busConfigurator.AddConsumer<FileMovedConsumer>();
            
            busConfigurator.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                rabbitCfg.ReceiveEndpoint("metadata-service-file-uploaded-queue", e =>
                {
                    e.Bind("file-uploaded", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<FileUploadedConsumer>(context);
                });
                
                rabbitCfg.ReceiveEndpoint("metadata-service-file-deleted-queue", e =>
                {
                    e.Bind("file-deleted", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<FileDeletedConsumer>(context);
                });
                
                rabbitCfg.ReceiveEndpoint("metadata-service-workspace-deleted-queue", e =>
                {
                    e.Bind("workspace-deleted", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<WorkspaceDeletedConsumer>(context);
                });
                
                rabbitCfg.ReceiveEndpoint("metadata-service-folder-deleted-queue", e =>
                {
                    e.Bind("folder-deleted", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<FolderDeletedConsumer>(context);
                });
                
                rabbitCfg.ReceiveEndpoint("metadata-service-file-moved-queue", e =>
                {
                    e.Bind("file-moved", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<FileMovedConsumer>(context);
                });
                
                rabbitCfg.ConfigureEndpoints(context);
                
            });
        });
        
        return services;
    }
}