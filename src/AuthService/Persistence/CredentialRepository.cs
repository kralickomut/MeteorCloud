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
        const string query = @"
            SELECT 
                UserId,
                Email,
                PasswordHash,
                IsVerified,
                VerificationCode,
                VerificationExpiry,
                CreatedAt
            FROM Credentials
            WHERE Email = @Email;
        ";

        return await connection.QuerySingleOrDefaultAsync<Credential>(query, new { Email = email });
    }
    
    public async Task<Credential?> GetCredentialById(int id)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = @"
            SELECT 
                UserId,
                Email,
                PasswordHash,
                IsVerified,
                VerificationCode,
                VerificationExpiry,
                CreatedAt
            FROM Credentials
            WHERE UserId = @UserId;
        ";

        return await connection.QuerySingleOrDefaultAsync<Credential>(query, new { UserId = id });
    }
    
    public async Task<Credential?> GetCredentialByResetPasswordToken(Guid token)
    {
        using var connection = await _context.CreateConnectionAsync();
        const string query = @"
            SELECT 
                UserId,
                Email,
                PasswordHash,
                IsVerified,
                VerificationCode,
                VerificationExpiry,
                CreatedAt
            FROM Credentials
            WHERE ResetPasswordToken = @ResetPassWordToken;
        ";

        return await connection.QuerySingleOrDefaultAsync<Credential>(query, new { ResetPasswordToken = token });
    }

    public async Task<Credential?> CreateCredential(Credential credential, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = await _context.CreateConnectionAsync();

        const string query = @"
        INSERT INTO Credentials 
            (Email, PasswordHash, IsVerified, VerificationCode, VerificationExpiry)
        VALUES 
            (@Email, @PasswordHash, @IsVerified, @VerificationCode, @VerificationExpiry)
        RETURNING *;
    ";

        try
        {
            var createdCredential = await connection.QuerySingleOrDefaultAsync<Credential>(query, credential);
            return createdCredential;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // Unique constraint (Email)
        {
            return null;
        }
    }
    
    public async Task<bool> UpdateCredential(Credential credential, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = await _context.CreateConnectionAsync();

        const string query = @"
            UPDATE Credentials
            SET PasswordHash = @PasswordHash,
                IsVerified = @IsVerified,
                VerificationCode = @VerificationCode,
                VerificationExpiry = @VerificationExpiry,
                ResetPasswordToken = @ResetPasswordToken
            WHERE UserId = @UserId;
        ";

        var rowsAffected = await connection.ExecuteAsync(query, credential);
        return rowsAffected > 0;
    }

    public async Task<Credential?> UpdateVerificationStatusAsync(string verificationCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = await _context.CreateConnectionAsync();

        const string query = @"
            UPDATE Credentials
            SET IsVerified = TRUE,
                VerificationCode = NULL,
                VerificationExpiry = NULL
            WHERE VerificationCode = @VerificationCode
              AND VerificationExpiry > NOW()
            RETURNING 
                UserId, Email, PasswordHash, IsVerified, VerificationCode, VerificationExpiry, CreatedAt;
        ";

        return await connection.QuerySingleOrDefaultAsync<Credential>(query, new
        {
            VerificationCode = verificationCode
        }) ?? null;
    }
}