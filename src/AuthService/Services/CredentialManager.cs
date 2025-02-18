using AuthService.Persistence;
using MeteorCloud.Caching.Abstraction;
using Newtonsoft.Json;

namespace AuthService.Services;

public class CredentialManager
{
    private readonly CredentialRepository _credentialRepository;
    private readonly ICacheService _cacheService;
    private readonly string _serviceCacheKey = "auth-service";

    public CredentialManager(CredentialRepository credentialRepository, ICacheService cacheService)
    {
        _credentialRepository = credentialRepository;
        _cacheService = cacheService;
    }
    
    public async Task<Credential?> GetCredentialsByEmailAsync(string email)
    {
        var cachedCredential = await _cacheService.GetAsync(_serviceCacheKey, "credential", email);
        
        if (cachedCredential != null)
        {
            return JsonConvert.DeserializeObject<Credential>(cachedCredential);
        }
        
        var credential = await _credentialRepository.GetCredentialByEmail(email);
        
        if (credential != null)
        {
            await _cacheService.SetAsync(_serviceCacheKey, "credential", email, JsonConvert.SerializeObject(credential), TimeSpan.FromMinutes(5));
        }
        
        return credential;
    }
    
    public async Task<int?> CreateCredentialsAsync(Credential credential)
    {
        var id = await _credentialRepository.CreateCredential(credential);
        
        if (id.HasValue)
        {
            await _cacheService.RemoveAsync(_serviceCacheKey, "credential", credential.Email);
        }
        
        return id;
    }
}