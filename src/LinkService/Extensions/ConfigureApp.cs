using LinkService.Persistence;

namespace LinkService.Extensions;

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