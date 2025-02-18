using MeteorCloud.Caching.Abstraction;
using Newtonsoft.Json;
using UserService.Persistence;

namespace UserService.Services;

public class UserManager
{
    private readonly UserRepository _userRepository;
    private readonly ICacheService _cache;
    private readonly string _serviceCacheKey = "user-service";
    
    public UserManager(UserRepository userRepository, ICacheService cache)
    {
        _userRepository = userRepository;
        _cache = cache;
    }
    
    public async Task<User?> GetUserByIdAsync(int id)
    {
        var cachedUser = await _cache.GetAsync(_serviceCacheKey, "user", id.ToString());
        if (cachedUser != null)
        {
            return JsonConvert.DeserializeObject<User>(cachedUser);
        }
        
        var user = await _userRepository.GetUserByIdAsync(id);
        
        // Store in Redis
        if (user != null)
        {
            await _cache.SetAsync(_serviceCacheKey, "user", id.ToString(), JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(10));
        }
        
        return user;
    }
    
    public async Task<IEnumerable<User>> GetUsersAsync(string? search, int page = 1, int pageSize = 10)
    {
        var cachedUsers = await _cache.GetAsync(_serviceCacheKey, "users", $"{search ?? "none"}:{page}:{pageSize}");
        if (cachedUsers != null)
        {
            return JsonConvert.DeserializeObject<List<User>>(cachedUsers) ?? new List<User>();
        }
        
        var users = await _userRepository.GetUsersAsync(search, page, pageSize);
        
        await _cache.SetAsync(_serviceCacheKey, "users", $"{search ?? "none"}:{page}:{pageSize}", JsonConvert.SerializeObject(users), TimeSpan.FromMinutes(5));
        
        return users;
    }
    
    public async Task<int?> CreateUserAsync(User user)
    {
        var id = await _userRepository.CreateUserAsync(user);
        
        if (id.HasValue)
        {
            await _cache.RemoveAsync(_serviceCacheKey, "users");
        }
        
        return id;
    }
    
    public async Task<bool> UpdateUserAsync(User user)
    {
        var success = await _userRepository.UpdateUserAsync(user);
        
        if (!success)
        {
            throw new Exception("Failed to update user");
        }
        
        await _cache.SetAsync(_serviceCacheKey, "user", user.Id.ToString(), JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(10));
        await _cache.RemoveAsync(_serviceCacheKey, "users");
        
        return success;
    }
    
    public async Task<bool> DeleteUserAsync(int id)
    {
        var success = await _userRepository.DeleteUserAsync(id);
        
        if (!success)
        {
            throw new Exception("Failed to delete user");
        }
        
        await _cache.RemoveAsync(_serviceCacheKey, "user", id.ToString());
        await _cache.RemoveAsync(_serviceCacheKey, "users");
        
        return success;
    }
}