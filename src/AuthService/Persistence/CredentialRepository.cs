using Dapper;
using Npgsql;

namespace AuthService.Persistence;

public class CredentialRepository
{
    private readonly DapperContext _context;
    
    public CredentialRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<Credential?> GetCredentialByEmail(string email)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = "SELECT * FROM Credentials WHERE Email = @Email;";
        var credential = await connection.QuerySingleOrDefaultAsync<Credential>(query, new { Email = email });

        return credential;
    }
    
    public async Task<Credential?> GetCredentialsByUserId(int userId)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = "SELECT * FROM Credentials WHERE UserId = @UserId;";
        var credential = await connection.QuerySingleOrDefaultAsync<Credential>(query, new { UserId = userId });

        return credential;
    }
    
    public async Task<int?> CreateCredential(Credential credential)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = "INSERT INTO Credentials (Email, PasswordHash, UserId) VALUES (@Email, @PasswordHash, @UserId) RETURNING Id;";
        var id = await connection.ExecuteScalarAsync<int>(query, credential);

        return id;
    }
    
    public async Task<bool> UpdateCredential(Credential credential)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = "UPDATE Credentials SET Email = @Email, PasswordHash = @PasswordHash, UserId = @UserId WHERE Id = @Id;";
        var rowsAffected = await connection.ExecuteAsync(query, credential);

        return rowsAffected > 0;
    }
    
    public async Task<string?> DeleteCredential(int userId)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = "DELETE FROM Credentials WHERE UserId = @UserId RETURNING Email;";

        var deletedEmail = await connection.ExecuteScalarAsync<string?>(query, new { UserId = userId });

        return deletedEmail; // Returns the deleted email, or null if not found
    }
}