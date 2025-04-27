using System.Text;
using MetadataService.Models.Tree;
using MetadataService.Persistence;
using MetadataService.Persistence.Entities;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Communication;
using MeteorCloud.Shared.SharedDto.Users;
using Newtonsoft.Json;

namespace MetadataService.Services;

public class FileMetadataManager : IFileMetadataManager
{
    private readonly IFileMetadataRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<FileMetadataManager> _logger;
    private readonly MSHttpClient _httpClient;
    private const string _serviceCacheKey = "metadata-service";

    public FileMetadataManager(
        IFileMetadataRepository repository,
        ICacheService cache,
        ILogger<FileMetadataManager> logger,
        MSHttpClient httpClient)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cachedFile = await _cache.GetAsync(_serviceCacheKey, "file", id.ToString());

        if (cachedFile != null)
        {
            return JsonConvert.DeserializeObject<FileMetadata>(cachedFile);
        }

        var file = await _repository.GetByIdAsync(id, cancellationToken);

        if (file != null)
        {
            await _cache.SetAsync(
                _serviceCacheKey,
                "file",
                id.ToString(),
                JsonConvert.SerializeObject(file),
                TimeSpan.FromMinutes(10));
        }

        return file;
    }

    public async Task<IEnumerable<FileMetadata>> GetByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken = default)
    {
        var cachedFiles = await _cache.GetAsync(_serviceCacheKey, "workspace-files", workspaceId.ToString());

        if (cachedFiles != null)
        {
            return JsonConvert.DeserializeObject<IEnumerable<FileMetadata>>(cachedFiles)!;
        }

        var files = await _repository.GetByWorkspaceAsync(workspaceId, cancellationToken);

        if (files.Any())
        {
            await _cache.SetAsync(
                _serviceCacheKey,
                "workspace-files",
                workspaceId.ToString(),
                JsonConvert.SerializeObject(files),
                TimeSpan.FromMinutes(10));
        }

        return files;
    }

    public async Task CreateAsync(FileMetadata metadata, CancellationToken cancellationToken = default)
    {
        await _repository.CreateAsync(metadata, cancellationToken);

        // No need to cache single file immediately unless you expect instant reads.
        await _cache.RemoveAsync(_serviceCacheKey, "workspace-files", metadata.WorkspaceId.ToString());
        await _cache.RemoveAsync(_serviceCacheKey, "workspace-tree", metadata.WorkspaceId.ToString());
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await _repository.GetByIdAsync(id, cancellationToken);

        if (file != null)
        {
            await _repository.DeleteAsync(id, cancellationToken);
            await _cache.RemoveAsync(_serviceCacheKey, "file", id.ToString());
            await _cache.RemoveAsync(_serviceCacheKey, "workspace-files", file.WorkspaceId.ToString());
            await _cache.RemoveAsync(_serviceCacheKey, "workspace-tree", file.WorkspaceId.ToString());
        }
    }

    public async Task UpdateAsync(FileMetadata metadata, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(metadata, cancellationToken);

        await _cache.SetAsync(
            _serviceCacheKey,
            "file",
            metadata.Id.ToString(),
            JsonConvert.SerializeObject(metadata),
            TimeSpan.FromMinutes(10));

        await _cache.RemoveAsync(_serviceCacheKey, "workspace-files", metadata.WorkspaceId.ToString());
        await _cache.RemoveAsync(_serviceCacheKey, "workspace-tree", metadata.WorkspaceId.ToString());
    }

    public async Task<FolderNode?> BuildTreeAsync(int workspaceId, CancellationToken cancellationToken = default)
    {
        // Try to load from cache first
        var cachedTree = await _cache.GetAsync(_serviceCacheKey, "workspace-tree", workspaceId.ToString());
        if (cachedTree != null)
        {
            return JsonConvert.DeserializeObject<FolderNode>(cachedTree)!;
        }

        // Otherwise build fresh
        FolderNode root = new FolderNode("root");
        var files = await GetByWorkspaceAsync(workspaceId, cancellationToken);

        if (files is null || !files.Any())
        {
            // 💡 Nothing inside, just return root directly without fetching users
            await _cache.SetAsync(
                _serviceCacheKey,
                "workspace-tree",
                workspaceId.ToString(),
                JsonConvert.SerializeObject(root),
                TimeSpan.FromMinutes(10)
            );

            return root;
        }

        // Otherwise continue normally
        var userIds = files.Select(f => f.UploadedBy).ToHashSet();

        var url = MicroserviceEndpoints.UserService.GetUsersBulk();
        var userResponse = await _httpClient.PostAsync<object, IEnumerable<UserModel>>(url, new { userIds }, cancellationToken);

        if (!userResponse.Success)
        {
            _logger.LogWarning("Failed to fetch users for workspace {WorkspaceId}: {Error}", workspaceId, userResponse.Message);
            return null; // 🚨 (Optional: maybe here you could also just return root instead of null)
        }

        var userDictionary = userResponse.Data?.ToDictionary(u => u.Id, u => u) ?? new Dictionary<int, UserModel>();

        foreach (var file in files)
        {
            var foldersInPath = file.Path.Split("/", StringSplitOptions.RemoveEmptyEntries);

            // Skip the first part (workspaceId) if present
            if (foldersInPath.Length > 0 && foldersInPath[0] == file.WorkspaceId.ToString())
            {
                foldersInPath = foldersInPath.Skip(1).ToArray();
            }

            var currentNode = root;
            foreach (var folderName in foldersInPath)
            {
                var existingFolder = currentNode.Folders.FirstOrDefault(f => f.Name == folderName);
                if (existingFolder == null)
                {
                    existingFolder = new FolderNode(folderName)
                    {
                        UploadedAt = null,
                        UploadedByName = null
                    };
                    currentNode.Folders.Add(existingFolder);
                }
                currentNode = existingFolder;
            }

            if (file.IsFolder)
            {
                var existingFolder = currentNode.Folders.FirstOrDefault(f => f.Name == file.FileName);
                if (existingFolder == null)
                {
                    currentNode.Folders.Add(new FolderNode(file.FileName)
                    {
                        UploadedAt = file.UploadedAt,
                        UploadedByName = userDictionary.TryGetValue(file.UploadedBy, out var user)
                            ? user.Name
                            : "Unknown User"
                    });
                }
            }
            else
            {
                currentNode.Files.Add(new FileNode
                {
                    Id = file.Id,
                    Name = file.FileName,
                    UploadedAt = file.UploadedAt,
                    UploadedByName = userDictionary.TryGetValue(file.UploadedBy, out var user)
                        ? user.Name
                        : "Unknown User",
                    ContentType = file.ContentType,
                    Size = file.Size
                });
            }
        }

        // Cache the final tree
        await _cache.SetAsync(
            _serviceCacheKey,
            "workspace-tree",
            workspaceId.ToString(),
            JsonConvert.SerializeObject(root),
            TimeSpan.FromMinutes(10)
        );

        return root;
    }

    public async Task<bool> DeleteByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeleteByWorkspaceAsync(workspaceId, cancellationToken);
        
        if (result)
        {
            await _cache.RemoveAsync(_serviceCacheKey, "workspace-files", workspaceId.ToString());
            await _cache.RemoveAsync(_serviceCacheKey, "workspace-tree", workspaceId.ToString());
        }

        return true;
    }
    
    public async Task<bool> DeleteFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeleteFolderAsync(path, cancellationToken);

        if (result)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0 && int.TryParse(segments[0], out int workspaceId))
            {
                await _cache.RemoveAsync(_serviceCacheKey, "workspace-files", workspaceId.ToString());
                await _cache.RemoveAsync(_serviceCacheKey, "workspace-tree", workspaceId.ToString());
            }
            else
            {
                _logger.LogWarning("Cannot extract workspaceId from path '{Path}' to invalidate cache.", path);
            }
        }

        return result;
    }
}