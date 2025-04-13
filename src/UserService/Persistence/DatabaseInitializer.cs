using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UserService.Persistence;

public class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly string _adminConnectionString;
    private readonly string _databaseName;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("Default")!;
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
            var databaseExists = await adminConnection.ExecuteScalarAsync<int?>(existsQuery, new { DatabaseName = _databaseName });

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

            const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INT PRIMARY KEY,  -- Provided by AuthService, NOT auto-incremented
                    Name VARCHAR(100) NOT NULL,
                    Email VARCHAR(255) UNIQUE NOT NULL,
                    Description TEXT DEFAULT '',
                    InTotalWorkspaces INT NOT NULL DEFAULT 0,
                    RegistrationDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    LastLoginDate TIMESTAMP DEFAULT NULL,
                    UpdatedAt TIMESTAMP
                );";

            await connection.ExecuteAsync(createTableQuery);

            _logger.LogInformation("Users table checked/created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error initializing database: {Message}", ex.Message);
            throw;
        }
    }
}