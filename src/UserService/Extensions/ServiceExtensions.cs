using System.Data;
using Dapper;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Caching.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Npgsql;
using StackExchange.Redis;
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
        
        return services;
    }
}