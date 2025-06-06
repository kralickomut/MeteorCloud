using System.Security.Cryptography;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Caching.Services;
using Microsoft.OpenApi.Models;
using MassTransit;
using MeteorCloud.Messaging.ConsumerExtensions;
using MeteorCloud.Messaging.Events;
using StackExchange.Redis;
using UserService.Consumers;
using UserService.Features;
using UserService.Persistence;

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
        services.AddScoped<Services.UserService>();

        services.AddSingleton<GetUserValidator>();
        services.AddScoped<GetUserHandler>();
        
        services.AddSingleton<GetUserByEmailValidator>();
        services.AddScoped<GetUserByEmailHandler>();
        
        services.AddSingleton<UpdateUserValidator>();
        services.AddScoped<UpdateUserHandler>();

        services.AddSingleton<GetUsersBulkValidator>();
        services.AddScoped<GetUsersBulkHandler>();
        
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

        services.AddMassTransit(config =>
        {
            config.AddConsumer<UserRegisteredConsumer>();
            config.AddConsumer<UserLoggedInConsumer>();
            config.AddConsumer<WorkspaceCreatedConsumer>();
            config.AddConsumer<WorkspaceDeletedConsumer>();
            config.AddConsumer<WorkspaceInvitationResponseConsumer>();
            config.AddConsumer<WorkspaceUserRemovedConsumer>();
            config.AddConsumer<ProfileImageUploadedConsumer>();

            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                cfg.Message<UserNameChangedEvent>(x => x.SetEntityName("user-name-changed"));
                
                cfg.ReceiveEndpoint("user-service-user-registered-queue", e =>
                {
                    e.ApplyStandardSettings();
                    e.Bind("user-registered", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<UserRegisteredConsumer>(context);
                });
                
                
                cfg.ReceiveEndpoint("user-service-user-logged-in-queue", e =>
                {
                    e.ApplyStandardSettings();
                    e.Bind("user-logged-in", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<UserLoggedInConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("user-service-workspace-created-queue", e =>
                {
                    e.ApplyStandardSettings();                    
                    e.Bind("workspace-created", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<WorkspaceCreatedConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("user-service-workspace-deleted-queue", e =>
                {                   
                    e.ApplyStandardSettings();
                    e.Bind("workspace-deleted", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<WorkspaceDeletedConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("user-service-workspace-invitation-accepted-queue", e =>
                {
                    e.ApplyStandardSettings();
                    e.Bind("workspace-invitation-response", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<WorkspaceInvitationResponseConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("user-service-workspace-user-removed-queue", e =>
                {
                    e.ApplyStandardSettings();
                    e.Bind("workspace-user-removed", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<WorkspaceUserRemovedConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("user-service-profile-image-uploaded-queue", e =>
                {
                    e.ApplyStandardSettings();
                    e.Bind("profile-image-uploaded", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<ProfileImageUploadedConsumer>(context);
                });
            });
        });
        
        return services;
    }
}