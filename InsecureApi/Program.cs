using Microsoft.Data.Sqlite;
using System.Text;
using System.Security.Cryptography;
using InsecureApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("*");
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

DatabaseHelper.InitializeDatabase();

Console.WriteLine("🚨 ATTENTION: Cette API est VOLONTAIREMENT INSÉCURISÉE pour les tests SAST 🚨");
Console.WriteLine("NE JAMAIS utiliser ce code en production !");

app.Run();


/*
// GET /users - Récupération de tous les utilisateurs avec injection SQL possible
app.MapGet("/users", (string? role) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    // INJECTION SQL VOLONTAIRE - Concaténation directe
    string query = "SELECT * FROM Users";
    if (!string.IsNullOrEmpty(role))
    {
        query += $" WHERE Role = '{role}'"; // VULNÉRABILITÉ : Injection SQL
    }
    
    using var command = new SqliteCommand(query, connection);
    using var reader = command.ExecuteReader();
    
    var users = new List<object>();
    while (reader.Read())
    {
        users.Add(new
        {
            Id = reader["Id"],
            Username = reader["Username"],
            Password = reader["Password"], // VULNÉRABILITÉ : Exposition des mots de passe
            Email = reader["Email"],
            CreditCard = reader["CreditCard"], // VULNÉRABILITÉ : Exposition des données sensibles
            SSN = reader["SSN"], // VULNÉRABILITÉ : Exposition du numéro de sécurité sociale
            ApiKey = reader["ApiKey"], // VULNÉRABILITÉ : Exposition des clés API
            Role = reader["Role"],
            IsActive = reader["IsActive"]
        });
    }
    
    return Results.Ok(users);
});

// GET /users/{id} - Récupération d'un utilisateur avec injection SQL
app.MapGet("/users/{id}", (string id) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    // INJECTION SQL VOLONTAIRE
    var query = $"SELECT * FROM Users WHERE Id = {id}";
    
    using var command = new SqliteCommand(query, connection);
    using var reader = command.ExecuteReader();
    
    if (reader.Read())
    {
        return Results.Ok(new
        {
            Id = reader["Id"],
            Username = reader["Username"],
            Password = reader["Password"], // VULNÉRABILITÉ : Exposition du mot de passe
            Email = reader["Email"],
            CreditCard = reader["CreditCard"],
            SSN = reader["SSN"],
            ApiKey = reader["ApiKey"],
            Role = reader["Role"],
            IsActive = reader["IsActive"]
        });
    }
    
    return Results.NotFound();
});

// POST /users - Création d'utilisateur sans validation
app.MapPost("/users", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    
    // VULNÉRABILITÉ : Désérialisation non sécurisée
    var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    
    if (userData == null) return Results.BadRequest("Invalid data");
    
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    // INJECTION SQL VOLONTAIRE dans l'insertion
    var insertQuery = $@"
        INSERT INTO Users (Username, Password, Email, CreditCard, SSN, ApiKey, Role, IsActive) 
        VALUES ('{userData.GetValueOrDefault("Username", "")}', 
                '{userData.GetValueOrDefault("Password", "")}', 
                '{userData.GetValueOrDefault("Email", "")}',
                '{userData.GetValueOrDefault("CreditCard", "")}',
                '{userData.GetValueOrDefault("SSN", "")}',
                '{userData.GetValueOrDefault("ApiKey", "")}',
                '{userData.GetValueOrDefault("Role", "user")}',
                1)";
    
    using var command = new SqliteCommand(insertQuery, connection);
    
    try
    {
        command.ExecuteNonQuery();
        
        // VULNÉRABILITÉ : Log des données sensibles
        Console.WriteLine($"User created: {json}");
        
        return Results.Ok(new { Message = "User created successfully", Data = userData });
    }
    catch (Exception ex)
    {
        // VULNÉRABILITÉ : Exposition des détails d'erreur
        return Results.BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
    }
});

// PUT /users/{id} - Mise à jour sans validation ni autorisation
app.MapPut("/users/{id}", async (string id, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    
    var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    if (userData == null) return Results.BadRequest("Invalid data");
    
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    // Construction dynamique de la requête - INJECTION SQL
    var setParts = new List<string>();
    foreach (var kvp in userData)
    {
        setParts.Add($"{kvp.Key} = '{kvp.Value}'");
    }
    
    var updateQuery = $"UPDATE Users SET {string.Join(", ", setParts)} WHERE Id = {id}";
    
    using var command = new SqliteCommand(updateQuery, connection);
    
    try
    {
        var rowsAffected = command.ExecuteNonQuery();
        
        if (rowsAffected > 0)
        {
            // VULNÉRABILITÉ : Log des données sensibles
            Console.WriteLine($"User {id} updated: {json}");
            return Results.Ok(new { Message = "User updated successfully" });
        }
        
        return Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
    }
});

// DELETE /users/{id} - Suppression sans autorisation
app.MapDelete("/users/{id}", (string id) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    // INJECTION SQL VOLONTAIRE
    var deleteQuery = $"DELETE FROM Users WHERE Id = {id}";
    
    using var command = new SqliteCommand(deleteQuery, connection);
    
    try
    {
        var rowsAffected = command.ExecuteNonQuery();
        
        if (rowsAffected > 0)
        {
            Console.WriteLine($"User {id} deleted");
            return Results.Ok(new { Message = "User deleted successfully" });
        }
        
        return Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
    }
});

// Endpoint de login ultra insécurisé
app.MapPost("/login", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    var loginData = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    
    if (loginData == null) return Results.BadRequest("Invalid data");
    
    var username = loginData.GetValueOrDefault("username", "");
    var password = loginData.GetValueOrDefault("password", "");
    
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    // INJECTION SQL VOLONTAIRE dans l'authentification
    var query = $"SELECT * FROM Users WHERE Username = '{username}' AND Password = '{password}'";
    
    using var command = new SqliteCommand(query, connection);
    using var reader2 = command.ExecuteReader();
    
    if (reader2.Read())
    {
        // VULNÉRABILITÉ : Token JWT généré de manière prévisible
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{DateTime.Now:yyyyMMddHHmmss}"));
        
        // VULNÉRABILITÉ : Log des credentials
        Console.WriteLine($"Login successful for {username} with password {password}");
        
        return Results.Ok(new
        {
            Token = token,
            User = new
            {
                Id = reader2["Id"],
                Username = reader2["Username"],
                Role = reader2["Role"],
                ApiKey = reader2["ApiKey"], // VULNÉRABILITÉ : Exposition de la clé API
                CreditCard = reader2["CreditCard"] // VULNÉRABILITÉ : Exposition des données sensibles
            }
        });
    }
    
    // VULNÉRABILITÉ : Information disclosure sur l'existence des utilisateurs
    return Results.Unauthorized();
});

// Endpoint d'administration sans protection
app.MapGet("/admin/database", () =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    // VULNÉRABILITÉ : Exposition de toute la structure de la base
    var tables = new List<object>();
    
    var tablesQuery = "SELECT name FROM sqlite_master WHERE type='table'";
    using var tablesCommand = new SqliteCommand(tablesQuery, connection);
    using var tablesReader = tablesCommand.ExecuteReader();
    
    while (tablesReader.Read())
    {
        var tableName = tablesReader["name"].ToString();
        tables.Add(new { TableName = tableName });
    }
    
    return Results.Ok(new { Tables = tables, ConnectionString = connectionString });
});

// Endpoint pour exécuter du SQL arbitraire
app.MapPost("/admin/sql", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    var sqlData = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    
    if (sqlData == null || !sqlData.ContainsKey("query"))
        return Results.BadRequest("Query required");
    
    var query = sqlData["query"];
    
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    try
    {
        // VULNÉRABILITÉ CRITIQUE : Exécution de SQL arbitraire
        using var command = new SqliteCommand(query, connection);
        
        if (query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            using var queryReader = command.ExecuteReader();
            var results = new List<Dictionary<string, object>>();
            
            while (queryReader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < queryReader.FieldCount; i++)
                {
                    row[queryReader.GetName(i)] = queryReader.GetValue(i);
                }
                results.Add(row);
            }
            
            return Results.Ok(new { Results = results });
        }
        else
        {
            var rowsAffected = command.ExecuteNonQuery();
            return Results.Ok(new { RowsAffected = rowsAffected });
        }
    }
    catch (Exception ex)
    {
        // VULNÉRABILITÉ : Exposition complète des erreurs
        return Results.BadRequest(new 
        { 
            Error = ex.Message, 
            StackTrace = ex.StackTrace,
            Query = query 
        });
    }
});

// VULNÉRABILITÉ : Endpoint de debug exposant des informations système
app.MapGet("/debug/info", () =>
{
    return Results.Ok(new
    {
        Environment = Environment.GetEnvironmentVariables(),
        MachineName = Environment.MachineName,
        UserName = Environment.UserName,
        OSVersion = Environment.OSVersion,
        ProcessorCount = Environment.ProcessorCount,
        WorkingSet = Environment.WorkingSet,
        ConnectionString = connectionString,
        DatabasePath = Path.GetFullPath("database.db")
    });
});
*/