using MeteorCloud.API.Validation.Auth;

namespace MeteorCloud.API.Extensions;

public static class ServiceExtensions
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<RegistrationValidator>();
    }
}