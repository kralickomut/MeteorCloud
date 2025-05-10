using Dapper;
using Npgsql;

namespace UserService.Persistence;

public class UserRepository
{
    private readonly DapperContext _context;

    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(int id, CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        await using var connection = await _context.CreateConnectionAsync();
        const string query = "SELECT * FROM Users WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<User>(query, new { Id = id });
    }
    
    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        await using var connection = await _context.CreateConnectionAsync();
        const string query = "SELECT * FROM Users WHERE Email = @Email;";
        return await connection.QuerySingleOrDefaultAsync<User>(query, new { Email = email });
    }

    public async Task<bool> CreateUserAsync(User user, CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        await using var connection = await _context.CreateConnectionAsync();

        const string query = @"
            INSERT INTO Users (Id, Name, Email, Description, InTotalWorkspaces, UpdatedAt)
            VALUES (@Id, @Name, @Email, @Description, @InTotalWorkspaces, @UpdatedAt);
        ";

        try
        {
            var rows = await connection.ExecuteAsync(query, user);
            return rows == 1;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // Duplicate key (e.g., email or id)
        {
            return false;
        }
    }
    
    public async Task<bool> UpdateUserAsync(User user, CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        await using var connection = await _context.CreateConnectionAsync();

        const string query = @"
            UPDATE Users
            SET Name = @Name,
                Email = @Email,
                Description = @Description,
                InTotalWorkspaces = @InTotalWorkspaces,
                LastLogin = @LastLogin,
                UpdatedAt = @UpdatedAt,
                ProfilePictureUrl = @ProfilePictureUrl
            WHERE Id = @Id;
        ";

        var rows = await connection.ExecuteAsync(query, user);
        return rows == 1;
    }
    
    
    public async Task<bool> DecrementWorkspaceCountsAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userIds == null || !userIds.Any())
            return false;

        await using var connection = await _context.CreateConnectionAsync();

        const string query = @"
        UPDATE Users
        SET InTotalWorkspaces = InTotalWorkspaces - 1
        WHERE Id = ANY(@UserIds);
    ";

        var affectedRows = await connection.ExecuteAsync(query, new { UserIds = userIds.ToArray() });
        return affectedRows == userIds.Count();
    }
    
}