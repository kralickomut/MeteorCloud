using Dapper;
using Npgsql;

namespace WorkspaceService.Persistence;

public class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly string _adminConnectionString;
    private readonly string _databaseName;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _logger = logger;

        // Extract database name from connection string
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        _databaseName = builder.Database;

        // Create an admin connection string without the database (to connect to default `postgres`)
        builder.Database = "postgres";
        _adminConnectionString = builder.ToString();
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("🔄 Checking if database '{Database}' exists...", _databaseName);

            // Connect to PostgreSQL default database to check/create our database
            await using var adminConnection = new NpgsqlConnection(_adminConnectionString);
            await adminConnection.OpenAsync();

            var existsQuery = "SELECT 1 FROM pg_database WHERE datname = @DatabaseName;";
            var databaseExists =
                await adminConnection.ExecuteScalarAsync<int?>(existsQuery, new { DatabaseName = _databaseName });

            if (databaseExists != 1)
            {
                _logger.LogInformation("⚡ Database '{Database}' does not exist. Creating now...", _databaseName);
                await adminConnection.ExecuteAsync($"CREATE DATABASE \"{_databaseName}\";");
                _logger.LogInformation("✅ Database '{Database}' created successfully.", _databaseName);
            }
            else
            {
                _logger.LogInformation("✅ Database '{Database}' already exists.", _databaseName);
            }

            // Now connect to the actual microservice database and ensure tables exist
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string createWorkspacesTable = @"
                CREATE TABLE IF NOT EXISTS Workspaces (
                    Id SERIAL PRIMARY KEY,
                    OwnerId INT NOT NULL,  -- Acts as a reference to Users (external service)
                    Name VARCHAR(255) NOT NULL,
                    Description TEXT NULL,
                    CreatedOn TIMESTAMP NOT NULL DEFAULT NOW(),
                    LastUploadOn TIMESTAMP NOT NULL DEFAULT NOW()
                );

                CREATE INDEX IF NOT EXISTS idx_workspaces_owner_id ON Workspaces (OwnerId);
            ";

                        const string createWorkspaceUsersTable = @"
                CREATE TABLE IF NOT EXISTS WorkspaceUsers (
                    Id SERIAL PRIMARY KEY,
                    WorkspaceId INT NOT NULL,  -- References Workspaces table
                    UserId INT NOT NULL,  -- Acts as a reference to Users (external service)
                    Role VARCHAR(50) NOT NULL DEFAULT 'member',
                    CONSTRAINT unique_workspace_user UNIQUE (WorkspaceId, UserId)
                );

                -- Indexes for fast lookups
                CREATE INDEX IF NOT EXISTS idx_workspace_users_user_id ON WorkspaceUsers (UserId);
                CREATE INDEX IF NOT EXISTS idx_workspace_users_workspace_id ON WorkspaceUsers (WorkspaceId);
            ";

            await connection.ExecuteAsync(createWorkspacesTable);
            await connection.ExecuteAsync(createWorkspaceUsersTable);
            _logger.LogInformation("✅ Tables initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error initializing database: {Message}", ex.Message);
        }
    }
}