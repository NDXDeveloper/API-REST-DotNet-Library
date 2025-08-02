using System.Text;
using System.Text.Json;
using LibraryAPI.Models;

namespace LibraryAPI.Services
{
    /// <summary>
    /// SERVICE D'ARCHIVAGE DES LOGS D'AUDIT
    /// 
    /// Ce service gère l'archivage des logs d'audit avant leur suppression.
    /// Supporte plusieurs formats d'export (CSV, JSON) et compression optionnelle.
    /// </summary>
    public class AuditArchiveService
    {
        private readonly ILogger<AuditArchiveService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _archivePath;

        public AuditArchiveService(ILogger<AuditArchiveService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _archivePath = configuration.GetValue<string>("AuditSettings:ArchivePath", "archives/audit") ?? "archives/audit";
        }

        /// <summary>
        /// Archive une liste de logs dans le format spécifié
        /// </summary>
        /// <param name="logs">Logs à archiver</param>
        /// <param name="actionType">Type d'action pour nommer le fichier</param>
        /// <param name="format">Format d'export (CSV, JSON)</param>
        /// <param name="compress">Compresser le fichier d'archive</param>
        /// <returns>Chemin du fichier d'archive créé</returns>
        public async Task<string> ArchiveLogsAsync(List<AuditLog> logs, string actionType, 
            ArchiveFormat format = ArchiveFormat.CSV, bool compress = false)
        {
            if (!logs.Any())
            {
                _logger.LogWarning("⚠️ Aucun log à archiver pour le type {ActionType}", actionType);
                return string.Empty;
            }

            try
            {
                // Création du dossier d'archive si nécessaire
                EnsureArchiveDirectoryExists();

                // Génération du nom de fichier
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var extension = format == ArchiveFormat.CSV ? "csv" : "json";
                var fileName = $"audit_archive_{SanitizeActionType(actionType)}_{timestamp}.{extension}";
                
                if (compress)
                {
                    fileName += ".gz";
                }

                var filePath = Path.Combine(_archivePath, fileName);

                // Export selon le format choisi
                string content = format switch
                {
                    ArchiveFormat.CSV => ExportToCsv(logs),
                    ArchiveFormat.JSON => ExportToJson(logs),
                    _ => throw new ArgumentException($"Format non supporté: {format}")
                };

                // Écriture du fichier (avec compression optionnelle)
                if (compress)
                {
                    await WriteCompressedFileAsync(filePath, content);
                }
                else
                {
                    await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                }

                var fileInfo = new FileInfo(filePath);
                _logger.LogInformation("💾 {Count} logs {ActionType} archivés vers {FileName} ({FileSize} Ko)", 
                                      logs.Count, actionType, fileName, fileInfo.Length / 1024);

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'archivage des logs {ActionType}", actionType);
                throw;
            }
        }

        /// <summary>
        /// Archive avec métadonnées enrichies
        /// </summary>
        public async Task<string> ArchiveLogsWithMetadataAsync(List<AuditLog> logs, string actionType, 
            DateTime cutoffDate, ArchiveFormat format = ArchiveFormat.JSON)
        {
            var metadata = new ArchiveMetadata
            {
                ActionType = actionType,
                CutoffDate = cutoffDate,
                ArchiveDate = DateTime.UtcNow,
                LogCount = logs.Count,
                DateRange = logs.Any() ? new DateRange
                {
                    StartDate = logs.Min(l => l.CreatedAt),
                    EndDate = logs.Max(l => l.CreatedAt)
                } : null,
                Statistics = GenerateStatistics(logs)
            };

            var archiveData = new ArchiveData
            {
                Metadata = metadata,
                Logs = logs.Select(l => new ArchivedAuditLog
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    Action = l.Action,
                    Message = l.Message,
                    CreatedAt = l.CreatedAt,
                    IpAddress = l.IpAddress
                }).ToList()
            };

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"audit_archive_{SanitizeActionType(actionType)}_{timestamp}_with_metadata.json";
            var filePath = Path.Combine(_archivePath, fileName);

            EnsureArchiveDirectoryExists();

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(archiveData, jsonOptions);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

            _logger.LogInformation("💾 {Count} logs {ActionType} archivés avec métadonnées vers {FileName}", 
                                  logs.Count, actionType, fileName);

