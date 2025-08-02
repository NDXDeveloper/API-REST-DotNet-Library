using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models.DTOs
{
    /// <summary>
    /// Modèle pour les requêtes de nettoyage manuel des logs d'audit
    /// </summary>
    public class CleanupRequest
    {
        /// <summary>
        /// Nombre de jours de rétention (logs plus anciens seront supprimés)
        /// </summary>
        [Range(1, 3650, ErrorMessage = "La durée de rétention doit être entre 1 et 3650 jours")]
        public int RetentionDays { get; set; } = 90;

        /// <summary>
        /// Type d'action spécifique à nettoyer (optionnel, si vide = tous les types)
        /// </summary>
        [StringLength(100, ErrorMessage = "Le type d'action ne peut dépasser 100 caractères")]
        public string? ActionType { get; set; }

        /// <summary>
        /// Archiver avant suppression
        /// </summary>
        public bool ArchiveBeforeDelete { get; set; } = false;

        /// <summary>
        /// Mode preview (ne supprime pas réellement, retourne juste le nombre)
        /// </summary>
        public bool PreviewOnly { get; set; } = false;
    }

    /// <summary>
    /// Réponse pour les opérations de nettoyage
    /// </summary>
    public class CleanupResponse
    {
        /// <summary>
        /// Message de résultat
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de logs supprimés
        /// </summary>
        public int DeletedCount { get; set; }

        /// <summary>
        /// Date de coupure utilisée
        /// </summary>
        public DateTime CutoffDate { get; set; }

        /// <summary>
        /// Statistiques détaillées par type d'action
        /// </summary>
        public Dictionary<string, int> DetailedStats { get; set; } = new();

        /// <summary>
        /// Chemin du fichier d'archive (si archivage activé)
        /// </summary>
        public string? ArchiveFilePath { get; set; }

        /// <summary>
        /// Durée de l'opération en millisecondes
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Indique si c'était un aperçu (preview)
        /// </summary>
        public bool IsPreview { get; set; }
    }

    /// <summary>
    /// Statistiques de la base de données d'audit
    /// </summary>
    public class AuditDatabaseStats
    {
        /// <summary>
        /// Nombre total de logs d'audit
        /// </summary>
        public int TotalLogs { get; set; }

        /// <summary>
        /// Nombre de logs des 7 derniers jours
        /// </summary>
        public int LogsLast7Days { get; set; }

        /// <summary>
        /// Nombre de logs des 30 derniers jours
        /// </summary>
        public int LogsLast30Days { get; set; }

        /// <summary>
        /// Date du log le plus ancien
        /// </summary>
        public DateTime? OldestLog { get; set; }

        /// <summary>
        /// Date du log le plus récent
        /// </summary>
        public DateTime? NewestLog { get; set; }

        /// <summary>
        /// Top 10 des actions les plus fréquentes
        /// </summary>
        public List<ActionStatistic> TopActions { get; set; } = new();

        /// <summary>
        /// Distribution par mois (6 derniers mois)
        /// </summary>
        public List<MonthlyStatistic> MonthlyDistribution { get; set; } = new();

        /// <summary>
        /// Estimation de la taille en base (approximative)
        /// </summary>
        public DatabaseSizeEstimate SizeEstimate { get; set; } = new();
    }

    /// <summary>
    /// Statistique pour un type d'action
    /// </summary>
    public class ActionStatistic
    {
        /// <summary>
        /// Type d'action
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Nombre d'occurrences
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Pourcentage du total
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// Date de la première occurrence
        /// </summary>
        public DateTime? FirstOccurrence { get; set; }

        /// <summary>
        /// Date de la dernière occurrence
        /// </summary>
        public DateTime? LastOccurrence { get; set; }
    }

    /// <summary>
    /// Statistique mensuelle
    /// </summary>
    public class MonthlyStatistic
    {
        /// <summary>
        /// Année et mois (format YYYY-MM)
        /// </summary>
        public string YearMonth { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de logs pour ce mois
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Actions les plus fréquentes ce mois-là
        /// </summary>
        public List<string> TopActionsThisMonth { get; set; } = new();
    }

    /// <summary>
    /// Estimation de la taille de la base de données
    /// </summary>
    public class DatabaseSizeEstimate
    {
        /// <summary>
        /// Taille estimée en Ko
        /// </summary>
        public long EstimatedSizeKB { get; set; }

        /// <summary>
        /// Taille moyenne par log (en octets)
        /// </summary>
        public double AverageSizePerLog { get; set; }

        /// <summary>
        /// Croissance estimée par jour (en Ko)
        /// </summary>
        public double DailyGrowthKB { get; set; }

        /// <summary>
        /// Prédiction pour 30 jours (en Ko)
        /// </summary>
        public double Predicted30DaysKB { get; set; }
    }

    /// <summary>
    /// Configuration des politiques de rétention
    /// </summary>
    public class RetentionPolicyConfig
    {
        /// <summary>
        /// Politiques par type d'action (ActionType -> Jours de rétention)
        /// </summary>
        public Dictionary<string, int> Policies { get; set; } = new();

        /// <summary>
        /// Politique par défaut en jours
        /// </summary>
        public int DefaultRetentionDays { get; set; } = 180;

        /// <summary>
        /// Activer le nettoyage automatique
        /// </summary>
        public bool AutoCleanupEnabled { get; set; } = true;

        /// <summary>
        /// Intervalle de nettoyage en heures
        /// </summary>
        public int CleanupIntervalHours { get; set; } = 24;

        /// <summary>
        /// Archiver avant suppression
        /// </summary>
        public bool ArchiveBeforeDelete { get; set; } = false;

        /// <summary>
        /// Chemin du dossier d'archives
        /// </summary>
        public string ArchivePath { get; set; } = "archives/audit";
    }
}