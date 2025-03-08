using MassTransit;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Messaging.Events;
using Newtonsoft.Json;
using UserService.Persistence;

namespace UserService.Services;

public class UserManager
{
    private readonly UserRepository _userRepository;
    private readonly ICacheService _cache;
    private readonly string _serviceCacheKey = "user-service";
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UserManager> _logger;
    
    public UserManager(UserRepository userRepository, ICacheService cache, IPublishEndpoint publishEndpoint, ILogger<UserManager> logger)
    {
        _userRepository = userRepository;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken)
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
    
    public async Task<IEnumerable<User>> GetUsersAsync(string? search, CancellationToken cancellationToken, int page = 1, int pageSize = 10)
    {
        var cachedUsers = await _cache.GetAsync(_serviceCacheKey, "users", $"{search ?? "none"}:{page}:{pageSize}");
        if (cachedUsers != null)
        {
            return JsonConvert.DeserializeObject<List<User>>(cachedUsers) ?? new List<User>();
        }
        
        var users = await _userRepository.GetUsersAsync(search, cancellationToken, page, pageSize);
        
        await _cache.SetAsync(_serviceCacheKey, "users", $"{search ?? "none"}:{page}:{pageSize}", JsonConvert.SerializeObject(users), TimeSpan.FromMinutes(5));
        
        return users;
    }
    
    public async Task<int?> CreateUserAsync(User user, CancellationToken cancellationToken)
    {
        var id = await _userRepository.CreateUserAsync(user, cancellationToken);
        
        if (id.HasValue)
        {
            await _cache.RemoveAsync(_serviceCacheKey, "users");
            await _publishEndpoint.Publish(new UserRegisteredEvent(id.Value, user.Email, user.FirstName, user.LastName));
        }
        
        return id;
    }
    
    public async Task<bool> UpdateUserAsync(User user, CancellationToken cancellationToken)
    {
        var success = await _userRepository.UpdateUserAsync(user, cancellationToken);
        
        if (!success)
        {
            return false;
        }
        
        await _cache.SetAsync(_serviceCacheKey, "user", user.Id.ToString(), JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(10));
        await _cache.RemoveAsync(_serviceCacheKey, "users");

        var updateEvent = new UserUpdatedEvent(user.Id, user.FirstName, user.LastName, user.Email);
        
        await _publishEndpoint.Publish(updateEvent);
        
        _logger.LogInformation("User updated event published: {0}", updateEvent.Id);
        
        return success;
    }
    
    public async Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken)
    {
        var success = await _userRepository.DeleteUserAsync(id, cancellationToken);
        
        if (!success)
        {
            throw new Exception("Failed to delete user");
        }
        
        await _cache.RemoveAsync(_serviceCacheKey, "user", id.ToString());
        await _cache.RemoveAsync(_serviceCacheKey, "users");

        await _publishEndpoint.Publish(new UserDeletedEvent(id));
        
        _logger.LogInformation("User deleted event published: {UserId}", id);
        
        return success;
    }


    public async Task IncrementUserWorkspaceCountAsync(int userId)
    {
        var result = await _userRepository.IncrementTotalWorkspacesAsync(userId);
        
        if (!result)
        {
            throw new Exception("Failed to increment total workspaces");
        }
        
        await _cache.RemoveAsync(_serviceCacheKey, "user", userId.ToString());
    }

    public async Task DecrementWorkspaceCountForUsers(IEnumerable<int> userIds)
    {
        var result = await _userRepository.DecrementWorkspacesCountForUsers(userIds);
        
        if (!result)
        {
            throw new Exception("Failed to decrement workspaces count for users");
        }
        
        foreach (var userId in userIds)
        {
            await _cache.RemoveAsync(_serviceCacheKey, "user", userId.ToString());
        }
        
        await _cache.RemoveAsync(_serviceCacheKey, "users");
    }
}