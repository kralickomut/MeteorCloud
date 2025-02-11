using System.Data;
using Microsoft.Data.Sqlite;
using UserService.Application.Abstraction;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Repository;

namespace UserService.API.Configuration;

public static class ConfigurationSettings
{
    public static void AddProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));
        
        DatabaseInitializer.Initialize(connectionString!);
        
        services.AddScoped<IUserRepository, UserRepository>();

    }
}