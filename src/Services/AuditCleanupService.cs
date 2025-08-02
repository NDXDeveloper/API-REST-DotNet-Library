using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.Models;
using System.Text;

namespace LibraryAPI.Services
{
    /// <summary>
    /// SERVICE DE NETTOYAGE AUTOMATIQUE DES LOGS D'AUDIT
    /// 
    /// Ce service s'exécute en arrière-plan pour nettoyer automatiquement
    /// les anciens logs d'audit selon les politiques de rétention définies.
    /// 
    /// Fonctionnalités :
    /// - Nettoyage quotidien automatique
    /// - Politiques de rétention différenciées par type d'action
    /// - Archivage optionnel avant suppression
    /// - Logging détaillé des opérations de nettoyage
    /// - Gestion robuste des erreurs
    /// </summary>
    public class AuditCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditCleanupService> _logger;
        private readonly IConfiguration _configuration;
        
        // Configuration par défaut du service
        private readonly TimeSpan _cleanupInterval;
        private readonly bool _cleanupEnabled;
        private readonly bool _archiveBeforeDelete;
        private readonly string _archivePath;

        public AuditCleanupService(
            IServiceScopeFactory scopeFactory, 
            ILogger<AuditCleanupService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;

            // Chargement de la configuration
            var cleanupIntervalHours = _configuration.GetValue<int>("AuditSettings:CleanupIntervalHours", 24);
            _cleanupInterval = TimeSpan.FromHours(cleanupIntervalHours);
            _cleanupEnabled = _configuration.GetValue<bool>("AuditSettings:CleanupEnabled", true);
            _archiveBeforeDelete = _configuration.GetValue<bool>("AuditSettings:ArchiveBeforeDelete", false);
            _archivePath = _configuration.GetValue<string>("AuditSettings:ArchivePath", "archives/audit") ?? "archives/audit";

            _logger.LogInformation("🧹 AuditCleanupService initialized - Enabled: {Enabled}, Interval: {Interval}h, Archive: {Archive}", 
                                  _cleanupEnabled, cleanupIntervalHours, _archiveBeforeDelete);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_cleanupEnabled)
            {
                _logger.LogInformation("🔒 Nettoyage automatique des logs d'audit désactivé dans la configuration");
                return;
            }

            _logger.LogInformation("🚀 Démarrage du service de nettoyage automatique des logs d'audit");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldLogsAsync();
                    
