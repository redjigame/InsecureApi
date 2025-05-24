using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace InsecureApi.Controllers;

// VULNÉRABILITÉ : Controller sans authentification ni autorisation
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // VULNÉRABILITÉ : Injection de dépendance manquante, accès direct à la DB
    
    // GET api/users - Récupération avec injection SQL possible
    [HttpGet]
    public IActionResult GetUsers([FromQuery] string? role, [FromQuery] string? search)
    {
        using var connection = new SqliteConnection(DatabaseHelper.connectionString);
        connection.Open();
        
        // INJECTION SQL VOLONTAIRE - Concaténation directe
        string query = "SELECT * FROM Users WHERE 1=1";
        
        if (!string.IsNullOrEmpty(role))
        {
            query += $" AND Role = '{role}'"; // VULNÉRABILITÉ : Injection SQL
        }
        
        if (!string.IsNullOrEmpty(search))
        {
            query += $" AND (Username LIKE '%{search}%' OR Email LIKE '%{search}%')"; // VULNÉRABILITÉ : Injection SQL
        }
        
        // VULNÉRABILITÉ : Log de la requête avec données potentiellement sensibles
        Console.WriteLine($"Executing query: {query}");
        
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
        
        return Ok(new { Users = users, Query = query }); // VULNÉRABILITÉ : Exposition de la requête
    }

    // GET api/users/{id} - Récupération avec injection SQL
    [HttpGet("{id}")]
    public IActionResult GetUser(string id)
    {
        using var connection = new SqliteConnection(DatabaseHelper.connectionString);
        connection.Open();
        
        // INJECTION SQL VOLONTAIRE
        var query = $"SELECT * FROM Users WHERE Id = {id} OR Username = '{id}'";
        
        using var command = new SqliteCommand(query, connection);
        using var reader = command.ExecuteReader();
        
        if (reader.Read())
        {
            var user = new
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
            };
            
            // VULNÉRABILITÉ : Log des données sensibles
            Console.WriteLine($"User accessed: {JsonSerializer.Serialize(user)}");
            
            return Ok(user);
        }
        
        return NotFound(new { Message = "User not found", SearchedId = id });
    }

    // POST api/users - Création sans validation
    [HttpPost]
    public async Task<IActionResult> CreateUser()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        
        // VULNÉRABILITÉ : Désérialisation non sécurisée
        var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        
        if (userData == null) 
            return BadRequest(new { Error = "Invalid data", ReceivedData = json });
        
        using var connection = new SqliteConnection(DatabaseHelper.connectionString);
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
            Console.WriteLine($"User created with data: {json}");
            
            return Ok(new 
            { 
                Message = "User created successfully", 
                Data = userData, 
                ExecutedQuery = insertQuery // VULNÉRABILITÉ : Exposition de la requête
            });
        }
        catch (Exception ex)
        {
            // VULNÉRABILITÉ : Exposition des détails d'erreur
            return BadRequest(new 
            { 
                Error = ex.Message, 
                StackTrace = ex.StackTrace,
                Query = insertQuery,
                InputData = userData
            });
        }
    }

    // PUT api/users/{id} - Mise à jour sans validation ni autorisation
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        
        var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        if (userData == null) 
            return BadRequest(new { Error = "Invalid data", ReceivedData = json });
        
        using var connection = new SqliteConnection(DatabaseHelper.connectionString);
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
                Console.WriteLine($"User {id} updated with: {json}");
                return Ok(new 
                { 
                    Message = "User updated successfully",
                    UpdatedData = userData,
                    ExecutedQuery = updateQuery
                });
            }
            
            return NotFound(new { Message = "User not found", UserId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(new 
            { 
                Error = ex.Message, 
                StackTrace = ex.StackTrace,
                Query = updateQuery,
                InputData = userData
            });
        }
    }

    // DELETE api/users/{id} - Suppression sans autorisation
    [HttpDelete("{id}")]
    public IActionResult DeleteUser(string id)
    {
        using var connection = new SqliteConnection(DatabaseHelper.connectionString);
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
                return Ok(new 
                { 
                    Message = "User deleted successfully",
                    DeletedUserId = id,
                    ExecutedQuery = deleteQuery
                });
            }
            
            return NotFound(new { Message = "User not found", UserId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(new 
            { 
                Error = ex.Message, 
                StackTrace = ex.StackTrace,
                Query = deleteQuery
            });
        }
    }
}