            return filePath;
        }

        /// <summary>
        /// Exporte les logs au format CSV
        /// </summary>
        private string ExportToCsv(List<AuditLog> logs)
        {
            var sb = new StringBuilder();
            
            // En-têtes CSV avec échappement
            sb.AppendLine("Id,UserId,Action,Message,CreatedAt,IpAddress");

            // Données avec échappement approprié
            foreach (var log in logs.OrderBy(l => l.CreatedAt))
            {
                sb.AppendLine(string.Join(",", 
                    EscapeCsvValue(log.Id.ToString()),
                    EscapeCsvValue(log.UserId ?? ""),
                    EscapeCsvValue(log.Action ?? ""),
                    EscapeCsvValue(log.Message ?? ""),
                    EscapeCsvValue(log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsvValue(log.IpAddress ?? "")
                ));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Exporte les logs au format JSON
        /// </summary>
        private string ExportToJson(List<AuditLog> logs)
        {
            var exportData = logs.OrderBy(l => l.CreatedAt).Select(l => new
            {
                l.Id,
                l.UserId,
                l.Action,
                l.Message,
                CreatedAt = l.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                l.IpAddress
            });

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(exportData, options);
        }

        /// <summary>
        /// Écrit un fichier compressé avec Gzip
        /// </summary>
        private async Task WriteCompressedFileAsync(string filePath, string content)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create);
            using var gzipStream = new System.IO.Compression.GZipStream(fileStream, System.IO.Compression.CompressionMode.Compress);
            using var writer = new StreamWriter(gzipStream, Encoding.UTF8);
            
            await writer.WriteAsync(content);
        }

        /// <summary>
        /// Échappe une valeur pour CSV
        /// </summary>
        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            // Si la valeur contient des guillemets, virgules ou retours à la ligne
            if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            {
                // Échapper les guillemets en les doublant et entourer de guillemets
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return $"\"{value}\"";
        }

        /// <summary>
        /// Nettoie le type d'action pour un nom de fichier valide
        /// </summary>
        private string SanitizeActionType(string actionType)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(actionType.Where(c => !invalid.Contains(c)).ToArray());
            return string.IsNullOrEmpty(sanitized) ? "UNKNOWN" : sanitized;
        }

        /// <summary>
        /// S'assure que le dossier d'archive existe
        /// </summary>
        private void EnsureArchiveDirectoryExists()
        {
            if (!Directory.Exists(_archivePath))
            {
                Directory.CreateDirectory(_archivePath);
                _logger.LogInformation("📁 Dossier d'archive créé : {ArchivePath}", _archivePath);
            }
        }

        /// <summary>
        /// Génère des statistiques sur les logs à archiver
        /// </summary>
        private ArchiveStatistics GenerateStatistics(List<AuditLog> logs)
        {
            if (!logs.Any())
                return new ArchiveStatistics();

            var uniqueUsers = logs.Select(l => l.UserId).Distinct().Count();
            var uniqueActions = logs.Select(l => l.Action).Distinct().Count();
            var topActions = logs.GroupBy(l => l.Action)
                               .OrderByDescending(g => g.Count())
                               .Take(5)
                               .ToDictionary(g => g.Key ?? "UNKNOWN", g => g.Count());

            return new ArchiveStatistics
            {
                TotalLogs = logs.Count,
                UniqueUsers = uniqueUsers,
                UniqueActions = uniqueActions,
                TopActions = topActions,
                DateSpan = logs.Max(l => l.CreatedAt) - logs.Min(l => l.CreatedAt)
            };
        }

