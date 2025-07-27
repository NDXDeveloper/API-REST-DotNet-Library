using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using LibraryAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;

namespace LibraryAPI.Controllers
{
    /// <summary>
    /// Contrôleur pour les vérifications de santé de l'API
    /// </summary>
    [DisableRateLimiting] // AUCUNE limitation pour les health checks
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HealthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Vérification de santé standard de l'API
        /// </summary>
        /// <returns>Statut de santé de l'API avec informations de base</returns>
        /// <response code="200">API en bonne santé</response>
        /// <response code="503">API en erreur</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> HealthCheck()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = GetAssemblyMetadata(assembly, "GitTag") ?? 
                         assembly.GetName().Version?.ToString() ?? "unknown";

            var healthStatus = new
            {
                Status = "Healthy",
                Service = "LibraryAPI",
                Version = version,
                Timestamp = DateTime.UtcNow,
                Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
                Database = await CheckDatabaseHealth(),
                Uptime = GetUptime(),
                Memory = GetMemoryUsage()
            };

            // Si la base de données est en erreur, retourner un statut 503
            if (healthStatus.Database is { } dbStatus && 
                typeof(object).GetProperty("Status")?.GetValue(dbStatus)?.ToString() != "OK")
            {
                return StatusCode(503, healthStatus);
            }

