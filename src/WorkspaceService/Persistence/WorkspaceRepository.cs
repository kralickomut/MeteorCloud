using System.Collections.Immutable;
using System.Data;
using Dapper;

namespace WorkspaceService.Persistence;

public class WorkspaceRepository
{
    private readonly DapperContext _context;
    private readonly ILogger<WorkspaceRepository> _logger;
    
    public WorkspaceRepository(DapperContext context, ILogger<WorkspaceRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<IEnumerable<Workspace>> GetWorkspacesByUserId(int userId)
    { 
        await using var connection = await _context.CreateConnectionAsync();
        
        const string query = "SELECT * FROM Workspaces WHERE OwnerId = @UserId;";
        var workspaces = await connection.QueryAsync<Workspace>(query, new { UserId = userId });
        
        return workspaces;
    }
    
    public async Task<int?> CreateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); // ✅ Stop early if canceled

        try
        {
            await using var connection = await _context.CreateConnectionAsync(); // ✅ Fully async connection
            await using var transaction = await connection.BeginTransactionAsync(); // ✅ Fully async transaction

            const string insertWorkspaceQuery = @"
            INSERT INTO Workspaces (Name, Description, OwnerId)
            VALUES (@Name, @Description, @OwnerId)
            RETURNING Id;
            ";
            
            var workspaceId = await connection.ExecuteScalarAsync<int>(
                insertWorkspaceQuery, workspace, transaction: transaction
            );

            if (workspaceId > 0)
            {
                const string insertRelationQuery = @"
                INSERT INTO WorkspaceUsers (WorkspaceId, UserId, Role)
                VALUES (@WorkspaceId, @OwnerId, @Role);
                ";

                await connection.ExecuteAsync(
                    insertRelationQuery,
                    new { WorkspaceId = workspaceId, OwnerId = workspace.OwnerId, Role = "owner" },
                    transaction: transaction
                );
            }

            await transaction.CommitAsync(); 
            return workspaceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workspace for user {OwnerId}", workspace.OwnerId);
            return null;
        }
    }
    
    public async Task<bool> DeleteWorkspaceAsync(int workspaceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var connection = await _context.CreateConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            // Delete from WorkspaceUsers first (due to FK constraint)
            const string deleteWorkspaceUsersQuery = @"
            DELETE FROM WorkspaceUsers WHERE WorkspaceId = @WorkspaceId;
        ";

            await connection.ExecuteAsync(
                deleteWorkspaceUsersQuery,
                new { WorkspaceId = workspaceId },
                transaction: transaction
            );

            // Delete from Workspaces table
            const string deleteWorkspaceQuery = @"
            DELETE FROM Workspaces WHERE Id = @WorkspaceId;
        ";

            var rowsAffected = await connection.ExecuteAsync(
                deleteWorkspaceQuery,
                new { WorkspaceId = workspaceId },
                transaction: transaction
            );

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return false; // Workspace not found
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workspace {WorkspaceId}", workspaceId);
            return false;
        }
    }
    
    public async Task<bool> UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var connection = await _context.CreateConnectionAsync();

            const string updateWorkspaceQuery = @"
            UPDATE Workspaces
            SET Name = @Name, Description = @Description
            WHERE Id = @Id;
        ";

            var rowsAffected = await connection.ExecuteAsync(
                updateWorkspaceQuery,
                new { workspace.Name, workspace.Description, workspace.Id }
            );

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workspace {WorkspaceId}", workspace.Id);
            return false;
        }
    }
    
    
    public async Task<bool> ChangeWorkspaceOwnerAsync(int workspaceId, int newOwnerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var connection = await _context.CreateConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            // Verify new owner exists in workspace
            const string userExistsQuery = @"
                SELECT COUNT(*) FROM WorkspaceUsers
                WHERE WorkspaceId = @WorkspaceId AND UserId = @NewOwnerId;
            ";

            var userExists = await connection.ExecuteScalarAsync<int>(
                userExistsQuery,
                new { WorkspaceId = workspaceId, NewOwnerId = newOwnerId },
                transaction: transaction
            );

            if (userExists == 0)
            {
                await transaction.RollbackAsync();
                return false; // New owner not found in workspace
            }

            // Get current owner
            const string getCurrentOwnerQuery = @"
                SELECT OwnerId FROM Workspaces WHERE Id = @WorkspaceId;
            ";

            var currentOwnerId = await connection.ExecuteScalarAsync<int>(
                getCurrentOwnerQuery,
                new { WorkspaceId = workspaceId },
                transaction: transaction
            );

            // Update Workspace owner
            const string updateOwnerQuery = @"
                UPDATE Workspaces SET OwnerId = @NewOwnerId WHERE Id = @WorkspaceId;
            ";

            await connection.ExecuteAsync(
                updateOwnerQuery,
                new { NewOwnerId = newOwnerId, WorkspaceId = workspaceId },
                transaction: transaction
            );

            // Update roles in WorkspaceUsers
            const string updateRoleQuery = @"
                UPDATE WorkspaceUsers
                SET Role = CASE
                    WHEN UserId = @NewOwnerId THEN 'owner'
                    WHEN UserId = @CurrentOwnerId THEN 'member'
                    ELSE Role
                END
                WHERE WorkspaceId = @WorkspaceId AND UserId IN (@NewOwnerId, @CurrentOwnerId);
            ";

            await connection.ExecuteAsync(
                updateRoleQuery,
                new { NewOwnerId = newOwnerId, CurrentOwnerId = currentOwnerId, WorkspaceId = workspaceId },
                transaction: transaction
            );

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing owner for workspace {WorkspaceId}", workspaceId);
            return false;
        }
    }
    
    public async Task<IEnumerable<int>> GetUserIdsInWorkspaceAsync(int workspaceId)
    {
        await using var connection = await _context.CreateConnectionAsync();
        
        const string query = "SELECT UserId FROM WorkspaceUsers WHERE WorkspaceId = @WorkspaceId;";
        var userIds = await connection.QueryAsync<int>(query, new { WorkspaceId = workspaceId });
        
        return userIds;
    }
    
    
    
}