using System.Data;
using Npgsql;
using Polly;
using Polly.Retry;

namespace WorkspaceService.Persistence;

public class DapperContext
{
    private readonly string _connectionString;
    private readonly AsyncRetryPolicy _retryPolicy;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // Retry policy: Retry 3 times with exponential backoff
        _retryPolicy = Policy
            .Handle<NpgsqlException>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
    }

    public async Task<NpgsqlConnection> CreateConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await _retryPolicy.ExecuteAsync(() => connection.OpenAsync());
        return connection; 
    }
}