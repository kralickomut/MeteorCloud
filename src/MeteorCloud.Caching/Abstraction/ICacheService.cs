namespace MeteorCloud.Caching.Abstraction;

public interface ICacheService
{
    Task<string?> GetAsync(string service, string objectType, string id);
    Task SetAsync(string service, string objectType, string id, string value, TimeSpan expiration);
    Task RemoveAsync(string service, string objectType, string? id = null);
}