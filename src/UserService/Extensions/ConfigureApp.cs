using Microsoft.OpenApi.Models;
using UserService.Features;
using UserService.Persistence;

namespace UserService.Extensions;

public static class ConfigureApp
{
     public static async Task InitializeDatabaseAsync(this WebApplication app)
     {
        using (var scope = app.Services.CreateScope())
        {
            var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await dbInitializer.InitializeDatabaseAsync();
        }
     }
}