using MetadataService.Persistence.Entities;

namespace MetadataService.Services;

public interface IFileMetadataManager
{
    Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<FileMetadata>> GetByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken = default);
    Task CreateAsync(FileMetadata metadata, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(FileMetadata metadata, CancellationToken cancellationToken = default);
}