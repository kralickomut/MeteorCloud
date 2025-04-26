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
}