using Dapper;
using Npgsql;

namespace MetadataService.Persistence;

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

            // Connect to the actual microservice database and ensure tables exist
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            const string createMetadataTable = @"
                CREATE TABLE IF NOT EXISTS FileMetadata (
                    Id UUID PRIMARY KEY,                             -- Unique FileId (UUID is the PG equivalent)
                    FileName VARCHAR(255) NOT NULL,                  -- Original file name
                    Path VARCHAR(500) NOT NULL,                      -- Folder path like 'Design System'
                    WorkspaceId INTEGER NOT NULL,                    -- Workspace foreign key
                    Size BIGINT NOT NULL,                            -- File size in bytes
                    ContentType VARCHAR(100) NOT NULL,               -- MIME type
                    UploadedAt TIMESTAMP WITHOUT TIME ZONE NOT NULL, -- Upload timestamp
                    LastModifiedAt TIMESTAMP WITHOUT TIME ZONE,      -- Last modified
                    UploadedBy INTEGER NOT NULL                      -- Uploader user ID
                    IsFolder BOOLEAN NOT NULL DEFAULT FALSE,       -- Is this a folder?
                );
            ";

            await connection.ExecuteAsync(createMetadataTable);

            _logger.LogInformation("✅ Tables initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error initializing database: {Message}", ex.Message);
        }
    }
}