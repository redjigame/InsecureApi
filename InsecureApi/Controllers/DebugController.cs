using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InsecureApi.Controllers;

// VULNÉRABILITÉ : Controller de debug exposant des informations système
[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetDebugInfo()
    {
        // VULNÉRABILITÉ : Exposition massive d'informations système
        var processInfo = Process.GetCurrentProcess();
        
        return Ok(new
        {
            Environment = Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(entry => entry.Key.ToString(), entry => entry.Value?.ToString()),
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            OSVersion = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet,
            ConnectionString = DatabaseHelper.connectionString,
            DatabasePassword = DatabaseHelper.dbPassword,
            DatabasePath = Path.GetFullPath("database.db"),
            ProcessInfo = new
            {
                ProcessName = processInfo.ProcessName,
                Id = processInfo.Id,
                StartTime = processInfo.StartTime,
                WorkingSet64 = processInfo.WorkingSet64,
                PrivateMemorySize64 = processInfo.PrivateMemorySize64
            },
            ApplicationInfo = new
            {
                ContentRootPath = Directory.GetCurrentDirectory(),
                Files = Directory.GetFiles(Directory.GetCurrentDirectory()).Take(10),
                Directories = Directory.GetDirectories(Directory.GetCurrentDirectory())
            }
        });
    }

    // GET api/debug/files/{*filePath} - Lecture de fichiers arbitraires
    [HttpGet("files/{*filePath}")]
    public IActionResult ReadFile(string filePath)
    {
        try
        {
            // VULNÉRABILITÉ CRITIQUE : Path traversal - lecture de fichiers arbitraires
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            
            if (System.IO.File.Exists(fullPath))
            {
                var content = System.IO.File.ReadAllText(fullPath);
                return Ok(new 
                { 
                    FilePath = fullPath,
                    Content = content,
                    Size = new FileInfo(fullPath).Length
                });
            }
            
            // VULNÉRABILITÉ : Tentative de lecture même si le fichier n'existe pas localement
            if (System.IO.File.Exists(filePath))
            {
                var content = System.IO.File.ReadAllText(filePath);
                return Ok(new 
                { 
                    FilePath = filePath,
                    Content = content,
                    Size = new FileInfo(filePath).Length
                });
            }
            
            return NotFound(new { Error = "File not found", RequestedPath = filePath });
        }
        catch (Exception ex)
        {
            return BadRequest(new 
            { 
                Error = ex.Message,
                StackTrace = ex.StackTrace,
                RequestedPath = filePath
            });
        }
    }
}