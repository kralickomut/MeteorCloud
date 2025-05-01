using System.Data;
using Dapper;
using MeteorCloud.Shared.ApiResults;
using Npgsql;
using WorkspaceService.Models;
using WorkspaceService.Persistence.Entities;

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

    public async Task<Workspace?> GetWorkspaceByIdAsync(int workspaceId)
    {
        await using var connection = await _context.CreateConnectionAsync();

        const string query = "SELECT * FROM Workspaces WHERE Id = @WorkspaceId;";
        return await connection.QuerySingleOrDefaultAsync<Workspace>(query, new { WorkspaceId = workspaceId });
    }

    public async Task<Workspace?> GetWorkspaceByIdIncludeUsersAsync(int workspaceId)
    {
        await using var connection = await _context.CreateConnectionAsync();

        const string workspaceQuery = "SELECT * FROM Workspaces WHERE Id = @WorkspaceId;";
        const string usersQuery = "SELECT * FROM WorkspaceUsers WHERE WorkspaceId = @WorkspaceId;";

        var workspace = await connection.QuerySingleOrDefaultAsync<Workspace>(
            workspaceQuery, new { WorkspaceId = workspaceId });

        if (workspace is null)
            return null;

        var users = (await connection.QueryAsync<WorkspaceUser>(
            usersQuery, new { WorkspaceId = workspaceId })).ToList();

        workspace.Users = users;
        return workspace;
    }

    public async Task<List<Workspace>> GetUserWorkspacesAsync(
        int userId,
        int page,
        int pageSize,
        double? sizeFrom = null,
        double? sizeTo = null,
        string? sortByDate = null,     // "asc" / "desc"
        string? sortByFiles = null,    // "asc" / "desc"
        CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        await using var connection = await _context.CreateConnectionAsync();

        // Base query
        var sql = @"
            SELECT *
            FROM Workspaces w
            WHERE EXISTS (
                SELECT 1 FROM WorkspaceUsers wu
                WHERE wu.WorkspaceId = w.Id AND wu.UserId = @UserId
            )
        ";

        var filters = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        // Add size filter
        if (sizeFrom.HasValue)
        {
            filters.Add("w.SizeInGB >= @SizeFrom");
            parameters.Add("SizeFrom", sizeFrom.Value);
        }

        if (sizeTo.HasValue)
        {
            filters.Add("w.SizeInGB <= @SizeTo");
            parameters.Add("SizeTo", sizeTo.Value);
        }

        if (filters.Any())
        {
            sql += " AND " + string.Join(" AND ", filters);
        }

        // Sorting logic (multi-sort supported)
        var sortClauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(sortByDate) && (sortByDate == "asc" || sortByDate == "desc"))
            sortClauses.Add($"w.CreatedOn {sortByDate.ToUpper()}");

        if (!string.IsNullOrWhiteSpace(sortByFiles) && (sortByFiles == "asc" || sortByFiles == "desc"))
            sortClauses.Add($"w.TotalFiles {sortByFiles.ToUpper()}");

        if (sortClauses.Count > 0)
            sql += " ORDER BY " + string.Join(", ", sortClauses);
        else
            sql += " ORDER BY w.CreatedOn DESC"; // Default fallback

        sql += " LIMIT @PageSize OFFSET @Offset;";

        var result = await connection.QueryAsync<Workspace>(sql, parameters);
        return result.ToList();
    }
    
    public async Task<WorkspaceInvitation?> GetWorkspaceInvitationByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        await using var connection = await _context.CreateConnectionAsync();

        const string query = @"
            SELECT * FROM WorkspaceInvitations
            WHERE Token = @Token;
        ";

        return await connection.QuerySingleOrDefaultAsync<WorkspaceInvitation>(query, new { Token = token });
    }

    public async Task<Workspace?> CreateWorkspaceAsync(Workspace workspace, CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        try
        {
            await using var connection = await _context.CreateConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            const string insertWorkspaceQuery = @"
                INSERT INTO Workspaces (Name, Description, OwnerId, OwnerName, Status, SizeInGB, TotalFiles, CreatedOn, LastUploadOn)
                VALUES (@Name, @Description, @OwnerId, @OwnerName, @Status, @SizeInGB, @TotalFiles, @CreatedOn, @LastUploadOn)
                RETURNING *;
            ";

            var createdWorkspace = await connection.QuerySingleOrDefaultAsync<Workspace>(
                insertWorkspaceQuery,
                new
                {
                    workspace.Name,
                    workspace.Description,
                    workspace.OwnerId,
                    workspace.OwnerName,
                    workspace.Status,
                    workspace.SizeInGB,
                    workspace.TotalFiles,
                    workspace.CreatedOn,
                    workspace.LastUploadOn
                },
                transaction: transaction
            );

            if (createdWorkspace is not null)
            {
                const string insertOwnerRelationQuery = @"
                    INSERT INTO WorkspaceUsers (WorkspaceId, UserId, Role)
                    VALUES (@WorkspaceId, @UserId, @Role);
                ";

                await connection.ExecuteAsync(
                    insertOwnerRelationQuery,
                    new
                    {
                        WorkspaceId = createdWorkspace.Id,
                        UserId = createdWorkspace.OwnerId,
                        Role = (int)Role.Owner
                    },
                    transaction: transaction
                );
            }

            await transaction.CommitAsync();
            return createdWorkspace;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workspace for owner {OwnerId}", workspace.OwnerId);
            return null;
        }
    }

    /// <summary>
    /// Returns null when the user is already a member of the workspace
    /// Throws exception when the user has already been invited
    /// </summary>
    /// <param name="workspaceId"></param>
    /// <param name="email"></param>
    /// <param name="invitedByUserId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<WorkspaceInvitation?> CreateInvitationAsync(int workspaceId, string email, int invitedByUserId, CancellationToken? ct = null)
    {
        ct?.ThrowIfCancellationRequested();

        var normalizedEmail = email.ToLowerInvariant().Trim();
        await using var connection = await _context.CreateConnectionAsync();
        
        // 1. Check if there's an existing invitation in status 'Pending'
        const string existingInviteQuery = @"
            SELECT Status
            FROM WorkspaceInvitations
            WHERE WorkspaceId = @WorkspaceId AND LOWER(Email) = @Email
            ORDER BY CreatedOn DESC
            LIMIT 1;
        ";

        var lastInviteStatus = await connection.QueryFirstOrDefaultAsync<string>(existingInviteQuery, new
        {
            WorkspaceId = workspaceId,
            Email = normalizedEmail
        });

        if (lastInviteStatus == "Pending")
        {
            _logger.LogInformation("An invitation is already pending for {Email} in workspace {WorkspaceId}. Skipping.", normalizedEmail, workspaceId);
            return null;
        }

        // 2. Insert new invitation
        var invitation = new WorkspaceInvitation
        {
            WorkspaceId = workspaceId,
            Email = normalizedEmail,
            InvitedByUserId = invitedByUserId,
            Token = Guid.NewGuid(),
            Status = "Pending",
            CreatedOn = DateTime.UtcNow
        };

        const string insertQuery = @"
            INSERT INTO WorkspaceInvitations
                (WorkspaceId, Email, InvitedByUserId, Token, Status, CreatedOn)
            VALUES
                (@WorkspaceId, @Email, @InvitedByUserId, @Token, @Status, @CreatedOn)
            RETURNING Id;
        ";

        try
        {
            var id = await connection.ExecuteScalarAsync<int>(insertQuery, invitation);
            invitation.Id = id;
            return invitation;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning("Invitation already exists for workspace {WorkspaceId} and email {Email}", workspaceId, normalizedEmail);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invitation for workspace {WorkspaceId}", workspaceId);
            return null;
        }
    }
    
    /// <summary>
    /// Returning userIds of users that were part of the workspace
    /// </summary>
    /// <param name="workspaceId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<int>?> DeleteWorkspaceAsync(int workspaceId, CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        try
        {
            await using var connection = await _context.CreateConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            // Get all users that were part of the workspace (before deleting!)
            const string getUserIdsQuery = @"
            SELECT UserId FROM WorkspaceUsers
            WHERE WorkspaceId = @WorkspaceId;
        ";

            var userIds = (await connection.QueryAsync<int>(
                getUserIdsQuery,
                new { WorkspaceId = workspaceId },
                transaction
            )).ToList();

            // Delete users
            const string deleteUsersQuery = "DELETE FROM WorkspaceUsers WHERE WorkspaceId = @WorkspaceId;";
            await connection.ExecuteAsync(deleteUsersQuery, new { WorkspaceId = workspaceId }, transaction);

            // Optionally delete invitations if you use them
            const string deleteInvitationsQuery = "DELETE FROM WorkspaceInvitations WHERE WorkspaceId = @WorkspaceId;";
            await connection.ExecuteAsync(deleteInvitationsQuery, new { WorkspaceId = workspaceId }, transaction);

            // Delete workspace
            const string deleteWorkspaceQuery = "DELETE FROM Workspaces WHERE Id = @WorkspaceId;";
            await connection.ExecuteAsync(deleteWorkspaceQuery, new { WorkspaceId = workspaceId }, transaction);

            await transaction.CommitAsync();

            return userIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workspace {WorkspaceId}", workspaceId);
            return null;
        }
    }
    
    
    public async Task<Workspace?> UpdateWorkspaceAsync(Workspace workspace, CancellationToken? ct = null)
    {
        ct?.ThrowIfCancellationRequested();

        try
        {
            await using var connection = await _context.CreateConnectionAsync();

            const string query = @"
            UPDATE Workspaces
            SET Name = @Name,
                Description = @Description,
                UpdatedOn = @UpdatedOn,
                LastUploadOn = @LastUploadOn,
                SizeInGB = @SizeInGB,
                TotalFiles = @TotalFiles
            WHERE Id = @Id;
        ";

            var rowsAffected = await connection.ExecuteAsync(query, workspace);

            if (rowsAffected == 0)
                return null;

            // Re-fetch updated entity
            const string fetchQuery = @"SELECT * FROM Workspaces WHERE Id = @Id;";
            var updated = await connection.QuerySingleOrDefaultAsync<Workspace>(fetchQuery, new { workspace.Id });
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workspace {WorkspaceId}", workspace.Id);
            return null;
        }
    }
    
    public async Task<WorkspaceInvitation?> GetInvitationByEmailAsync(string email, CancellationToken? ct = null)
    {
        ct?.ThrowIfCancellationRequested();

        await using var connection = await _context.CreateConnectionAsync();

        const string query = @"
            SELECT * FROM WorkspaceInvitations
            WHERE Email = @Email;
        ";

        return await connection.QuerySingleOrDefaultAsync<WorkspaceInvitation>(query, new { Email = email });
    }
    
    
    public async Task<bool> IsUserInWorkspaceAsync(int userId, int workspaceId, CancellationToken? ct = null)
    {
        ct?.ThrowIfCancellationRequested();

        const string query = @"
        SELECT EXISTS (
            SELECT 1
            FROM WorkspaceUsers
            WHERE WorkspaceId = @WorkspaceId AND UserId = @UserId
        );
    ";

        await using var connection = await _context.CreateConnectionAsync();

        var isMember = await connection.ExecuteScalarAsync<bool>(query, new
        {
            WorkspaceId = workspaceId,
            UserId = userId
        });

        return isMember;
    }
    
    public async Task<WorkspaceInvitation?> GetInvitationByTokenAsync(Guid token, CancellationToken ct)
    {
        const string query = @"SELECT * FROM WorkspaceInvitations WHERE Token = @Token";
        await using var connection = await _context.CreateConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<WorkspaceInvitation>(query, new { Token = token });
    }

    public async Task<bool> RespondToInvitationAsync(Guid token, bool accept, int userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        await using var connection = await _context.CreateConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // 1. Get the invitation
            const string getInvitationQuery = @"
                SELECT * FROM WorkspaceInvitations 
                WHERE Token = @Token FOR UPDATE;
            ";

            var invitation = await connection.QueryFirstOrDefaultAsync<WorkspaceInvitation>(
                getInvitationQuery,
                new { Token = token },
                transaction
            );

            if (invitation is null || invitation.Status != "Pending")
            {
                return false; // Not found or already accepted/declined
            }

            if (accept)
            {
                var role = Role.Guest; // or whatever logic you use to decide the role

                const string addUserQuery = @"
                    INSERT INTO WorkspaceUsers (WorkspaceId, UserId, Role)
                    VALUES (@WorkspaceId, @UserId, @Role);
                ";

                await connection.ExecuteAsync(
                    addUserQuery,
                    new
                    {
                        WorkspaceId = invitation.WorkspaceId,
                        UserId = userId,
                        Role = (int)role, // 💡 enum cast to int here
                        JoinedOn = DateTime.UtcNow
                    },
                    transaction
                );
            }

            // 3. Update invitation status
            const string updateStatusQuery = @"
                UPDATE WorkspaceInvitations
                SET Status = @Status,
                    AcceptedByUserId = @AcceptedByUserId
                WHERE Token = @Token;
            ";

            await connection.ExecuteAsync(
                updateStatusQuery,
                new
                {
                    Token = token,
                    Status = accept ? "Accepted" : "Declined",
                    AcceptedByUserId = userId
                },
                transaction
            );

            await transaction.CommitAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "❌ Error responding to workspace invitation for token {Token}", token);
            return false;
        }
    }
    
    
    public async Task<bool> ChangeUserRoleAsync(int workspaceId, int userId, int role, string? userName = null, CancellationToken? ct = null)
    {
        ct?.ThrowIfCancellationRequested();

        await using var connection = await _context.CreateConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. If promoting to owner, first demote current owner
            if (role == (int)Role.Owner)
            {
                const string demoteOldOwnerQuery = @"
                    UPDATE WorkspaceUsers
                    SET Role = @ManagerRole
                    WHERE WorkspaceId = @WorkspaceId AND Role = @OwnerRole AND UserId != @UserId;
                ";

                await connection.ExecuteAsync(demoteOldOwnerQuery, new
                {
                    WorkspaceId = workspaceId,
                    ManagerRole = (int)Role.Manager,
                    OwnerRole = (int)Role.Owner,
                    UserId = userId // don't demote the new owner
                }, transaction);
            }

            // 2. Update the user's role
            const string updateRoleQuery = @"
                UPDATE WorkspaceUsers
                SET Role = @Role
                WHERE WorkspaceId = @WorkspaceId AND UserId = @UserId;
            ";

            var rowsAffected = await connection.ExecuteAsync(updateRoleQuery, new
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                Role = role
            }, transaction);

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // 3. If setting new owner, update the workspace table owner fields
            if (role == (int)Role.Owner && !string.IsNullOrWhiteSpace(userName))
            {
                const string updateWorkspaceQuery = @"
                    UPDATE Workspaces
                    SET OwnerId = @UserId,
                        OwnerName = @UserName
                    WHERE Id = @WorkspaceId;
                ";

                await connection.ExecuteAsync(updateWorkspaceQuery, new
                {
                    WorkspaceId = workspaceId,
                    UserId = userId,
                    UserName = userName
                }, transaction);
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user role for WorkspaceId {WorkspaceId}, UserId {UserId}", workspaceId, userId);
            await transaction.RollbackAsync();
            return false;
        }
    }
    
    public async Task<bool> RemoveUserFromWorkspaceAsync(int workspaceId, int userId, CancellationToken? ct = null)
    {
        ct?.ThrowIfCancellationRequested();

        await using var connection = await _context.CreateConnectionAsync();
        const string query = @"
            DELETE FROM WorkspaceUsers
            WHERE WorkspaceId = @WorkspaceId AND UserId = @UserId;
        ";

        var rowsAffected = await connection.ExecuteAsync(query, new
        {
            WorkspaceId = workspaceId,
            UserId = userId
        });

        return rowsAffected > 0;
    }
    
    public async Task<PagedResult<WorkspaceInvitation>> GetWorkspaceInvitationsAsync(
        int workspaceId, int page, int pageSize, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await using var connection = await _context.CreateConnectionAsync();

        const string dataQuery = @"
        SELECT * FROM WorkspaceInvitations
        WHERE WorkspaceId = @WorkspaceId
        ORDER BY CreatedOn DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
    ";

        const string countQuery = @"
        SELECT COUNT(*) FROM WorkspaceInvitations
        WHERE WorkspaceId = @WorkspaceId;
    ";

        var parameters = new
        {
            WorkspaceId = workspaceId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var items = (await connection.QueryAsync<WorkspaceInvitation>(dataQuery, parameters)).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new { WorkspaceId = workspaceId });

        return new PagedResult<WorkspaceInvitation>
        {
            Items = items,
            TotalCount = totalCount
        };
    }
    
}