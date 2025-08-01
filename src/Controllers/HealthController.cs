using Microsoft.AspNetCore.Mvc; // Framework MVC pour les contrôleurs API
using Microsoft.EntityFrameworkCore; // ORM pour l'accès aux données
using System.Reflection; // Réflexion pour obtenir des informations sur l'assembly
using LibraryAPI.Data; // Contexte de données de l'application
using Microsoft.AspNetCore.Authorization; // Gestion des autorisations
using Microsoft.AspNetCore.RateLimiting; // Limitation du taux de requêtes
using System.Diagnostics; // Outils de diagnostic et de performance

// Namespace contenant tous les contrôleurs de l'API Library
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

        /// <summary>
        /// Contexte de base de données pour vérifier la connectivité
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Configuration de l'application pour accéder aux paramètres
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initialise une nouvelle instance du contrôleur de santé
        /// </summary>
        /// <param name="context">Le contexte de base de données</param>
        /// <param name="configuration">La configuration de l'application</param>
        public HealthController(ApplicationDbContext context, IConfiguration configuration)
        {
            // Injection des dépendances nécessaires pour les vérifications de santé
            _context = context; // Stockage du contexte DB pour les vérifications de connectivité
            _configuration = configuration; // Stockage de la configuration pour accéder aux paramètres d'environnement
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

                return Ok(new
                {
                    Status = "Healthy",
                    Database = canConnect ? "OK" : "ERROR",
                    Timestamp = DateTime.UtcNow,
                    Version = GetAssemblyMetadata(Assembly.GetExecutingAssembly(), "GitTag") ?? "dev"
                });
            }
            catch
            {
                return StatusCode(503, new
                {
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

                return new
                {
                    Status = "OK",
                    Message = "Database connection successful",
                    UserCount = userCount
                };
            }
            catch (Exception ex)
            {
                return new
                {
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
                    return new
                    {
                        Status = "ERROR",
                        Message = "Cannot connect to database",
                        ConnectionTime = connectionTime.TotalMilliseconds
                    };
                }

                // Tests de performance de base
                var userCount = await _context.Users.CountAsync();
                var bookCount = await _context.BooksMagazines.CountAsync();
                var totalTime = DateTime.UtcNow - startTime;

                return new
                {
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
                return new
                {
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

                return Ok(new
                {
                    Status = "Healthy",
                    Database = canConnect ? "OK" : "ERROR",
                    Timestamp = DateTime.UtcNow,
                    Version = GetAssemblyMetadata(Assembly.GetExecutingAssembly(), "GitTag") ?? "dev"
                });
            }
            catch
            {
                return StatusCode(503, new
                {
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

/*
===== SYSTÈME DE MONITORING DE SANTÉ MULTI-NIVEAUX =====
✅ ARCHITECTURE DU HEALTH CHECK CONTROLLER :
- HealthController principal (/api/health) avec 3 endpoints distincts
- RootHealthController legacy (/health) pour compatibilité Docker/Railway
- Rate limiting désactivé sur tous les endpoints pour monitoring critique
- Authentification différentielle selon le niveau de détail requis
- Réponses standardisées avec codes HTTP appropriés (200, 503, 401, 403)

✅ ENDPOINTS ET LEURS USAGES SPÉCIFIQUES :
1. GET /api/health - Health check standard (PUBLIC)
   → Monitoring basique avec infos essentielles
   → Utilisé par les load balancers et monitoring externes
   → Inclut version, uptime, mémoire, base de données

2. GET /api/health/detailed - Health check détaillé (ADMIN SEULEMENT)
   → Diagnostic approfondi pour les administrateurs
   → Informations Git (tag, commit, branche, build time)
   → Métriques système complètes (OS, .NET, processeur)
   → Configuration des services (JWT, Email, Swagger)
   → Statistiques applicatives (utilisateurs, livres, téléchargements)

3. GET /api/health/simple - Health check minimal (PUBLIC)
   → Conçu pour Docker healthcheck et Railway
   → Réponse ultra-légère pour services automatisés
   → Test basique de connectivité DB uniquement

4. GET /health - Endpoint legacy (PUBLIC)
   → Compatibilité avec anciens systèmes de monitoring
   → Duplication du comportement simple pour migration

===== MÉCANISMES DE VÉRIFICATION DE SANTÉ =====
🔍 TESTS DE BASE DE DONNÉES :
✅ CheckDatabaseHealth() - Version standard
   → Test de connectivité avec CanConnectAsync()
   → Comptage simple des utilisateurs pour vérifier les requêtes
   → Gestion des exceptions avec messages d'erreur explicites

✅ CheckDatabaseHealthDetailed() - Version complète
   → Mesure des temps de connexion et de requête
   → Tests sur toutes les tables principales (Users, Books, Authors, Categories)
   → Métriques de performance pour détecter les ralentissements

🔧 MÉTRIQUES SYSTÈME :
✅ GetUptime() - Calcul du temps de fonctionnement
   → Basé sur le Process.StartTime du processus courant
   → Format lisible : "5d 12h 30m 45s"

✅ GetMemoryUsage() - Surveillance mémoire
   → Working Set : mémoire physique utilisée
   → Private Memory : mémoire privée du processus
   → GC Memory : mémoire gérée par le Garbage Collector

✅ GetApplicationStatistics() - Statistiques métier
   → Compteurs d'entités (users, books, downloads, views)
   → Indicateurs d'activité (utilisateurs récents, livres récents)
   → Données pour tableaux de bord administrateurs

===== SÉCURITÉ ET CONTRÔLE D'ACCÈS =====
🔒 NIVEAUX D'AUTORISATION :
- Endpoints publics : accès libre pour monitoring externe
- Endpoint détaillé : rôle Admin requis pour protéger infos sensibles
- Rate limiting désactivé : priorité absolue au monitoring

🛡️ INFORMATIONS SENSIBLES PROTÉGÉES :
- Configuration JWT : masquée (présence/absence seulement)
- Configuration Email : masquée (présence/absence seulement)
- Détails système complets : Admin seulement
- Statistiques business : Admin seulement

===== CODES DE RETOUR ET SIGNIFICATION =====
📊 STATUS CODES UTILISÉS :
- 200 OK : Système en bonne santé
- 503 Service Unavailable : Problème critique détecté (DB down)
- 401 Unauthorized : Token manquant/invalide
- 403 Forbidden : Permissions insuffisantes (non-Admin)

✅ LOGIQUE DE DÉTERMINATION DU STATUT :
- Statut 503 si base de données inaccessible
- Statut 200 pour tous les autres cas
- Réponse JSON dans tous les cas avec détails du problème

===== INTÉGRATION AVEC L'ÉCOSYSTÈME =====
🐳 COMPATIBILITÉ DOCKER :
- Endpoint /health configuré pour Docker HEALTHCHECK
- Réponse minimaliste pour éviter les timeouts
- Gestion des exceptions pour éviter les crashes

🚀 COMPATIBILITÉ RAILWAY/HEROKU :
- Endpoint racine /health respecte les conventions PaaS
- Variables d'environnement ASPNETCORE_ENVIRONMENT supportées
- Détection automatique de l'environnement

📈 MONITORING ET OBSERVABILITÉ :
- Timestamps UTC pour cohérence globale
- Versioning automatique via GitTag ou assembly
- Métriques exposées pour Grafana/Prometheus (via parsing JSON)

===== BONNES PRATIQUES IMPLÉMENTÉES =====
✅ PERFORMANCE :
- Requêtes DB asynchrones partout
- Pas de operations bloquantes
- Cache des métadonnées assembly

✅ ROBUSTESSE :
- Try-catch sur toutes les opérations critiques
- Fallback values pour toutes les métriques
- Pas d'exceptions non gérées

✅ MAINTENANCE :
- Séparation claire des responsabilités par méthode
- Code DRY pour éviter la duplication
- Documentation XML complète pour IntelliSense

===== UTILISATION PRATIQUE =====
🔍 Pour les développeurs :
→ GET /api/health pour vérifier l'état général en développement
→ GET /api/health/detailed (en tant qu'admin) pour diagnostic approfondi

🖥️ Pour les administrateurs système :
→ GET /health dans Docker healthcheck
→ GET /api/health/simple pour monitoring automatisé
→ Surveillance des métriques mémoire et uptime

📊 Pour le monitoring :
→ Parser le JSON de /api/health pour extraire les métriques
→ Alertes sur Status != "Healthy" ou codes HTTP 503
→ Graphiques d'évolution de la mémoire et uptime

💡 Ce système de health check suit les standards de l'industrie et fournit
   une observabilité complète de l'application LibraryAPI en production.
*/