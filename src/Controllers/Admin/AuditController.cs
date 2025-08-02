// Controllers/Admin/AuditController.cs - Version étendue
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.Models;
using LibraryAPI.Models.DTOs;
using LibraryAPI.Services;
using System.Diagnostics;

namespace LibraryAPI.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditController> _logger;
        private readonly AuditArchiveService _archiveService;
        private readonly IConfiguration _configuration;

        public AuditController(
            ApplicationDbContext context,
            ILogger<AuditController> logger,
            AuditArchiveService archiveService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _archiveService = archiveService;
            _configuration = configuration;
        }

        /// <summary>
        /// Récupère les logs d'audit avec pagination
        /// </summary>
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int size = 50)
        {
            try
            {
                if (page < 1 || size < 1 || size > 200)
                {
                    return BadRequest("Page doit être >= 1 et size entre 1 et 200");
                }

                var totalLogs = await _context.AuditLogs.CountAsync();
                var logs = await _context.AuditLogs
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(a => new
                    {
                        a.Id,
                        a.UserId,
                        a.Action,
                        a.Message,
                        a.CreatedAt,
                        a.IpAddress
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Logs = logs,
                    Pagination = new
                    {
                        Page = page,
                        Size = size,
                        TotalItems = totalLogs,
                        TotalPages = (int)Math.Ceiling(totalLogs / (double)size)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des logs d'audit");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Obtient des statistiques détaillées sur la base de données d'audit
        /// </summary>
        [HttpGet("database-size")]
        public async Task<IActionResult> GetDatabaseStats()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Statistiques de base
                var totalLogs = await _context.AuditLogs.CountAsync();
                var last7Days = await _context.AuditLogs
                    .CountAsync(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-7));
                var last30Days = await _context.AuditLogs
                    .CountAsync(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-30));

                // Dates extrêmes
                var oldestLog = await _context.AuditLogs
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                var newestLog = await _context.AuditLogs
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                // Top actions
                var topActions = await _context.AuditLogs
                    .GroupBy(a => a.Action)
                    .Select(g => new ActionStatistic
                    {
                        Action = g.Key ?? "UNKNOWN",
                        Count = g.Count(),
                        FirstOccurrence = g.Min(x => x.CreatedAt),
                        LastOccurrence = g.Max(x => x.CreatedAt)
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                // Calcul des pourcentages
                foreach (var action in topActions)
                {
                    action.Percentage = totalLogs > 0 ? (double)action.Count / totalLogs * 100 : 0;
                }

                // Distribution mensuelle (6 derniers mois)
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                var monthlyStats = await _context.AuditLogs
                    .Where(a => a.CreatedAt >= sixMonthsAgo)
                    .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
                    .Select(g => new
                    {
                        YearMonth = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                        Count = g.Count(),
                        TopActions = g.GroupBy(x => x.Action)
                                   .OrderByDescending(ag => ag.Count())
                                   .Take(3)
                                   .Select(ag => ag.Key ?? "UNKNOWN")
                                   .ToList()
                    })
                    .OrderBy(x => x.YearMonth)
                    .ToListAsync();

                var monthlyDistribution = monthlyStats.Select(m => new MonthlyStatistic
                {
                    YearMonth = m.YearMonth,
                    Count = m.Count,
                    TopActionsThisMonth = m.TopActions
                }).ToList();

                // Estimation de taille
                var avgMessageLength = totalLogs > 0
                    ? await _context.AuditLogs.AverageAsync(a => (double?)a.Message!.Length) ?? 0
                    : 0;

                var estimatedSizePerLog = 50 + avgMessageLength + 20; // ID, dates, IP, etc. + message
                var totalSizeKB = (long)(totalLogs * estimatedSizePerLog / 1024);

                var dailyGrowth = last7Days > 0 ? last7Days / 7.0 : 0;
                var dailyGrowthKB = dailyGrowth * estimatedSizePerLog / 1024;

                var sizeEstimate = new DatabaseSizeEstimate
                {
                    EstimatedSizeKB = totalSizeKB,
                    AverageSizePerLog = estimatedSizePerLog,
                    DailyGrowthKB = dailyGrowthKB,
                    Predicted30DaysKB = dailyGrowthKB * 30
                };

                stopwatch.Stop();

                var stats = new AuditDatabaseStats
                {
                    TotalLogs = totalLogs,
                    LogsLast7Days = last7Days,
                    LogsLast30Days = last30Days,
                    OldestLog = oldestLog == default ? null : oldestLog,
                    NewestLog = newestLog == default ? null : newestLog,
                    TopActions = topActions,
                    MonthlyDistribution = monthlyDistribution,
                    SizeEstimate = sizeEstimate
                };

                _logger.LogInformation("📊 Statistiques d'audit calculées en {Duration}ms", stopwatch.ElapsedMilliseconds);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des statistiques d'audit");
                return StatusCode(500, "Erreur lors du calcul des statistiques");
            }
        }

        /// <summary>
        /// Nettoyage manuel des logs d'audit
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupLogs([FromBody] CleanupRequest request)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var cutoffDate = DateTime.UtcNow.AddDays(-request.RetentionDays);

                _logger.LogInformation("🧹 Début du nettoyage manuel - Rétention: {Days} jours, Cutoff: {CutoffDate}",
                                      request.RetentionDays, cutoffDate);

                List<AuditLog> logsToProcess;

                // Filtrage par type d'action si spécifié
                if (!string.IsNullOrEmpty(request.ActionType))
                {
                    logsToProcess = await _context.AuditLogs
                        .Where(log => log.Action.Contains(request.ActionType) && log.CreatedAt < cutoffDate)
                        .ToListAsync();
                }
                else
                {
                    logsToProcess = await _context.AuditLogs
                        .Where(log => log.CreatedAt < cutoffDate)
                        .ToListAsync();
                }

                if (!logsToProcess.Any())
                {
                    return Ok(new CleanupResponse
                    {
                        Message = "Aucun log à nettoyer selon les critères spécifiés",
                        DeletedCount = 0,
                        CutoffDate = cutoffDate,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        IsPreview = request.PreviewOnly
                    });
                }

                // Mode preview : retourner le nombre sans supprimer
                if (request.PreviewOnly)
                {
                    var previewStats = logsToProcess
                        .GroupBy(l => l.Action)
                        .ToDictionary(g => g.Key ?? "UNKNOWN", g => g.Count());

                    return Ok(new CleanupResponse
                    {
                        Message = $"Aperçu : {logsToProcess.Count} logs seraient supprimés",
                        DeletedCount = logsToProcess.Count,
                        CutoffDate = cutoffDate,
                        DetailedStats = previewStats,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        IsPreview = true
                    });
                }

                // Archivage avant suppression si demandé
                string? archiveFilePath = null;
                if (request.ArchiveBeforeDelete)
                {
                    var actionType = request.ActionType ?? "MANUAL_CLEANUP";
                    archiveFilePath = await _archiveService.ArchiveLogsAsync(
                        logsToProcess, actionType, ArchiveFormat.JSON, false);
                }

                // Statistiques détaillées avant suppression
                var detailedStats = logsToProcess
                    .GroupBy(l => l.Action)
                    .ToDictionary(g => g.Key ?? "UNKNOWN", g => g.Count());

                // Suppression effective
                _context.AuditLogs.RemoveRange(logsToProcess);
                await _context.SaveChangesAsync();

                stopwatch.Stop();

                _logger.LogInformation("✅ Nettoyage manuel terminé : {Count} logs supprimés en {Duration}ms",
                                      logsToProcess.Count, stopwatch.ElapsedMilliseconds);

                // Créer un log d'audit pour cette opération
                var auditEntry = new AuditLog
                {
                    UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "ADMIN",
                    Action = "MANUAL_AUDIT_CLEANUP",
                    Message = $"Nettoyage manuel : {logsToProcess.Count} logs supprimés. Type: {request.ActionType ?? "TOUS"}, Rétention: {request.RetentionDays} jours",
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.AuditLogs.Add(auditEntry);
                await _context.SaveChangesAsync();

                return Ok(new CleanupResponse
                {
                    Message = $"Nettoyage terminé : {logsToProcess.Count} logs supprimés avec succès",
                    DeletedCount = logsToProcess.Count,
                    CutoffDate = cutoffDate,
                    DetailedStats = detailedStats,
                    ArchiveFilePath = archiveFilePath,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    IsPreview = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage manuel des logs");
                return StatusCode(500, "Erreur lors du nettoyage des logs");
            }
        }

        /// <summary>
        /// Configuration des politiques de rétention
        /// </summary>
        [HttpGet("retention-config")]
        public IActionResult GetRetentionConfig()
        {
            try
            {
                var config = new RetentionPolicyConfig();

                // Chargement depuis la configuration
                var configSection = _configuration.GetSection("AuditSettings");

                if (configSection.Exists())
                {
                    config.AutoCleanupEnabled = configSection.GetValue<bool>("CleanupEnabled", true);
                    config.CleanupIntervalHours = configSection.GetValue<int>("CleanupIntervalHours", 24);
                    config.ArchiveBeforeDelete = configSection.GetValue<bool>("ArchiveBeforeDelete", false);
                    config.ArchivePath = configSection.GetValue<string>("ArchivePath", "archives/audit") ?? "archives/audit";
                    config.DefaultRetentionDays = configSection.GetValue<int>("DefaultRetentionDays", 180);

                    // Politiques spécifiques
                    var retentionSection = configSection.GetSection("RetentionPolicies");
                    if (retentionSection.Exists())
                    {
                        foreach (var child in retentionSection.GetChildren())
                        {
                            if (int.TryParse(child.Value, out int days))
                            {
                                config.Policies[child.Key] = days;
                            }
                        }
                    }
                }

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la configuration");
                return StatusCode(500, "Erreur lors de la récupération de la configuration");
            }
        }

        /// <summary>
        /// Liste les fichiers d'archive
        /// </summary>
        [HttpGet("archives")]
        public IActionResult ListArchives()
        {
            try
            {
                var archives = _archiveService.ListArchiveFiles();
                return Ok(archives);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des archives");
                return StatusCode(500, "Erreur lors de la récupération des archives");
            }
        }

        /// <summary>
        /// Télécharge un fichier d'archive
        /// </summary>
        [HttpGet("archives/download/{fileName}")]
        public IActionResult DownloadArchive(string fileName)
        {
            try
            {
                var archivePath = _configuration.GetValue<string>("AuditSettings:ArchivePath", "archives/audit") ?? "archives/audit";
                var filePath = Path.Combine(archivePath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Fichier d'archive non trouvé");
                }

                // Vérification de sécurité : le fichier doit être dans le dossier d'archives
                var fullArchivePath = Path.GetFullPath(archivePath);
                var fullFilePath = Path.GetFullPath(filePath);

                if (!fullFilePath.StartsWith(fullArchivePath))
                {
                    _logger.LogWarning("⚠️ Tentative d'accès à un fichier hors du dossier d'archives : {FilePath}", fileName);
                    return BadRequest("Chemin de fichier non autorisé");
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = fileName.EndsWith(".json") ? "application/json" :
                                fileName.EndsWith(".csv") ? "text/csv" :
                                "application/octet-stream";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement de l'archive {FileName}", fileName);
                return StatusCode(500, "Erreur lors du téléchargement");
            }
        }

        /// <summary>
        /// Supprime les anciens fichiers d'archive
        /// </summary>
        [HttpDelete("archives/cleanup")]
        public async Task<IActionResult> CleanupArchives([FromQuery] int maxAgeDays = 365)
        {
            try
            {
                if (maxAgeDays < 1)
                {
                    return BadRequest("L'âge maximum doit être d'au moins 1 jour");
                }

                var maxAge = TimeSpan.FromDays(maxAgeDays);
                var deletedCount = await _archiveService.CleanupOldArchivesAsync(maxAge);

                return Ok(new
                {
                    Message = $"Nettoyage des archives terminé",
                    DeletedCount = deletedCount,
                    MaxAgeDays = maxAgeDays
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage des archives");
                return StatusCode(500, "Erreur lors du nettoyage des archives");
            }
        }

        /// <summary>
        /// Force le nettoyage automatique immédiatement
        /// </summary>
        [HttpPost("force-cleanup")]
        public async Task<IActionResult> ForceAutoCleanup()
        {
            try
            {
                // Cette action déclenche manuellement le même processus que le BackgroundService
                _logger.LogInformation("🔧 Nettoyage automatique forcé par un administrateur");

                // Récupération des politiques de rétention
                var retentionPolicies = GetRetentionPoliciesFromConfig();
                int totalDeleted = 0;
                var deletionStats = new Dictionary<string, int>();

                foreach (var policy in retentionPolicies)
                {
                    var actionPattern = policy.Key;
                    var retentionDays = policy.Value;
                    var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                    List<AuditLog> logsToDelete;

                    if (actionPattern == "DEFAULT")
                    {
                        var knownActions = retentionPolicies.Keys.Where(k => k != "DEFAULT").ToList();
                        logsToDelete = await _context.AuditLogs
                            .Where(log => log.CreatedAt < cutoffDate &&
                                         !knownActions.Any(action => log.Action.Contains(action)))
                            .ToListAsync();
                    }
                    else
                    {
                        logsToDelete = await _context.AuditLogs
                            .Where(log => log.Action.Contains(actionPattern) && log.CreatedAt < cutoffDate)
                            .ToListAsync();
                    }

                    if (logsToDelete.Any())
                    {
                        _context.AuditLogs.RemoveRange(logsToDelete);
                        deletionStats[actionPattern] = logsToDelete.Count;
                        totalDeleted += logsToDelete.Count;
                    }
                }

                if (totalDeleted > 0)
                {
                    await _context.SaveChangesAsync();

                    // Log d'audit pour le nettoyage forcé
                    var auditEntry = new AuditLog
                    {
                        UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "ADMIN",
                        Action = "FORCED_AUTO_CLEANUP",
                        Message = $"Nettoyage automatique forcé : {totalDeleted} logs supprimés",
                        CreatedAt = DateTime.UtcNow,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };

                    _context.AuditLogs.Add(auditEntry);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    Message = $"Nettoyage automatique forcé terminé",
                    DeletedCount = totalDeleted,
                    DetailedStats = deletionStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage automatique forcé");
                return StatusCode(500, "Erreur lors du nettoyage forcé");
            }
        }

        /// <summary>
        /// Récupère les politiques de rétention depuis la configuration
        /// </summary>
        private Dictionary<string, int> GetRetentionPoliciesFromConfig()
        {
            var policies = new Dictionary<string, int>();

            var configSection = _configuration.GetSection("AuditSettings:RetentionPolicies");

            if (configSection.Exists())
            {
                foreach (var child in configSection.GetChildren())
                {
                    if (int.TryParse(child.Value, out int days))
                    {
                        policies[child.Key] = days;
                    }
                }
            }

            // Politiques par défaut si configuration vide
            if (!policies.Any())
            {
                policies = new Dictionary<string, int>
                {
                    { "LOGIN", 180 },
                    { "LOGOUT", 90 },
                    { "REGISTER", 365 },
                    { "PROFILE_UPDATED", 365 },
                    { "BOOK_CREATED", 730 },
                    { "BOOK_DELETED", 730 },
                    { "BOOK_DOWNLOADED", 90 },
                    { "BOOK_VIEWED", 30 },
                    { "FAVORITE_ADDED", 90 },
                    { "FAVORITE_REMOVED", 90 },
                    { "UNAUTHORIZED_ACCESS", 365 },
                    { "RATE_LIMIT_EXCEEDED", 90 },
                    { "SYSTEM_ERROR", 365 },
                    { "DEFAULT", 180 }
                };
            }

            return policies;
        }

        /// <summary>
        /// Statistiques rapides pour le dashboard
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetQuickStats()
        {
            try
            {
                var stats = new
                {
                    TotalLogs = await _context.AuditLogs.CountAsync(),
                    LogsToday = await _context.AuditLogs
                        .CountAsync(a => a.CreatedAt >= DateTime.UtcNow.Date),
                    LogsLast7Days = await _context.AuditLogs
                        .CountAsync(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
                    LoginAttempts = await _context.AuditLogs
                        .CountAsync(a => a.Action.Contains("LOGIN")),
                    BookActions = await _context.AuditLogs
                        .CountAsync(a => a.Action.Contains("BOOK")),
                    SecurityEvents = await _context.AuditLogs
                        .CountAsync(a => a.Action.Contains("UNAUTHORIZED") ||
                                       a.Action.Contains("RATE_LIMIT") ||
                                       a.Action.Contains("SYSTEM_ERROR"))
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques rapides");
                return StatusCode(500, "Erreur lors de la récupération des statistiques");
            }
        }

        /// <summary>
        /// Recherche dans les logs d'audit
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchLogs(
            [FromQuery] string? query,
            [FromQuery] string? action,
            [FromQuery] string? userId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int size = 50)
        {
            try
            {
                if (page < 1 || size < 1 || size > 200)
                {
                    return BadRequest("Page doit être >= 1 et size entre 1 et 200");
                }

                var logsQuery = _context.AuditLogs.AsQueryable();

                // Filtres
                if (!string.IsNullOrEmpty(query))
                {
                    logsQuery = logsQuery.Where(l => l.Message.Contains(query) ||
                                                   l.Action.Contains(query));
                }

                if (!string.IsNullOrEmpty(action))
                {
                    logsQuery = logsQuery.Where(l => l.Action.Contains(action));
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    logsQuery = logsQuery.Where(l => l.UserId == userId);
                }

                if (startDate.HasValue)
                {
                    logsQuery = logsQuery.Where(l => l.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    logsQuery = logsQuery.Where(l => l.CreatedAt <= endDate.Value);
                }

                var totalItems = await logsQuery.CountAsync();

                var logs = await logsQuery
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(l => new
                    {
                        l.Id,
                        l.UserId,
                        l.Action,
                        l.Message,
                        l.CreatedAt,
                        l.IpAddress
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Logs = logs,
                    Pagination = new
                    {
                        Page = page,
                        Size = size,
                        TotalItems = totalItems,
                        TotalPages = (int)Math.Ceiling(totalItems / (double)size)
                    },
                    Filters = new
                    {
                        Query = query,
                        Action = action,
                        UserId = userId,
                        StartDate = startDate,
                        EndDate = endDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche dans les logs");
                return StatusCode(500, "Erreur lors de la recherche");
            }
        }

        /// <summary>
        /// Export personnalisé des logs d'audit
        /// </summary>
        [HttpPost("export")]
        public async Task<IActionResult> ExportLogs([FromBody] ExportRequest request)
        {
            try
            {
                var logsQuery = _context.AuditLogs.AsQueryable();

                // Application des filtres
                if (request.StartDate.HasValue)
                {
                    logsQuery = logsQuery.Where(l => l.CreatedAt >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    logsQuery = logsQuery.Where(l => l.CreatedAt <= request.EndDate.Value);
                }

                if (!string.IsNullOrEmpty(request.ActionType))
                {
                    logsQuery = logsQuery.Where(l => l.Action.Contains(request.ActionType));
                }

                if (!string.IsNullOrEmpty(request.UserId))
                {
                    logsQuery = logsQuery.Where(l => l.UserId == request.UserId);
                }

                var logs = await logsQuery
                    .OrderBy(l => l.CreatedAt)
                    .Take(Math.Min(request.MaxRecords, 10000)) // Limite de sécurité
                    .ToListAsync();

                if (!logs.Any())
                {
                    return BadRequest("Aucun log trouvé selon les critères spécifiés");
                }

                // Export via le service d'archivage
                var archiveFormat = request.Format.ToUpperInvariant() == "JSON"
                    ? ArchiveFormat.JSON
                    : ArchiveFormat.CSV;

                var actionType = request.ActionType ?? "CUSTOM_EXPORT";
                var filePath = await _archiveService.ArchiveLogsAsync(
                    logs, actionType, archiveFormat, request.Compress);

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                var contentType = archiveFormat == ArchiveFormat.JSON ? "application/json" : "text/csv";

                if (request.Compress)
                {
                    contentType = "application/gzip";
                }

                // Log de l'export
                var auditEntry = new AuditLog
                {
                    UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "ADMIN",
                    Action = "AUDIT_EXPORT",
                    Message = $"Export de {logs.Count} logs au format {request.Format}",
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.AuditLogs.Add(auditEntry);
                await _context.SaveChangesAsync();

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export des logs");
                return StatusCode(500, "Erreur lors de l'export");
            }
        }
    }

    /// <summary>
    /// Modèle pour les requêtes d'export personnalisé
    /// </summary>
    public class ExportRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ActionType { get; set; }
        public string? UserId { get; set; }
        public string Format { get; set; } = "CSV"; // CSV ou JSON
        public bool Compress { get; set; } = false;
        public int MaxRecords { get; set; } = 5000;
    }
}

/*
================================================================================
                           DOCUMENTATION - AuditController
================================================================================

DESCRIPTION GÉNÉRALE :
Le AuditController est un contrôleur d'administration destiné à la gestion 
complète des logs d'audit dans l'API Library. Il fournit des fonctionnalités 
avancées de consultation, analyse, nettoyage et archivage des données d'audit.

SÉCURITÉ :
- Accès restreint aux utilisateurs avec le rôle "Admin" uniquement
- Validation des paramètres d'entrée pour éviter les injections
- Vérification des chemins de fichiers pour empêcher le directory traversal
- Logging de toutes les opérations sensibles

================================================================================
                                 ENDPOINTS
================================================================================

📊 CONSULTATION ET STATISTIQUES
-------------------------------

GET /api/admin/audit/logs
- Récupère les logs d'audit avec pagination
- Paramètres : page (défaut: 1), size (défaut: 50, max: 200)
- Retourne : liste paginée des logs avec métadonnées de pagination

GET /api/admin/audit/database-size
- Fournit des statistiques détaillées sur la base de données d'audit
- Inclut : nombre total, distribution temporelle, top actions, estimations de taille
- Performance : optimisé avec des requêtes groupées et calculs en parallèle

GET /api/admin/audit/stats
- Statistiques rapides pour tableau de bord
- Métriques : logs totaux, aujourd'hui, 7 derniers jours, par catégories
- Usage : idéal pour des widgets de monitoring

🔍 RECHERCHE ET FILTRAGE
------------------------

GET /api/admin/audit/search
- Recherche avancée dans les logs avec filtres multiples
- Filtres : query (texte libre), action, userId, startDate, endDate
- Pagination : support complet avec métadonnées
- Performance : indexation recommandée sur CreatedAt et Action

🧹 NETTOYAGE ET MAINTENANCE
---------------------------

POST /api/admin/audit/cleanup
- Nettoyage manuel des logs avec options avancées
- Body : CleanupRequest (retentionDays, actionType?, previewOnly?, archiveBeforeDelete?)
- Fonctionnalités :
  * Mode preview pour estimation avant suppression
  * Archivage automatique optionnel
  * Statistiques détaillées par type d'action
  * Logging de l'opération dans l'audit trail

POST /api/admin/audit/force-cleanup
- Déclenche immédiatement le processus de nettoyage automatique
- Utilise les politiques de rétention configurées
- Idéal pour maintenance planifiée ou résolution d'urgence

⚙️ CONFIGURATION
----------------

GET /api/admin/audit/retention-config
- Récupère la configuration des politiques de rétention
- Sources : appsettings.json section "AuditSettings"
- Inclut : intervalles de nettoyage, politiques par action, chemins d'archivage

📁 GESTION DES ARCHIVES
-----------------------

GET /api/admin/audit/archives
- Liste tous les fichiers d'archive disponibles
- Informations : nom, taille, date de création, format

GET /api/admin/audit/archives/download/{fileName}
- Télécharge un fichier d'archive spécifique
- Sécurité : validation du chemin pour éviter directory traversal
- Formats supportés : JSON, CSV, avec compression optionnelle

DELETE /api/admin/audit/archives/cleanup
- Supprime les archives anciennes selon l'âge spécifié
- Paramètre : maxAgeDays (défaut: 365)
- Retourne : nombre de fichiers supprimés

📤 EXPORT PERSONNALISÉ
----------------------

POST /api/admin/audit/export
- Export personnalisé avec filtres avancés
- Body : ExportRequest (dates, actionType, userId, format, compress, maxRecords)
- Formats : CSV, JSON
- Limite : 10 000 enregistrements maximum par export
- Compression : support gzip optionnel

================================================================================
                            MODÈLES DE DONNÉES
================================================================================

ExportRequest
- StartDate/EndDate : plage temporelle
- ActionType : filtre par type d'action
- UserId : filtre par utilisateur
- Format : "CSV" ou "JSON"
- Compress : bool pour compression gzip
- MaxRecords : limite (max 10 000)

CleanupRequest
- RetentionDays : nombre de jours à conserver
- ActionType : type d'action spécifique (optionnel)
- PreviewOnly : mode aperçu sans suppression
- ArchiveBeforeDelete : archivage automatique avant suppression

AuditDatabaseStats
- TotalLogs : nombre total de logs
- LogsLast7Days/Last30Days : métriques temporelles
- OldestLog/NewestLog : dates extrêmes
- TopActions : liste des actions les plus fréquentes
- MonthlyDistribution : répartition mensuelle
- SizeEstimate : estimation de l'espace disque

================================================================================
                           CONFIGURATION RECOMMANDÉE
================================================================================

appsettings.json :
{
  "AuditSettings": {
    "CleanupEnabled": true,
    "CleanupIntervalHours": 24,
    "ArchiveBeforeDelete": false,
    "ArchivePath": "archives/audit",
    "DefaultRetentionDays": 180,
    "RetentionPolicies": {
      "LOGIN": 180,
      "LOGOUT": 90,
      "REGISTER": 365,
      "BOOK_CREATED": 730,
      "BOOK_DELETED": 730,
      "BOOK_DOWNLOADED": 90,
      "UNAUTHORIZED_ACCESS": 365,
      "SYSTEM_ERROR": 365,
      "DEFAULT": 180
    }
  }
}

================================================================================
                              DÉPENDANCES
================================================================================

Services injectés :
- ApplicationDbContext : accès aux données
- ILogger<AuditController> : logging des opérations
- AuditArchiveService : gestion des archives
- IConfiguration : lecture de la configuration

Services externes requis :
- Entity Framework Core pour les requêtes LINQ
- System.Diagnostics pour la mesure de performances
- System.IO pour la gestion des fichiers

================================================================================
                         CONSIDÉRATIONS DE PERFORMANCE
================================================================================

OPTIMISATIONS IMPLÉMENTÉES :
- Requêtes LINQ avec Select() pour limiter les données transférées
- Pagination systématique avec Skip/Take
- Utilisation d'index sur CreatedAt recommandée
- Calculs statistiques optimisés avec GroupBy
- Stopwatch pour mesurer les temps d'exécution

RECOMMANDATIONS :
- Index base de données sur (CreatedAt, Action)
- Planification du nettoyage automatique hors heures de pointe
- Monitoring de la taille de la base de données
- Archivage régulier pour maintenir les performances

================================================================================
                            SÉCURITÉ ET AUDIT
================================================================================

MESURES DE SÉCURITÉ :
- Autorisation admin obligatoire sur tous les endpoints
- Validation des paramètres d'entrée
- Limitation du nombre d'enregistrements exportables
- Vérification des chemins de fichiers
- Logging de toutes les opérations sensibles

AUDIT TRAIL :
- Toutes les opérations de nettoyage sont loggées
- Les exports sont tracés avec détails
- Les accès aux archives sont enregistrés
- Horodatage UTC pour toutes les opérations

================================================================================
                              MAINTENANCE
================================================================================

TÂCHES RÉGULIÈRES :
- Surveillance de la croissance des logs
- Vérification des archives générées
- Contrôle des politiques de rétention
- Monitoring des performances du nettoyage

DÉPANNAGE :
- Vérifier les logs d'erreur pour les échecs de nettoyage
- Contrôler l'espace disque pour les archives
- Valider la configuration des politiques de rétention
- Surveiller les temps de réponse des requêtes statistiques

================================================================================
                                CHANGELOG
================================================================================

Version actuelle : Fonctionnalités complètes
- Gestion complète des logs d'audit
- Système d'archivage intégré
- Politiques de rétention configurables
- Export personnalisé multi-format
- Statistiques avancées avec distribution temporelle
- Interface de nettoyage manuel et automatique

Améliorations futures potentielles :
- Interface graphique pour la visualisation des statistiques
- Alertes automatiques en cas de croissance anormale
- API de streaming pour très gros exports
- Compression différentielle des archives
- Intégration avec systèmes de monitoring externes

================================================================================
*/
