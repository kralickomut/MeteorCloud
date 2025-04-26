using MetadataService.Persistence.Entities;

namespace MetadataService.Persistence;

public interface IFileMetadataRepository
{
    Task CreateAsync(FileMetadata file, CancellationToken cancellationToken);
    Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<FileMetadata>> GetByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(FileMetadata file, CancellationToken cancellationToken);
}