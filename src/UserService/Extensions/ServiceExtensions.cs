using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Caching.Services;
using Microsoft.OpenApi.Models;
using MassTransit;
using MeteorCloud.Messaging.Events;
using MeteorCloud.Messaging.Events.Workspace;
using StackExchange.Redis;
using UserService.Consumers;
using UserService.Features;
using UserService.Persistence;
using UserService.Services;

namespace UserService.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register IConfiguration explicitly
        services.AddSingleton<IConfiguration>(configuration);

        // Register DatabaseInitializer and pass IConfiguration
        services.AddSingleton<DatabaseInitializer>();

        // Ensure DapperContext receives IConfiguration
        services.AddSingleton<DapperContext>();

        services.AddScoped<UserRepository>();
        services.AddScoped<UserManager>();

        services.AddSingleton<GetUserValidator>();
        services.AddScoped<GetUserHandler>();
        
        services.AddSingleton<GetUsersValidator>();
        services.AddScoped<GetUsersHandler>();
        
        services.AddSingleton<UpdateUserValidator>();
        services.AddScoped<UpdateUserHandler>();
        
        services.AddSingleton<CreateUserValidator>();
        services.AddScoped<CreateUserHandler>();
        
        services.AddSingleton<DeleteUserValidator>();
        services.AddScoped<DeleteUserHandler>();
        
        // Get Redis connection details from environment variables
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
        var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
        var redisConnectionString = $"{redisHost}:{redisPort}";

        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));

        // Register Redis Cache Service from shared library
        services.AddScoped<ICacheService, RedisCacheService>();
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Service API", Version = "v1" });
        });

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddConsumer<WorkspaceCreatedConsumer>();
            busConfigurator.AddConsumer<WorkspaceDeletedConsumer>();
            
            busConfigurator.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                // Publish messages to the "users" exchange
                rabbitCfg.Message<UserUpdatedEvent>(x => x.SetEntityName("users"));
                rabbitCfg.Message<UserDeletedEvent>(x => x.SetEntityName("users"));
                
                rabbitCfg.ReceiveEndpoint("workspace-created-queue", e =>
                {
                    e.Bind("workspaces");
                    e.ConfigureConsumer<WorkspaceCreatedConsumer>(context);
                });
                
                rabbitCfg.ReceiveEndpoint("workspace-deleted-queue", e =>
                {
                    e.Bind("workspaces");
                    e.ConfigureConsumer<WorkspaceDeletedConsumer>(context);
                });
                
                rabbitCfg.ConfigureEndpoints(context);
            });
        });
        
        return services;
    }
}