using AuthService.Consumers;
using AuthService.Features.Auth;
using AuthService.Features.Credentials;
using AuthService.Persistence;
using AuthService.Services;
using MassTransit;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Caching.Services;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace AuthService.Extensions;

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

        services.AddScoped<CredentialRepository>();
        services.AddScoped<CredentialManager>();

        services.AddSingleton<GetCredentialsByEmailValidator>();
        services.AddScoped<GetCredentialsByEmailHandler>();
        
        services.AddSingleton<CreateCredentialsValidator>();
        services.AddScoped<CreateCredentialsHandler>();
        
        services.AddSingleton<LoginValidator>();
        services.AddScoped<LoginHandler>();

        services.AddSingleton<TokenService>();
        
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
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth Service API", Version = "v1" });
        });

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddConsumer<UserUpdatedConsumer>();
            busConfigurator.AddConsumer<UserDeletedConsumer>();
            
            busConfigurator.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                rabbitCfg.ReceiveEndpoint("user-updated-queue", e =>
                {
                    e.Bind("users");
                    e.ConfigureConsumer<UserUpdatedConsumer>(context);
                });
                
                rabbitCfg.ReceiveEndpoint("user-deleted-queue", e =>
                {
                    e.Bind("users");
                    e.ConfigureConsumer<UserDeletedConsumer>(context);
                });
                
            });
        });
        
        return services;
    }
}