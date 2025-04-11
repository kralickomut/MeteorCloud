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
        services.AddScoped<Services.UserService>();

        services.AddSingleton<GetUserValidator>();
        services.AddScoped<GetUserHandler>();
        
        
        
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

            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("user-service-user-registered-queue", e =>
                {
                    e.Bind("user-registered", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<UserRegisteredConsumer>(context);
                });
            });
        });
        
        return services;
    }
}