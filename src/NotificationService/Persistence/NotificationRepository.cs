using Dapper;

namespace EmailService.Persistence;

public class NotificationRepository
{
    private readonly DapperContext _context;

    public NotificationRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Notification>> GetAllByUserIdAsync(int userId)
    {
        var query = "SELECT * FROM Notifications WHERE UserId = @UserId ORDER BY CreatedAt DESC";

        using var connection = await _context.CreateConnectionAsync();
        return await connection.QueryAsync<Notification>(query, new { UserId = userId });
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        var query = "SELECT * FROM Notifications WHERE Id = @Id";

        using var connection = await _context.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Notification>(query, new { Id = id });
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        var query = @"
        INSERT INTO Notifications (UserId, Title, Message, IsRead, WorkspaceId, IsAccepted)
        VALUES (@UserId, @Title, @Message, @IsRead, @WorkspaceId, @IsAccepted)
        RETURNING *;
    ";

        using var connection = await _context.CreateConnectionAsync();
        return await connection.QuerySingleAsync<Notification>(query, notification);
    }
    
    public async Task<IEnumerable<Notification>> GetRecentNotificationsAsync(
        int userId,
        int skip = 0,
        int take = 10,
        CancellationToken? cancellationToken = null)
    {
        var query = @"
        SELECT *
        FROM Notifications
        WHERE UserId = @UserId
        ORDER BY IsRead ASC, CreatedAt DESC
        OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
    ";

        using var connection = await _context.CreateConnectionAsync();
        return await connection.QueryAsync<Notification>(query, new
        {
            UserId = userId,
            Skip = skip,
            Take = take
        });
    }

    public async Task MarkAsReadAsync(int id)
    {
        var query = "UPDATE Notifications SET IsRead = TRUE WHERE Id = @Id";

        using var connection = await _context.CreateConnectionAsync();
        await connection.ExecuteAsync(query, new { Id = id });
    }
    
    
    public async Task DeleteByWorkspaceIdAsync(int workspaceId)
    {
        var query = "DELETE FROM Notifications WHERE WorkspaceId = @WorkspaceId";

        using var connection = await _context.CreateConnectionAsync();
        await connection.ExecuteAsync(query, new { WorkspaceId = workspaceId });
    }
    
    public async Task UpdateWorkspaceInvitationResponseAsync(int userId, int workspaceId, bool isAccepted)
    {
        var query = "UPDATE Notifications SET IsAccepted = @IsAccepted WHERE UserId = @UserId AND WorkspaceId = @WorkspaceId AND IsAccepted IS NULL";

        using var connection = await _context.CreateConnectionAsync();
        await connection.ExecuteAsync(query, new { IsAccepted = isAccepted, UserId = userId, WorkspaceId = workspaceId });
    }
}