        /// <summary>
        /// Liste les fichiers d'archive existants
        /// </summary>
        public List<ArchiveFileInfo> ListArchiveFiles()
        {
            var archiveFiles = new List<ArchiveFileInfo>();

            if (!Directory.Exists(_archivePath))
                return archiveFiles;

            try
            {
                var files = Directory.GetFiles(_archivePath, "audit_archive_*.*")
                                   .Where(f => f.EndsWith(".csv") || f.EndsWith(".json") || f.EndsWith(".gz"))
                                   .OrderByDescending(f => new FileInfo(f).CreationTime);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var archiveInfo = new ArchiveFileInfo
                    {
                        FileName = fileInfo.Name,
                        FilePath = fileInfo.FullName,
                        SizeBytes = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTime,
                        IsCompressed = fileInfo.Extension == ".gz"
                    };

                    // Essayer d'extraire des infos du nom de fichier
                    ParseFileNameInfo(archiveInfo);
                    archiveFiles.Add(archiveInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la lecture des fichiers d'archive");
            }

            return archiveFiles;
        }

        /// <summary>
        /// Parse les informations depuis le nom du fichier d'archive
        /// </summary>
        private void ParseFileNameInfo(ArchiveFileInfo archiveInfo)
        {
            try
            {
                // Format attendu: audit_archive_{ActionType}_{timestamp}.{extension}
                var nameParts = Path.GetFileNameWithoutExtension(archiveInfo.FileName).Split('_');
                
                if (nameParts.Length >= 4 && nameParts[0] == "audit" && nameParts[1] == "archive")
                {
                    // Reconstituer l'ActionType (peut contenir des underscores)
                    var actionTypeParts = nameParts.Skip(2).Take(nameParts.Length - 3).ToArray();
                    archiveInfo.ActionType = string.Join("_", actionTypeParts);

                    // Le timestamp est le dernier élément
                    var timestampStr = nameParts.Last();
                    if (DateTime.TryParseExact(timestampStr, "yyyyMMdd_HHmmss", null, 
                        System.Globalization.DateTimeStyles.None, out DateTime timestamp))
                    {
                        archiveInfo.ArchiveTimestamp = timestamp;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "⚠️ Impossible de parser le nom de fichier {FileName}", archiveInfo.FileName);
            }
        }

        /// <summary>
        /// Supprime les anciens fichiers d'archive
        /// </summary>
        public /*async*/ Task<int> CleanupOldArchivesAsync(TimeSpan maxAge)
        {
            var cutoffDate = DateTime.UtcNow - maxAge;
            var deletedCount = 0;

            try
            {
                var archiveFiles = ListArchiveFiles()
                    .Where(f => f.CreatedAt < cutoffDate)
                    .ToList();

                foreach (var file in archiveFiles)
                {
                    try
                    {
                        File.Delete(file.FilePath);
                        deletedCount++;
                        _logger.LogInformation("🗑️ Fichier d'archive supprimé : {FileName}", file.FileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Impossible de supprimer le fichier d'archive {FileName}", file.FileName);
                    }
                }

                if (deletedCount > 0)
                {
                    _logger.LogInformation("✅ {Count} anciens fichiers d'archive supprimés", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du nettoyage des archives");
            }

            /*return deletedCount;*/
            return Task.FromResult(deletedCount);
        }
    }

    /// <summary>
    /// Format d'archive supporté
    /// </summary>
    public enum ArchiveFormat
    {
        CSV,
        JSON
    }

    /// <summary>
    /// Métadonnées d'une archive
    /// </summary>
    public class ArchiveMetadata
    {
        public string ActionType { get; set; } = string.Empty;
        public DateTime CutoffDate { get; set; }
        public DateTime ArchiveDate { get; set; }
        public int LogCount { get; set; }
        public DateRange? DateRange { get; set; }
        public ArchiveStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// Données d'archive complètes
    /// </summary>
    public class ArchiveData
    {
        public ArchiveMetadata Metadata { get; set; } = new();
        public List<ArchivedAuditLog> Logs { get; set; } = new();
    }

    /// <summary>
    /// Log d'audit archivé (version simplifiée)
    /// </summary>
    public class ArchivedAuditLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Action { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// Plage de dates
    /// </summary>
    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// Statistiques d'archive
    /// </summary>
    public class ArchiveStatistics
    {
        public int TotalLogs { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueActions { get; set; }
        public Dictionary<string, int> TopActions { get; set; } = new();
        public TimeSpan DateSpan { get; set; }
    }

    /// <summary>
    /// Informations sur un fichier d'archive
    /// </summary>
    public class ArchiveFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCompressed { get; set; }
        public string? ActionType { get; set; }
        public DateTime? ArchiveTimestamp { get; set; }
        
        public string SizeFormatted => FormatBytes(SizeBytes);
        
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
}