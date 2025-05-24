using Microsoft.Data.Sqlite;

namespace InsecureApi;

public static class DatabaseHelper{
    public const string connectionString = "Data Source=database.db;";
    public const string dbPassword = "admin123"; // VULNÉRABILITÉ : Mot de passe hardcodé inutilisé

// Initialisation de la base avec des données sensibles
    public static void InitializeDatabase()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Création de table avec des données sensibles stockées en clair
        var createTableQuery = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY,
            Username TEXT,
            Password TEXT,
            Email TEXT,
            CreditCard TEXT,
            SSN TEXT,
            ApiKey TEXT,
            Role TEXT,
            IsActive INTEGER
        );";

        using var command = new SqliteCommand(createTableQuery, connection);
        command.ExecuteNonQuery();

        // Insertion de données de test avec mots de passe en clair
        var insertQuery = @"
        INSERT OR IGNORE INTO Users (Id, Username, Password, Email, CreditCard, SSN, ApiKey, Role, IsActive) 
        VALUES 
        (1, 'admin', 'admin123', 'admin@company.com', '4532-1234-5678-9012', '123-45-6789', 'sk_live_12345abcdef', 'admin', 1),
        (2, 'user', 'password', 'user@company.com', '4532-9876-5432-1098', '987-65-4321', 'sk_test_67890ghijkl', 'user', 1);";

        using var insertCommand = new SqliteCommand(insertQuery, connection);
        insertCommand.ExecuteNonQuery();
    }
}