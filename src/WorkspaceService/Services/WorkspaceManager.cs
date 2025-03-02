using MassTransit;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.Workspace;
using WorkspaceService.Persistence;

namespace WorkspaceService.Services;

public class WorkspaceManager
{
    private readonly WorkspaceRepository _workspaceRepository;
    private readonly ICacheService _cacheService;
    private const string _serviceCacheKey = "workspace-service";
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly MSHttpClient _httpClient;

    public WorkspaceManager(WorkspaceRepository workspaceRepository, ICacheService cacheService, IPublishEndpoint publishEndpoint, MSHttpClient httpClient)
    {
        _workspaceRepository = workspaceRepository;
        _cacheService = cacheService;
        _publishEndpoint = publishEndpoint;
        _httpClient = httpClient;
    }

    public async Task<int?> CreateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        var url = MicroserviceEndpoints.UserService + $"/api/user/{workspace.OwnerId}";
        var response = await _httpClient.GetAsync<object>(url, cancellationToken);

        if (!response.Success)
        {
            return null;
        }
        
        var id = await _workspaceRepository.CreateWorkspaceAsync(workspace, cancellationToken);

        if (id.HasValue)
        {
            await _cacheService.RemoveAsync(_serviceCacheKey, "workspace", workspace.Id.ToString());
            await _publishEndpoint.Publish(new WorkspaceCreatedEvent(id.Value, workspace.OwnerId));
        }

        return id;
    }
    
    public async Task<bool> DeleteWorkspaceAsync(int id, CancellationToken cancellationToken)
    {
        var success = await _workspaceRepository.DeleteWorkspaceAsync(id, cancellationToken);

        if (success)
        {
            await _cacheService.RemoveAsync(_serviceCacheKey, "workspace", id.ToString());
            
            var usersInWorkspace = await GetUserIdsInWorkspaceAsync(id);
            await _publishEndpoint.Publish(new WorkspaceDeletedEvent(id, usersInWorkspace));
        }

        return success;
    }
    
    public async Task<IEnumerable<int>> GetUserIdsInWorkspaceAsync(int workspaceId)
    {
        var cachedUserIds = await _cacheService.GetAsync(_serviceCacheKey, "workspace-users", workspaceId.ToString());
        if (cachedUserIds != null)
        {
            return cachedUserIds.Split(',').Select(int.Parse);
        }
        
        var userIds = await _workspaceRepository.GetUserIdsInWorkspaceAsync(workspaceId);
        
        await _cacheService.SetAsync(_serviceCacheKey, "workspace-users", workspaceId.ToString(), string.Join(',', userIds), TimeSpan.FromMinutes(5));
        
        return userIds;
    }
}