                    _logger.LogDebug("⏰ Prochain nettoyage dans {Interval}", _cleanupInterval);
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 Service de nettoyage arrêté gracieusement");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors du nettoyage automatique des logs d'audit");
                    
                    // Attendre 1h en cas d'erreur avant de réessayer
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Nettoie les anciens logs selon les politiques de rétention
        /// </summary>
        private async Task CleanupOldLogsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var startTime = DateTime.UtcNow;
            _logger.LogInformation("🧹 Début du nettoyage des logs d'audit à {StartTime}", startTime);

            try
            {
                // Récupération des politiques de rétention depuis la configuration
                var retentionPolicies = GetRetentionPolicies();
                int totalDeleted = 0;
                var deletionStats = new Dictionary<string, int>();

                // Nettoyage par politique spécifique
                foreach (var policy in retentionPolicies)
                {
                    var actionPattern = policy.Key;
                    var retentionDays = policy.Value;
                    var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                    _logger.LogDebug("🔍 Vérification des logs {Action} antérieurs au {CutoffDate} ({RetentionDays} jours)", 
                                    actionPattern, cutoffDate, retentionDays);

                    List<AuditLog> logsToDelete;

                    if (actionPattern == "DEFAULT")
                    {
                        // Politique par défaut pour tous les autres types d'actions
                        var knownActions = retentionPolicies.Keys.Where(k => k != "DEFAULT").ToList();
                        
                        logsToDelete = await context.AuditLogs
                            .Where(log => log.CreatedAt < cutoffDate && 
                                         !knownActions.Any(action => log.Action.Contains(action)))
                            .ToListAsync();
                    }
                    else
                    {
                        // Politique spécifique pour un type d'action
                        logsToDelete = await context.AuditLogs
                            .Where(log => log.Action.Contains(actionPattern) && log.CreatedAt < cutoffDate)
                            .ToListAsync();
                    }

                    if (logsToDelete.Any())
                    {
                        // Archivage optionnel avant suppression
                        if (_archiveBeforeDelete)
                        {
                            await ArchiveLogsAsync(logsToDelete, actionPattern);
                        }

                        context.AuditLogs.RemoveRange(logsToDelete);
                        deletionStats[actionPattern] = logsToDelete.Count;
                        totalDeleted += logsToDelete.Count;

                        _logger.LogInformation("🗑️ {Count} logs {Action} marqués pour suppression (> {RetentionDays} jours)", 
                                              logsToDelete.Count, actionPattern, retentionDays);
                    }
                }

                // Sauvegarde des suppressions si nécessaire
                if (totalDeleted > 0)
                {
                    await context.SaveChangesAsync();
                    
                    var duration = DateTime.UtcNow - startTime;
                    _logger.LogInformation("✅ Nettoyage terminé : {TotalDeleted} logs supprimés en {Duration}ms", 
                                          totalDeleted, duration.TotalMilliseconds);

                    // Log détaillé des statistiques
                    foreach (var stat in deletionStats)
                    {
                        _logger.LogInformation("📊 {Action}: {Count} logs supprimés", stat.Key, stat.Value);
                    }

                    // Audit du nettoyage lui-même
                    await LogCleanupAuditAsync(context, totalDeleted, deletionStats);
                }
                else
                {
                    var duration = DateTime.UtcNow - startTime;
                    _logger.LogInformation("✨ Aucun log à supprimer (vérification en {Duration}ms)", 
                                          duration.TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur durant le processus de nettoyage des logs");
                throw;
            }
        }

        /// <summary>
        /// Récupère les politiques de rétention depuis la configuration
        /// </summary>
        private Dictionary<string, int> GetRetentionPolicies()
        {
            var policies = new Dictionary<string, int>();
            
            // Chargement depuis la configuration avec valeurs par défaut
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

            // Politiques par défaut si la configuration est vide
            if (!policies.Any())
            {
                policies = new Dictionary<string, int>
                {
                    { "LOGIN", 180 },              // 6 mois pour connexions
                    { "LOGOUT", 90 },              // 3 mois pour déconnexions  
                    { "REGISTER", 365 },           // 1 an pour inscriptions
                    { "PROFILE_UPDATED", 365 },    // 1 an pour modifications profil
                    { "BOOK_CREATED", 730 },       // 2 ans pour créations livres
                    { "BOOK_DELETED", 730 },       // 2 ans pour suppressions livres
                    { "BOOK_DOWNLOADED", 90 },     // 3 mois pour téléchargements
                    { "BOOK_VIEWED", 30 },         // 1 mois pour consultations
                    { "FAVORITE_ADDED", 90 },      // 3 mois pour ajouts favoris
                    { "FAVORITE_REMOVED", 90 },    // 3 mois pour suppressions favoris
                    { "UNAUTHORIZED_ACCESS", 365 }, // 1 an pour erreurs sécurité
                    { "RATE_LIMIT_EXCEEDED", 90 }, // 3 mois pour rate limiting
                    { "SYSTEM_ERROR", 365 },       // 1 an pour erreurs système
                    { "DEFAULT", 180 }             // 6 mois par défaut
                };

                _logger.LogWarning("⚠️ Aucune politique de rétention configurée, utilisation des valeurs par défaut");
            }

            return policies;
        }

        /// <summary>
        /// Archive les logs avant suppression
        /// </summary>
        private async Task ArchiveLogsAsync(List<AuditLog> logs, string actionType)
        {
            try
            {
                // Création du dossier d'archive si nécessaire
                if (!Directory.Exists(_archivePath))
                {
                    Directory.CreateDirectory(_archivePath);
                    _logger.LogInformation("📁 Dossier d'archive créé : {ArchivePath}", _archivePath);
                }

                // Nom du fichier d'archive
                var fileName = $"audit_archive_{actionType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                var filePath = Path.Combine(_archivePath, fileName);

                // Export en CSV
                var csvContent = ExportToCsv(logs);
                await File.WriteAllTextAsync(filePath, csvContent);

                _logger.LogInformation("💾 {Count} logs {ActionType} archivés vers {FileName}", 
                                      logs.Count, actionType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'archivage des logs {ActionType}", actionType);
                // Ne pas faire échouer le nettoyage si l'archivage échoue
            }
        }

        /// <summary>
        /// Exporte une liste de logs en format CSV
        /// </summary>
        private string ExportToCsv(List<AuditLog> logs)
        {
            var sb = new StringBuilder();
            
            // En-têtes CSV
            sb.AppendLine("Id,UserId,Action,Message,CreatedAt,IpAddress");

            // Données
            foreach (var log in logs)
            {
                var escapedMessage = log.Message?.Replace("\"", "\"\"") ?? "";
                sb.AppendLine($"{log.Id},{log.UserId},{log.Action}," +
                             $"\"{escapedMessage}\",{log.CreatedAt:yyyy-MM-dd HH:mm:ss},{log.IpAddress}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Crée un log d'audit pour l'opération de nettoyage elle-même
        /// </summary>
        private async Task LogCleanupAuditAsync(ApplicationDbContext context, int totalDeleted, Dictionary<string, int> stats)
        {
            try
            {
                var statsMessage = string.Join(", ", stats.Select(s => $"{s.Key}: {s.Value}"));
                
                var auditEntry = new AuditLog
                {
                    UserId = "SYSTEM",
                    Action = "AUDIT_CLEANUP",
                    Message = $"Nettoyage automatique des logs : {totalDeleted} entrées supprimées. Détail: {statsMessage}",
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = "127.0.0.1"
                };

                context.AuditLogs.Add(auditEntry);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Impossible de créer l'audit du nettoyage");
                // Non bloquant
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 Arrêt du service de nettoyage des logs d'audit...");
            await base.StopAsync(stoppingToken);
            _logger.LogInformation("✅ Service de nettoyage arrêté");
        }
    }
}