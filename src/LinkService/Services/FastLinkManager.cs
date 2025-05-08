using System.Text.Json;
using LinkService.Persistence;
using LinkService.Persistence.Entities;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Shared.ApiResults;

namespace LinkService.Services;

public class FastLinkManager
{
    private readonly FastLinkRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<FastLinkManager> _logger;
    private const string _serviceCacheKey = "fastlink-service";

    public FastLinkManager(FastLinkRepository repository, ICacheService cache, ILogger<FastLinkManager> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<FastLink?> GetByTokenAsync(string token)
    {
        var cached = await _cache.GetAsync(_serviceCacheKey, "link", token);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<FastLink>(cached);
        }

        var link = await _repository.GetByTokenAsync(token);
        if (link != null)
        {
            await _cache.SetAsync(_serviceCacheKey, "link", token, JsonSerializer.Serialize(link), TimeSpan.FromMinutes(10));
        }

        return link;
    }

    public async Task<FastLink> CreateLinkAsync(FastLink link)
    {
        var created = await _repository.CreateAsync(link);
        await _cache.SetAsync(_serviceCacheKey, "link", link.Token, JsonSerializer.Serialize(created), TimeSpan.FromMinutes(10));
        await _cache.RemoveByPrefixAsync(_serviceCacheKey, "links-user", link.CreatedByUserId.ToString());
        _logger.LogInformation("Created FastLink: {Token} for file {FileId}", link.Token, link.FileId);
        return created;
    }

    public async Task<bool> DeleteLinkAsync(Guid fileId)
    {
        var result = await _repository.DeleteByFileIdAsync(fileId);
        if (!string.IsNullOrEmpty(result))
        {
            await _cache.RemoveAsync(_serviceCacheKey, "link", result);
            _logger.LogInformation("Deleted FastLink: {Token}", result);

            return true;
        }
        
        return false;
    }

    public async Task<bool> UpdateExpirationAsync(string token, DateTime newExpiration)
    {
        var result = await _repository.UpdateExpirationAsync(token, newExpiration);
        if (result)
        {
            await _cache.RemoveAsync(_serviceCacheKey, "link", token);
            _logger.LogInformation(" Updated expiration for FastLink: {Token}", token);
        }
        return result;
    }

    public async Task<PagedResult<FastLink>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10)
    {
        var allLinks = await _repository.GetByUserIdAsync(userId, page, pageSize);

        return allLinks;
    }

    public async Task IncrementAccessCountAsync(string token)
    {
        await _repository.IncrementAccessCountAsync(token);
        await _cache.RemoveAsync(_serviceCacheKey, "link", token);
        _logger.LogInformation(" Accessed FastLink: {Token}", token);
    }
}
