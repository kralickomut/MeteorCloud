using MassTransit;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.Workspace;
using Newtonsoft.Json;
using Npgsql;
using WorkspaceService.Persistence;
using WorkspaceService.Persistence.Entities;

namespace WorkspaceService.Services;

public class WorkspaceManager
{
    private readonly WorkspaceRepository _workspaceRepository;
    private readonly ICacheService _cache;
    private const string _serviceCacheKey = "workspace-service";
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WorkspaceManager> _logger;
    private readonly MSHttpClient _httpClient;

    public WorkspaceManager(
        WorkspaceRepository workspaceRepository, 
        ICacheService cache, 
        IPublishEndpoint publishEndpoint,
        MSHttpClient httpClient,
        ILogger<WorkspaceManager> logger)
    {
        _workspaceRepository = workspaceRepository;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<Workspace?> GetWorkspaceByIdAsync(int id)
    {
        var cachedWorkspace = await _cache.GetAsync(_serviceCacheKey, "workspace", id.ToString());
        
        if (cachedWorkspace != null)
        {
            return JsonConvert.DeserializeObject<Workspace>(cachedWorkspace);
        }
        
        var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(id);
        
        if (workspace != null)
        {
            await _cache.SetAsync(_serviceCacheKey, "workspace", id.ToString(), JsonConvert.SerializeObject(workspace), TimeSpan.FromMinutes(10));
        }

        return workspace;
    }
    
    public async Task<Workspace?> GetWorkspaceByIdIncludeUsersAsync(int id)
    {
        var cachedWorkspace = await _cache.GetAsync(_serviceCacheKey, "workspace-users", id.ToString());
        
        if (cachedWorkspace != null)
        {
            return JsonConvert.DeserializeObject<Workspace>(cachedWorkspace);
        }
        
        var workspace = await _workspaceRepository.GetWorkspaceByIdIncludeUsersAsync(id);
        
        if (workspace != null)
        {
            await _cache.SetAsync(_serviceCacheKey, "workspace-users", id.ToString(), JsonConvert.SerializeObject(workspace), TimeSpan.FromMinutes(10));
        }

        return workspace;
    }
    
    public async Task<List<Workspace>> GetUserWorkspacesAsync(int userId, int page, int pageSize, CancellationToken? cancellationToken = null)
    {
        var cachedWorkspaces = await _cache.GetAsync(_serviceCacheKey, "user-workspaces", userId.ToString());
        
        if (cachedWorkspaces != null)
        {
            return JsonConvert.DeserializeObject<List<Workspace>>(cachedWorkspaces);
        }
        
        var workspaces = await _workspaceRepository.GetUserWorkspacesAsync(userId, page, pageSize, cancellationToken);
        
        if (workspaces != null || workspaces.Count != 0)
        {
            await _cache.SetAsync(_serviceCacheKey, "user-workspaces", userId.ToString(), JsonConvert.SerializeObject(workspaces), TimeSpan.FromMinutes(10));
        }

        return workspaces;
    }
    
    public async Task<Workspace?> CreateWorkspaceAsync(Workspace workspace, CancellationToken? cancellationToken = null)
    {
        var newWorkspace = await _workspaceRepository.CreateWorkspaceAsync(workspace, cancellationToken);
        
        if (newWorkspace != null)
        {
            await _cache.RemoveAsync(_serviceCacheKey, "user-workspaces", newWorkspace.OwnerId.ToString());
            await _cache.SetAsync(_serviceCacheKey, "workspace", newWorkspace.Id.ToString(), 
                JsonConvert.SerializeObject(newWorkspace), TimeSpan.FromMinutes(10));
        }
        
        await _publishEndpoint.Publish(new WorkspaceCreatedEvent(newWorkspace.Id, workspace.OwnerId));

        return newWorkspace;
    }

    public async Task<WorkspaceInvitation?> InviteToWorkspaceAsync(int workspaceId, string email, int invitedByUserId,
        CancellationToken? cancellationToken = null)
    {
        var invitation =
                await _workspaceRepository.CreateInvitationAsync(workspaceId, email, invitedByUserId, cancellationToken);

        return invitation;
    }

    public async Task<(int workspaceId, IEnumerable<int> userIds)?> DeleteWorkspaceAsync(int workspaceId, CancellationToken? cancellationToken = null)
    {
        var workspace = await GetWorkspaceByIdIncludeUsersAsync(workspaceId);

        if (workspace is null) return null;

        var userIds = await _workspaceRepository.DeleteWorkspaceAsync(workspaceId, cancellationToken);

        if (userIds is not null)
        {
            await _cache.RemoveAsync(_serviceCacheKey, "user-workspaces", workspace.OwnerId.ToString());
            await _cache.RemoveAsync(_serviceCacheKey, "workspace", workspaceId.ToString());
            await _cache.RemoveAsync(_serviceCacheKey, "workspace-users", workspaceId.ToString());
            
            foreach (var user in workspace.Users)
            {
                await _cache.RemoveAsync(_serviceCacheKey, "user-workspaces", user.UserId.ToString());
            }
        }

        return (workspace.Id, userIds);
    }

    public async Task<WorkspaceInvitation?> GetInvitationByEmailAsync(string email)
    {
        var invitation = await _workspaceRepository.GetInvitationByEmailAsync(email);

        return invitation;
    }
    
    public async Task<bool> IsUserInWorkspaceAsync(int userId, int workspaceId, CancellationToken? cancellationToken = null)
    {
        var isUserInWorkspace = await _workspaceRepository.IsUserInWorkspaceAsync(userId, workspaceId, cancellationToken);

        return isUserInWorkspace;
    }
    
}
    

