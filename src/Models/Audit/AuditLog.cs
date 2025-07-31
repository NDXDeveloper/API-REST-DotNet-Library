using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)] // Compatible avec ASP.NET Identity
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optionnel : IP pour sécurité
        [StringLength(45)]
        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// CONSTANTES POUR LES ACTIONS D'AUDIT
    /// Standardise les noms d'actions pour faciliter les requêtes et analyses
    /// </summary>
    public static class AuditActions
    {
        // === ACTIONS UTILISATEUR ===
        public const string LOGIN_SUCCESS = "LOGIN_SUCCESS";
        public const string LOGIN_FAILED = "LOGIN_FAILED";
        public const string LOGOUT = "LOGOUT";
        public const string REGISTER = "REGISTER";
        public const string PASSWORD_CHANGED = "PASSWORD_CHANGED";
        public const string PROFILE_UPDATED = "PROFILE_UPDATED";

        // === ACTIONS LIVRE/MAGAZINE ===
        public const string BOOK_CREATED = "BOOK_CREATED";
        public const string BOOK_UPDATED = "BOOK_UPDATED";
        public const string BOOK_DELETED = "BOOK_DELETED";
        public const string BOOK_DOWNLOADED = "BOOK_DOWNLOADED";
        public const string BOOK_VIEWED = "BOOK_VIEWED";
        public const string BOOK_RATED = "BOOK_RATED";
        public const string BOOK_COMMENTED = "BOOK_COMMENTED";

        // === ACTIONS FAVORIS ===
        public const string FAVORITE_ADDED = "FAVORITE_ADDED";
        public const string FAVORITE_REMOVED = "FAVORITE_REMOVED";

        // === ACTIONS ADMINISTRATION ===
        public const string USER_ROLE_CHANGED = "USER_ROLE_CHANGED";
        public const string USER_DELETED = "USER_DELETED";
        public const string NOTIFICATION_SENT = "NOTIFICATION_SENT";

        // === ÉVÉNEMENTS SÉCURITÉ ===
        public const string UNAUTHORIZED_ACCESS = "UNAUTHORIZED_ACCESS";
        public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
        public const string SUSPICIOUS_ACTIVITY = "SUSPICIOUS_ACTIVITY";
        public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";

        // === ÉVÉNEMENTS SYSTÈME ===
        public const string SYSTEM_ERROR = "SYSTEM_ERROR";
        public const string SYSTEM_STARTUP = "SYSTEM_STARTUP";
        public const string SYSTEM_SHUTDOWN = "SYSTEM_SHUTDOWN";
    }
    
}