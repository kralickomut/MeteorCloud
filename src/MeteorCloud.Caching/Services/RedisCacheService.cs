using MeteorCloud.Caching.Abstraction;
using StackExchange.Redis;

namespace MeteorCloud.Caching.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    private static string GenerateCacheKey(string service, string objectType, string id)
    {
        return $"{service}:{objectType}:{id}"; 
    }

    public async Task<string?> GetAsync(string service, string objectType, string id)
    {
        var key = GenerateCacheKey(service, objectType, id);
        return await _database.StringGetAsync(key);
    }

    public async Task SetAsync(string service, string objectType, string id, string value, TimeSpan expiration)
    {
        var key = GenerateCacheKey(service, objectType, id);
        await _database.StringSetAsync(key, value, expiration);
    }

    public async Task RemoveAsync(string service, string objectType, string? id = null)
    {
        var pattern = id == null 
            ? $"{service}:{objectType}:*"  // Delete all related keys
            : $"{service}:{objectType}:{id}"; // Delete a specific key

        var endpoints = _database.Multiplexer.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _database.Multiplexer.GetServer(endpoint);
            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
            }
        }
    }
}