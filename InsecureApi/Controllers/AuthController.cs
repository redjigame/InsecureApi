using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace InsecureApi.Controllers;

// VULNÉRABILITÉ : Controller d'authentification sans sécurité
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
     // POST api/auth/login - Login ultra insécurisé
    [HttpPost("login")]
    public async Task<IActionResult> Login()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        var loginData = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        
        if (loginData == null) 
            return BadRequest(new { Error = "Invalid data", ReceivedData = json });
        
        var username = loginData.GetValueOrDefault("username", "");
        var password = loginData.GetValueOrDefault("password", "");
        
        using var connection = new SqliteConnection(DatabaseHelper.connectionString);
        connection.Open();
        
        // INJECTION SQL VOLONTAIRE dans l'authentification
        var query = $"SELECT * FROM Users WHERE Username = '{username}' AND Password = '{password}'";
        
        using var command = new SqliteCommand(query, connection);
        using var dbReader = command.ExecuteReader();
        
        if (dbReader.Read())
        {
            // VULNÉRABILITÉ : Token JWT généré de manière prévisible
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{DateTime.Now:yyyyMMddHHmmss}"));
            
            // VULNÉRABILITÉ : Log des credentials
            Console.WriteLine($"Successful login - Username: {username}, Password: {password}");
            
            var userInfo = new
            {
                Id = dbReader["Id"],
                Username = dbReader["Username"],
                Role = dbReader["Role"],
                ApiKey = dbReader["ApiKey"], // VULNÉRABILITÉ : Exposition de la clé API
                CreditCard = dbReader["CreditCard"], // VULNÉRABILITÉ : Exposition des données sensibles
                SSN = dbReader["SSN"] // VULNÉRABILITÉ : Exposition du SSN
            };
            
            return Ok(new
            {
                Token = token,
                User = userInfo,
                LoginQuery = query, // VULNÉRABILITÉ : Exposition de la requête
                ExpiresAt = DateTime.Now.AddHours(24)
            });
        }
        
        // VULNÉRABILITÉ : Information disclosure détaillée
        Console.WriteLine($"Failed login attempt - Username: {username}, Password: {password}");
        return Unauthorized(new 
        { 
            Message = "Invalid credentials",
            AttemptedUsername = username,
            AttemptedPassword = password, // VULNÉRABILITÉ CRITIQUE : Exposition du mot de passe
            LoginQuery = query
        });
    }

    // GET api/auth/validate/{token} - Validation de token prévisible
    [HttpGet("validate/{token}")]
    public IActionResult ValidateToken(string token)
    {
        try
        {
            // VULNÉRABILITÉ : Validation de token ultra faible
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split(':');
            
            if (parts.Length == 2)
            {
                var username = parts[0];
                var timestamp = parts[1];
                
                // VULNÉRABILITÉ : Pas de vérification d'expiration réelle
                Console.WriteLine($"Token validated for user: {username}");
                
                return Ok(new 
                { 
                    Valid = true, 
                    Username = username, 
                    Timestamp = timestamp,
                    DecodedToken = decoded // VULNÉRABILITÉ : Exposition du token décodé
                });
            }
            
            return BadRequest(new { Valid = false, Token = token });
        }
        catch (Exception ex)
        {
            return BadRequest(new 
            { 
                Valid = false, 
                Error = ex.Message,
                Token = token,
                StackTrace = ex.StackTrace
            });
        }
    }
}