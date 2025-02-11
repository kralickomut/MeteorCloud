using Microsoft.Data.Sqlite;

namespace UserService.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static void Initialize(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
       
        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
        pragmaCommand.ExecuteNonQuery();
        
        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS UserMetadata (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Color TEXT NOT NULL,
                    FileHierarchy TEXT NOT NULL,
                    HasConfirmedEmail INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,               
                    Username TEXT NOT NULL,
                    Email TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    CreatedOn DATETIME NOT NULL,
                    UpdatedOn DATETIME NULL,
                    UserMetadataId INTEGER NULL
                )";
        //                     UserMetadataId INTEGER NOT NULL FOREIGN KEY REFERENCES UserMetadata(Id)

        using var command = connection.CreateCommand();
        command.CommandText = createTableQuery;
        command.ExecuteNonQuery();
    }
}
