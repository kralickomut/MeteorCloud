using MetadataService.Models.Tree;
using MetadataService.Persistence.Entities;

namespace MetadataService.Services;

public interface IFileMetadataManager
{
    Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<FileMetadata>> GetByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken = default);
    Task CreateAsync(FileMetadata metadata, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(FileMetadata metadata, CancellationToken cancellationToken = default);
    
    Task<FolderNode?> BuildTreeAsync(int workspaceId, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteFolderAsync(string path, CancellationToken cancellationToken = default);
}