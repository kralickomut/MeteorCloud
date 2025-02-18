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
    
    public async Task<int?> CreateCredential(Credential credential)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = "INSERT INTO Credentials (Email, PasswordHash, UserId) VALUES (@Email, @PasswordHash, @UserId) RETURNING Id;";
        var id = await connection.ExecuteScalarAsync<int>(query, credential);

        return id;
    }
}