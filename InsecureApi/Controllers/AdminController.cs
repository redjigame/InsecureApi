using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace InsecureApi.Controllers;

// VULNÉRABILITÉ : Controller d'administration sans protection
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    // GET api/admin/database - Exposition de la structure de la base
    [HttpGet("database")]
    public IActionResult GetDatabaseInfo()
    {
        using var connection = new SqliteConnection(DatabaseHelper.connectionString);
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
        
        return Ok(new 
        { 
            Tables = tables, 
            ConnectionString = DatabaseHelper.connectionString,
            DatabasePassword = DatabaseHelper.dbPassword,
            DatabasePath = Path.GetFullPath("database.db")
        });
    }

    // POST api/admin/sql - Exécution de SQL arbitraire
    [HttpPost("sql")]
    public async Task<IActionResult> ExecuteSql()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        var sqlData = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        
        if (sqlData == null || !sqlData.ContainsKey("query"))
            return BadRequest(new { Error = "Query required", ReceivedData = json });
        
        var query = sqlData["query"];
        
        using var connection = new SqliteConnection(DatabaseHelper.connectionString);
        connection.Open();
        
        try
        {
            // VULNÉRABILITÉ CRITIQUE : Exécution de SQL arbitraire
            using var command = new SqliteCommand(query, connection);
            
            // VULNÉRABILITÉ : Log de toutes les requêtes exécutées
            Console.WriteLine($"Executing arbitrary SQL: {query}");
            
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
                
                return Ok(new 
                { 
                    Results = results, 
                    Query = query,
                    ExecutedAt = DateTime.Now
                });
            }
            else
            {
                var rowsAffected = command.ExecuteNonQuery();
                return Ok(new 
                { 
                    RowsAffected = rowsAffected,
                    Query = query,
                    ExecutedAt = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            // VULNÉRABILITÉ : Exposition complète des erreurs
            return BadRequest(new 
            { 
                Error = ex.Message, 
                StackTrace = ex.StackTrace,
                Query = query,
                InnerException = ex.InnerException?.Message
            });
        }
    }

    // GET api/admin/logs - Exposition des logs système
    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        try
        {
            // VULNÉRABILITÉ : Exposition des logs système
            var logPath = Path.Combine(Environment.CurrentDirectory, "logs");
            var logs = new List<string>();
            
            if (Directory.Exists(logPath))
            {
                var logFiles = Directory.GetFiles(logPath, "*.log");
                foreach (var file in logFiles)
                {
                    logs.Add(System.IO.File.ReadAllText(file));
                }
            }
            
            return Ok(new 
            { 
                Logs = logs,
                LogPath = logPath,
                CurrentDirectory = Environment.CurrentDirectory
            });
        }
        catch (Exception ex)
        {
            return Ok(new 
            { 
                Error = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}