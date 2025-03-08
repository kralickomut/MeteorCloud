using MassTransit;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Caching.Services;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.Workspace;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using WorkspaceService.Features;
using WorkspaceService.Persistence;
using WorkspaceService.Services;

namespace WorkspaceService.Extensions;

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
        
        // Register WorkspaceRepository
        services.AddScoped<WorkspaceRepository>();
        
        // Register WorkspaceManager
        services.AddScoped<WorkspaceManager>();
        
        // Register CreateWorkspaceHandler
        services.AddScoped<CreateWorkspaceHandler>();
        services.AddSingleton<CreateWorkspaceRequestValidator>();
        
        // Register DeleteWorkspaceHandler
        services.AddScoped<DeleteWorkspaceHandler>();
        services.AddSingleton<DeleteWorkspaceRequestValidator>();
        
        // Register UpdateWorkspaceHandler
        services.AddScoped<UpdateWorkspaceHandler>();
        services.AddSingleton<UpdateWorkspaceRequestValidator>();
        
        // Get Redis connection details from environment variables
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
        var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
        var redisConnectionString = $"{redisHost}:{redisPort}";

        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));

        // Register Redis Cache Service from shared library
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddHttpClient<MSHttpClient>();
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Workspace Service API", Version = "v1" });
        });

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                rabbitCfg.Message<WorkspaceCreatedEvent>(x => x.SetEntityName("workspaces"));
                rabbitCfg.Message<WorkspaceDeletedEvent>(x => x.SetEntityName("workspaces"));
                
                rabbitCfg.ConfigureEndpoints(context);
                
            });
        });
        
        return services;
    }
}