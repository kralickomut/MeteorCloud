using MassTransit;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.Workspace;
using MeteorCloud.Shared.ApiResults;
using MeteorCloud.Shared.SharedDto.Users;
using Newtonsoft.Json;
using Npgsql;
using WorkspaceService.Models;
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
            await _cache.SetAsync(_serviceCacheKey, "workspace", id.ToString(), JsonConvert.SerializeObject(workspace),
                TimeSpan.FromMinutes(10));
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
            await _cache.SetAsync(_serviceCacheKey, "workspace-users", id.ToString(),
                JsonConvert.SerializeObject(workspace), TimeSpan.FromMinutes(10));
        }

        return workspace;
    }

    public async Task<List<Workspace>> GetUserWorkspacesAsync(
        int userId,
        int page,
        int pageSize,
        double? sizeFrom = null,
        double? sizeTo = null,
        string? sortByDate = null, // "asc" / "desc"
        string? sortByFiles = null, // "asc" / "desc"
        CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        var isUnfiltered = !sizeFrom.HasValue && !sizeTo.HasValue &&
                           string.IsNullOrEmpty(sortByDate) &&
                           string.IsNullOrEmpty(sortByFiles);

        var cacheKey = $"{userId}-page-{page}-size-{pageSize}";

        if (isUnfiltered)
        {
            var cached = await _cache.GetAsync(_serviceCacheKey, "user-workspaces", cacheKey);
            if (cached != null)
            {
                return JsonConvert.DeserializeObject<List<Workspace>>(cached)!;
            }
        }

        var workspaces = await _workspaceRepository.GetUserWorkspacesAsync(
            userId, page, pageSize, sizeFrom, sizeTo, sortByDate, sortByFiles, cancellationToken);

        if (isUnfiltered && workspaces is { Count: > 0 })
        {
            await _cache.SetAsync(
                _serviceCacheKey,
                "user-workspaces",
                cacheKey,
                JsonConvert.SerializeObject(workspaces),
                TimeSpan.FromMinutes(10));
        }

        return workspaces;
    }

    public async Task<Workspace?> CreateWorkspaceAsync(Workspace workspace, CancellationToken? cancellationToken = null)
    {
        var newWorkspace = await _workspaceRepository.CreateWorkspaceAsync(workspace, cancellationToken);

        if (newWorkspace != null)
        {
            await _cache.RemoveByPrefixAsync(_serviceCacheKey, "user-workspaces", $"{newWorkspace.OwnerId}-page-");
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

    public async Task<(int workspaceId, IEnumerable<int> userIds)?> DeleteWorkspaceAsync(int workspaceId,
        CancellationToken? cancellationToken = null)
    {
        var workspace = await GetWorkspaceByIdIncludeUsersAsync(workspaceId);

        if (workspace is null) return null;

        var userIds = await _workspaceRepository.DeleteWorkspaceAsync(workspaceId, cancellationToken);

        if (userIds is not null)
        {
            await _cache.RemoveByPrefixAsync(_serviceCacheKey, "user-workspaces", $"{workspace.OwnerId}-page-");
            await _cache.RemoveAsync(_serviceCacheKey, "workspace", workspaceId.ToString());
            await _cache.RemoveAsync(_serviceCacheKey, "workspace-users", workspaceId.ToString());

            foreach (var user in workspace.Users)
            {
                await _cache.RemoveByPrefixAsync(_serviceCacheKey, "user-workspaces", $"{user.UserId}-page-");
            }
        }

        return (workspace.Id, userIds);
    }

    public async Task<WorkspaceInvitation?> GetInvitationByEmailAsync(string email)
    {
        var invitation = await _workspaceRepository.GetInvitationByEmailAsync(email);

        return invitation;
    }

    public async Task<WorkspaceInvitation?> GetInvitationByTokenAsync(Guid token,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _workspaceRepository.GetWorkspaceInvitationByTokenAsync(token, cancellationToken);

        return invitation;
    }

    public async Task<bool> IsUserInWorkspaceAsync(int userId, int workspaceId,
        CancellationToken? cancellationToken = null)
    {
        var isUserInWorkspace =
            await _workspaceRepository.IsUserInWorkspaceAsync(userId, workspaceId, cancellationToken);

        return isUserInWorkspace;
    }

    public async Task<(bool success, int? userId, int? workspaceId)> RespondToInvitationAsync(Guid token, bool accept,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();


        var invitation = await _workspaceRepository.GetInvitationByTokenAsync(token, cancellationToken);

        if (invitation is null)
        {
            return (false, null, null);
        }

        var url = MicroserviceEndpoints.UserService.GetUserByEmail(invitation.Email);
        var response = await _httpClient.GetAsync<int>(url);

        int userId = response.Data;

        // Sets new user in workspace + updates status
        // Or sets status as declined
        var success = await _workspaceRepository.RespondToInvitationAsync(token, accept, userId, cancellationToken);

        if (success)
        {
            if (accept)
            {
                await _cache.RemoveByPrefixAsync(_serviceCacheKey, "user-workspaces", $"{userId}-page-");
                await _cache.RemoveAsync(_serviceCacheKey, "workspace-users", invitation.WorkspaceId.ToString());

                _logger.LogInformation(
                    "User {UserId} accepted invitation to workspace {WorkspaceId}", userId, invitation.WorkspaceId);
            }

            await _publishEndpoint.Publish(
                new WorkspaceInvitationResponseEvent(userId, invitation.WorkspaceId, accept));
        }

        return (success, userId, invitation.WorkspaceId);
    }


    public async Task<bool> UpdateWorkspaceAsync(Workspace workspace, CancellationToken? cancellationToken = null)
    {
        var updatedWorkspace = await _workspaceRepository.UpdateWorkspaceAsync(workspace, cancellationToken);

        if (updatedWorkspace != null)
        {
            await _cache.RemoveByPrefixAsync(_serviceCacheKey, "user-workspaces", $"{workspace.OwnerId}-page-");
            await _cache.SetAsync(_serviceCacheKey, "workspace", workspace.Id.ToString(),
                JsonConvert.SerializeObject(updatedWorkspace), TimeSpan.FromMinutes(10));
        }

        return updatedWorkspace != null;
    }

    public async Task<bool> ChangeUserRoleAsync(int workspaceId, int userId, int role, int clientId,
        CancellationToken? cancellationToken = null)
    {
        var workspace = await _workspaceRepository.GetWorkspaceByIdIncludeUsersAsync(workspaceId);
        var requestor = workspace.Users.First(x => x.UserId == clientId);

        if (role == (int)Role.Owner && requestor.Role != Role.Owner)
        {
            return false;
        }

        if (role == (int)Role.Manager && requestor.Role != Role.Owner)
        {
            return false;
        }

        var url = MicroserviceEndpoints.UserService.GetUserById(userId);
        var response = await _httpClient.GetAsync<UserModel>(url);

        if (!response.Success)
        {
            return false;
        }

        var user = response.Data;

        var result = await _workspaceRepository.ChangeUserRoleAsync(workspaceId,
            userId,
            role,
            (int)Role.Owner == role ? user.Name : null,
            cancellationToken);

        if (result)
        {
            await _cache.RemoveByPrefixAsync(_serviceCacheKey, "user-workspaces", $"{userId}-page-");
            await _cache.RemoveAsync(_serviceCacheKey, "workspace-users", workspaceId.ToString());
        }

        return result;
    }


    public async Task<bool> RemoveUserFromWorkspaceAsync(int workspaceId, int userId,
        CancellationToken? cancellationToken = null)
    {
        var result = await _workspaceRepository.RemoveUserFromWorkspaceAsync(workspaceId, userId, cancellationToken);

        if (result)
        {
            await _publishEndpoint.Publish(new WorkspaceUserRemovedEvent(workspaceId, userId));
            _logger.LogInformation(
                "User {UserId} removed from workspace {WorkspaceId}", userId, workspaceId);
            
            await _cache.RemoveByPrefixAsync(_serviceCacheKey, "user-workspaces", $"{userId}-page-");
            await _cache.RemoveAsync(_serviceCacheKey, "workspace-users", workspaceId.ToString());
        }

        return result;
    }

    public async Task<PagedResult<WorkspaceInvitation>> GetWorkspaceInvitationsAsync(int workspaceId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var invitations = await _workspaceRepository.GetWorkspaceInvitationsAsync(
            workspaceId,
            page,
            pageSize, 
            cancellationToken);
        
        return invitations;
    }
}
    

