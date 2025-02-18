using System.Diagnostics;
using System.Text;
using Dapper;
using MeteorCloud.Caching.Abstraction;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Npgsql;

namespace UserService.Persistence;

public class UserRepository
{
    private readonly DapperContext _context;
    
    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        // Fetch from database if not found in Redis
        using var connection = await _context.CreateConnectionAsync();
        const string query = "SELECT * FROM Users WHERE Id = @Id;";
        var user = await connection.QuerySingleOrDefaultAsync<User>(query, new { Id = id });

        return user;
    }
    
    
    // Get users with filter and pagination
    public async Task<IEnumerable<User>> GetUsersAsync(string? search, int page = 1, int pageSize = 10)
    {
        using var connection = await _context.CreateConnectionAsync();

        var query = new StringBuilder(@"
        SELECT Id, FirstName, LastName, Email, RegistrationDate, UpdatedOn 
        FROM Users WHERE 1=1
    ");

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(search))
        {
            query.Append(" AND (FirstName ILIKE @Search OR LastName ILIKE @Search OR Email ILIKE @Search)");
            parameters.Add("Search", $"%{search}%"); // Uses Indexed Search
        }

        query.Append(" ORDER BY Id DESC LIMIT @PageSize OFFSET @Offset");
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        var users = (await connection.QueryAsync<User>(query.ToString(), parameters)).ToList();
        
        return users;
    }
    
    
    public async Task<int?> CreateUserAsync(User user)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = @"
                INSERT INTO Users (FirstName, LastName, Email)
                VALUES (@FirstName, @LastName, @Email)
                RETURNING Id;";
        
        try
        {
            int userId = await connection.ExecuteScalarAsync<int>(query, user);
            return userId;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // Email duplicate
        {
            return null;
        }       
    }
    
    public async Task<bool> UpdateUserAsync(User user)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = @"
                UPDATE Users
                SET FirstName = @FirstName, LastName = @LastName, Email = @Email
                WHERE Id = @Id;";
        
        var affectedRows = await connection.ExecuteAsync(query, user);
        return affectedRows > 0;
    }
    
    public async Task<bool> DeleteUserAsync(int id)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = "DELETE FROM Users WHERE Id = @Id;";
        var affectedRows = await connection.ExecuteAsync(query, new { Id = id });
        return affectedRows > 0;
    }
}