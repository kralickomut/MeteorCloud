using MassTransit;
using MeteorCloud.Caching.Abstraction;
using Newtonsoft.Json;
using UserService.Persistence;

namespace UserService.Services;

public class UserService
{
    private readonly UserRepository _userRepository;
    private readonly ICacheService _cache;
    private readonly string _serviceCacheKey = "user-service";
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UserService> _logger;
    
    public UserService(UserRepository userRepository, ICacheService cache, IPublishEndpoint publishEndpoint, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task<User?> GetUserByIdAsync(int id, CancellationToken? cancellationToken = null)
    {
        var cachedUser = await _cache.GetAsync(_serviceCacheKey, "user", id.ToString());
        if (cachedUser != null)
        {
            return JsonConvert.DeserializeObject<User>(cachedUser);
        }
        
        var user = await _userRepository.GetUserByIdAsync(id, cancellationToken);
        
        // Store in Redis
        if (user != null)
        {
            await _cache.SetAsync(_serviceCacheKey, "user", id.ToString(), JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(10));
        }
        
        return user;
    }
    
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        
        var user = await _userRepository.GetUserByEmailAsync(email);
        
        
        return user;
    }
    
    public async Task CreateUserAsync(User user)
    {
        var success = await _userRepository.CreateUserAsync(user);

        if (success)
        {
            await _cache.SetAsync(_serviceCacheKey, "user", user.Id.ToString(), JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(10));
        }
    }
    
    public async Task UpdateUserAsync(User user)
    {
        var success = await _userRepository.UpdateUserAsync(user);

        if (success)
        {
            await _cache.SetAsync(_serviceCacheKey, "user", user.Id.ToString(), JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(10));
        }
    }
    
    public async Task<bool> DecrementWorkspacesCountAsync(IEnumerable<int> userIds)
    {
        var result = await _userRepository.DecrementWorkspaceCountsAsync(userIds);
        
        if (result)
        {
            foreach (var userId in userIds)
            {
                await _cache.RemoveAsync(_serviceCacheKey, "user", userId.ToString());
            }
        }
        
        return result;
    }
    
}