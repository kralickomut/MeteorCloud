using MetadataService.Persistence;
using MetadataService.Persistence.Entities;
using MeteorCloud.Caching.Abstraction;
using Newtonsoft.Json;

namespace MetadataService.Services;

public class FileMetadataManager : IFileMetadataManager
{
    private readonly IFileMetadataRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<FileMetadataManager> _logger;
    private const string _serviceCacheKey = "metadata-service";

    public FileMetadataManager(
        IFileMetadataRepository repository,
        ICacheService cache,
        ILogger<FileMetadataManager> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
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
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await _repository.GetByIdAsync(id, cancellationToken);

        if (file != null)
        {
            await _repository.DeleteAsync(id, cancellationToken);
            await _cache.RemoveAsync(_serviceCacheKey, "file", id.ToString());
            await _cache.RemoveAsync(_serviceCacheKey, "workspace-files", file.WorkspaceId.ToString());
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
    }
}