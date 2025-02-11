using System.Data;
using Dapper;
using UserService.Application.Abstraction;
using UserService.Domain.Models;

namespace UserService.Infrastructure.Repository;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;
    
    public UserRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<User?> GetUserById(int id)
    {
        const string query = "SELECT * FROM Users WHERE Id = @id";
        return await _connection.QueryFirstOrDefaultAsync<User>(query, new { Id = id });
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        const string query = "SELECT * FROM Users WHERE Email = @email";
        return await _connection.QueryFirstOrDefaultAsync<User>(query, new { Email = email });
    }

    public async Task<User?> GetUserByUsername(string username)
    {
        const string query = "SELECT * FROM Users WHERE Username = @username";
        return await _connection.QueryFirstOrDefaultAsync<User>(query, new { Username = username });
    }

    public async Task<IEnumerable<User>> GetAllUsers()
    {
        const string query = "SLECET * FROM Users";
        return await _connection.QueryAsync<User>(query);
    }

    public async Task<User> CreateUser(User user)
    {
        const string query = @"
            INSERT INTO Users (Username, Email, PasswordHash, CreatedOn, UpdatedOn, UserMetadataId)
            VALUES (@Username, @Email, @PasswordHash, @CreatedOn, @UpdatedOn, @UserMetadataId);
            SELECT last_insert_rowid();";
        var id = await _connection.ExecuteScalarAsync<int>(query, user);
        user.Id = id;
        return user;
    }

    public async Task<User> UpdateUser(User user)
    {
        const string query = @"
            UPDATE Users
            SET Username = @Username, Email = @Email, PasswordHash = @PasswordHash, CreatedOn = @CreatedOn, UpdatedOn = @UpdatedOn, UserMetadataId = @UserMetadataId
            WHERE Id = @Id";
        await _connection.ExecuteAsync(query, user);
        return user;
    }
    
    public async Task<User> DeleteUser(User user)
    {
        const string query = "DELETE FROM Users WHERE Id = @Id";
        await _connection.ExecuteAsync(query, user);
        return user;
    }
}