using Dapper;
using MetadataService.Persistence.Entities;

namespace MetadataService.Persistence;

public class FileMetadataRepository : IFileMetadataRepository
{
    private readonly DapperContext _context;

    public FileMetadataRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(FileMetadata file, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO FileMetadata (Id, FileName, Path, WorkspaceId, Size, ContentType, UploadedAt, LastModifiedAt, UploadedBy, IsFolder)
            VALUES (@Id, @FileName, @Path, @WorkspaceId, @Size, @ContentType, @UploadedAt, @LastModifiedAt, @UploadedBy, @IsFolder);";

        await using var connection = await _context.CreateConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(sql, file, cancellationToken: cancellationToken));
    }

    public async Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT * FROM FileMetadata WHERE Id = @Id";

        await using var connection = await _context.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<FileMetadata>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<FileMetadata>> GetByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT * FROM FileMetadata WHERE WorkspaceId = @WorkspaceId";

        await using var connection = await _context.CreateConnectionAsync();
        return await connection.QueryAsync<FileMetadata>(new CommandDefinition(sql, new { WorkspaceId = workspaceId }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM FileMetadata WHERE Id = @Id";

        await using var connection = await _context.CreateConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }
    
    public async Task UpdateAsync(FileMetadata file, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE FileMetadata
            SET FileName = @FileName,
                Path = @Path,
                WorkspaceId = @WorkspaceId,
                Size = @Size,
                ContentType = @ContentType,
                UploadedAt = @UploadedAt,
                LastModifiedAt = @LastModifiedAt,
                UploadedBy = @UploadedBy
            WHERE Id = @Id;";

        await using var connection = await _context.CreateConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(sql, file, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM FileMetadata WHERE WorkspaceId = @WorkspaceId";

        await using var connection = await _context.CreateConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(sql, new { WorkspaceId = workspaceId }, cancellationToken: cancellationToken));
        
        return rowsAffected > 0;
    }
    
    public async Task<bool> DeleteFolderAsync(string folderPath, CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(folderPath))
    {
        Console.WriteLine("❌ Folder path is empty.");
        return false;
    }

    var segments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length == 0 || !int.TryParse(segments[0], out int workspaceId))
    {
        Console.WriteLine($"❌ Cannot extract workspaceId from path '{folderPath}'");
        return false;
    }

    var folderName = segments.Last(); // The folder we are deleting
    var parentPath = segments.Length > 1 
        ? string.Join('/', segments.Take(segments.Length - 1)) // Take everything except last
        : ""; // If only workspaceId is there, root

    const string sql = @"
        DELETE FROM FileMetadata
        WHERE WorkspaceId = @WorkspaceId
          AND (
              Path = @FolderPath
              OR Path LIKE @FolderPathPrefix
              OR (Path = @ParentPath AND FileName = @FolderName AND ContentType = 'folder')
          );
    ";

    await using var connection = await _context.CreateConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    try
    {
        int affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                WorkspaceId = workspaceId,
                FolderPath = string.Join('/', segments),                // '1/ASd'
                FolderPathPrefix = string.Join('/', segments) + "/%",   // '1/ASd/%'
                ParentPath = parentPath,                                // '1'
                FolderName = folderName                                 // 'ASd'
            },
            transaction: transaction,
            cancellationToken: cancellationToken
        ));

        await transaction.CommitAsync(cancellationToken);

        Console.WriteLine($"✅ Deleted {affectedRows} item(s) related to folder '{folderPath}' (WorkspaceId: {workspaceId})");

        return affectedRows > 0;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(cancellationToken);
        Console.WriteLine($"❌ Failed to delete folder '{folderPath}' in workspace {workspaceId}: {ex.Message}");
        return false;
    }
}
   
}