            return Ok(healthStatus);
        }

        /// <summary>
        /// Vérification de santé détaillée (Administrateurs uniquement)
        /// </summary>
        /// <returns>Statut de santé détaillé avec métriques système</returns>
        /// <response code="200">Informations détaillées de santé</response>
        /// <response code="401">Non autorisé</response>
        /// <response code="403">Accès refusé - rôle Admin requis</response>
        [HttpGet("detailed")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> DetailedHealthCheck()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            var detailedHealth = new
            {
                Status = "Healthy",
                Service = "LibraryAPI",
                Version = new
                {
                    Assembly = assembly.GetName().Version?.ToString() ?? "unknown",
                    Git = new
                    {
                        Tag = GetAssemblyMetadata(assembly, "GitTag") ?? "unknown",
                        Commit = GetAssemblyMetadata(assembly, "GitCommit") ?? "unknown",
                        Branch = GetAssemblyMetadata(assembly, "GitBranch") ?? "unknown",
                        BuildTime = GetAssemblyMetadata(assembly, "BuildTime") ?? "unknown"
                    }
                },
                Timestamp = DateTime.UtcNow,
                Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
                Database = await CheckDatabaseHealthDetailed(),
                System = new
                {
                    Uptime = GetUptime(),
                    Memory = GetMemoryUsage(),
                    Platform = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    MachineName = Environment.MachineName,
                    DotNetVersion = Environment.Version.ToString()
                },
                Configuration = new
                {
                    JwtConfigured = !string.IsNullOrEmpty(_configuration["Jwt:Key"]),
                    EmailConfigured = !string.IsNullOrEmpty(_configuration["EmailSettings:SmtpServer"]),
                    SwaggerEnabled = _configuration.GetValue<bool>("EnableSwagger")
                },
                Statistics = await GetApplicationStatistics()
            };

            return Ok(detailedHealth);
        }

        /// <summary>
        /// Endpoint simple de santé pour services externes (Railway, Docker, etc.)
        /// </summary>
        /// <returns>Statut de santé minimal</returns>
        /// <response code="200">Service disponible</response>
        /// <response code="503">Service indisponible</response>
        [HttpGet("simple")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> SimpleHealthCheck()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                
                return Ok(new {
                    Status = "Healthy",
                    Database = canConnect ? "OK" : "ERROR",
                    Timestamp = DateTime.UtcNow,
                    Version = GetAssemblyMetadata(Assembly.GetExecutingAssembly(), "GitTag") ?? "dev"
                });
            }
            catch
            {
                return StatusCode(503, new {
                    Status = "Unhealthy",
                    Database = "ERROR",
                    Timestamp = DateTime.UtcNow,
                    Version = "unknown"
                });
            }
        }

        /// <summary>
        /// Vérification de la santé de la base de données
        /// </summary>
        private async Task<dynamic> CheckDatabaseHealth()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return new { Status = "ERROR", Message = "Cannot connect to database" };
                }

                // Test simple de requête
                var userCount = await _context.Users.CountAsync();
                
                return new { 
                    Status = "OK", 
                    Message = "Database connection successful",
                    UserCount = userCount
                };
            }
            catch (Exception ex)
            {
                return new { 
                    Status = "ERROR", 
                    Message = ex.Message 
                };
            }
        }

        /// <summary>
        /// Vérification détaillée de la base de données
        /// </summary>
        private async Task<object> CheckDatabaseHealthDetailed()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var canConnect = await _context.Database.CanConnectAsync();
                var connectionTime = DateTime.UtcNow - startTime;

                if (!canConnect)
                {
                    return new { 
                        Status = "ERROR", 
                        Message = "Cannot connect to database",
                        ConnectionTime = connectionTime.TotalMilliseconds
                    };
                }

                // Tests de performance de base
                var userCount = await _context.Users.CountAsync();
                var bookCount = await _context.BooksMagazines.CountAsync();
                var totalTime = DateTime.UtcNow - startTime;

                return new { 
                    Status = "OK", 
                    Message = "Database connection successful",
                    ConnectionTime = connectionTime.TotalMilliseconds,
                    QueryTime = totalTime.TotalMilliseconds,
                    Tables = new
                    {
                        Users = userCount,
                        Books = bookCount,
                        Authors = await _context.Authors.CountAsync(),
                        Categories = await _context.Categories.CountAsync()
                    }
                };
            }
            catch (Exception ex)
            {
                return new { 
                    Status = "ERROR", 
                    Message = ex.Message,
                    Type = ex.GetType().Name
                };
            }
        }

        /// <summary>
        /// Calcul du temps de fonctionnement
        /// </summary>
        private static string GetUptime()
        {
            try
            {
                var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
                return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Information sur l'utilisation mémoire
        /// </summary>
        private static object GetMemoryUsage()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                return new
                {
                    WorkingSetMB = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2),
                    PrivateMemoryMB = Math.Round(process.PrivateMemorySize64 / 1024.0 / 1024.0, 2),
                    GCMemoryMB = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2)
                };
            }
            catch
            {
                return new { Message = "Memory info unavailable" };
            }
        }

        /// <summary>
        /// Statistiques de l'application
        /// </summary>
        private async Task<object> GetApplicationStatistics()
        {
            try
            {
                return new
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalBooks = await _context.BooksMagazines.CountAsync(),
                    TotalDownloads = await _context.BooksMagazines.SumAsync(b => b.DownloadCount),
                    TotalViews = await _context.BooksMagazines.SumAsync(b => b.ViewCount),
                    ActiveUsers = await _context.Users
                        .Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                        .CountAsync(),
                    RecentBooks = await _context.BooksMagazines
                        .Where(b => b.UploadDate >= DateTime.UtcNow.AddDays(-7))
                        .CountAsync()
                };
            }
            catch
            {
                return new { Message = "Statistics unavailable" };
            }
        }

        /// <summary>
        /// Récupération des métadonnées d'assembly
        /// </summary>
        private static string? GetAssemblyMetadata(Assembly assembly, string key)
        {
            try 
            {
                return assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(x => x.Key == key)?.Value;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Contrôleur pour l'endpoint /health legacy (Railway, Docker, etc.)
    /// </summary>
    [DisableRateLimiting]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)] // Cache ce contrôleur de Swagger
    public class RootHealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RootHealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Endpoint racine pour Railway et services externes
        /// </summary>
        [HttpGet("/health")]
        [AllowAnonymous]
        public async Task<IActionResult> RootHealthCheck()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                
                return Ok(new {
                    Status = "Healthy",
                    Database = canConnect ? "OK" : "ERROR",
                    Timestamp = DateTime.UtcNow,
                    Version = GetAssemblyMetadata(Assembly.GetExecutingAssembly(), "GitTag") ?? "dev"
                });
            }
            catch
            {
                return StatusCode(503, new {
                    Status = "Unhealthy",
                    Database = "ERROR",
                    Timestamp = DateTime.UtcNow,
                    Version = "unknown"
                });
            }
        }

        private static string? GetAssemblyMetadata(Assembly assembly, string key)
        {
            try 
            {
                return assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(x => x.Key == key)?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}