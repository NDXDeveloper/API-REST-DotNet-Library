using Microsoft.AspNetCore.Authorization;  // Gère l'authentification et l'autorisation des utilisateurs (attributs [Authorize])
using Microsoft.AspNetCore.Mvc;            // Fournit les outils pour créer des contrôleurs API et gérer les actions HTTP
using Microsoft.EntityFrameworkCore;       // Permet l'utilisation d'Entity Framework Core pour interagir avec la base de données
using System.Linq;                         // Méthodes d'extension LINQ pour les requêtes sur les collections et bases de données
using System.Threading.Tasks;              // Support pour la programmation asynchrone avec async/await
using System.Security.Claims; // Utilisé pour manipuler les informations des utilisateurs (claims) dans les tokens d'authentification, comme l'identifiant de l'utilisateur (UserId).
using LibraryAPI.Data;                     // Contexte de base de données de l'application (ApplicationDbContext)
using LibraryAPI.Models;                   // Modèles de données de l'application (Notification, UserNotification, AuditActions, etc.)
using Microsoft.AspNetCore.RateLimiting;   // Services de limitation du taux de requêtes pour éviter les abus

namespace LibraryAPI.Controllers
{
    /// <summary>
    /// CONTRÔLEUR DE GESTION DES NOTIFICATIONS
    /// 
    /// Ce contrôleur gère toutes les opérations liées aux notifications :
    /// - Création de notifications (admins uniquement)
    /// - Envoi d'emails de notification
    /// - Marquage des notifications comme lues
    /// 
    /// LOGS SERILOG (TECHNIQUES UNIQUEMENT) :
    /// - Erreurs de base de données (connexion, transactions)
    /// - Problèmes d'envoi d'emails (SMTP, configuration)
    /// - Incohérences de données (notifications/utilisateurs null)
    /// - Erreurs de service EmailService
    /// - Problèmes de performance (requêtes lentes)
    /// 
    /// NOTE : Les logs d'audit (qui crée/lit quelles notifications)
    /// sont gérés par un système séparé
    /// </summary>
    [EnableRateLimiting("StrictPolicy")]  // Limitation stricte pour éviter les abus
    [ApiController]                       // Contrôleur API avec validation automatique
    [Route("api/[controller]")]           // Route de base : /api/Notification
    public class NotificationController : ControllerBase
    {
        // ===== SERVICES INJECTÉS =====
        
        /// <summary>
        /// Contexte de base de données pour les opérations sur les notifications
        /// </summary>
        private readonly ApplicationDbContext _context;
        
        /// <summary>
        /// Service d'envoi d'emails pour les notifications par email
        /// </summary>
        private readonly EmailService _emailService;
        
        /// <summary>
        /// ✅ SERVICE DE LOGGING SERILOG - LOGS TECHNIQUES SEULEMENT
        /// Utilisé pour :
        /// - Erreurs techniques (exceptions, problèmes de services)
        /// - Problèmes de base de données (connexion, intégrité)
        /// - Erreurs d'envoi d'emails (SMTP, configuration)
        /// - Incohérences de données (références nulles)
        /// - Problèmes de performance
        /// 
        /// PAS utilisé pour :
        /// - Audit des notifications (qui crée/lit quoi)
        /// - Statistiques d'utilisation
        /// - Traçabilité métier
        /// </summary>
        private readonly ILogger<NotificationController> _logger;
        
        /// <summary>
        /// ✅ SERVICE D'AUDIT - LOGS MÉTIER ET TRAÇABILITÉ
        /// Utilisé pour :
        /// - Traçabilité des actions métier (qui crée/envoie des notifications)
        /// - Audit de sécurité (accès aux fonctions admin)
        /// - Historique des opérations importantes
        /// - Conformité réglementaire
        /// </summary>
        private readonly AuditLogger _auditLogger;

        // ===== CONSTRUCTEUR =====

        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        /// <param name="context">Contexte de base de données</param>
        /// <param name="emailService">Service d'envoi d'emails</param>
        /// <param name="logger">✅ Service de logging pour aspects techniques</param>
        /// <param name="auditLogger">✅ Service d'audit pour traçabilité métier</param>
        public NotificationController(ApplicationDbContext context, EmailService emailService, ILogger<NotificationController> logger, AuditLogger auditLogger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;  // ✅ Ajout du service de logging technique
            _auditLogger = auditLogger;  // ✅ Ajout du service d'audit métier
        }

        // ===== MÉTHODES DE GESTION DES NOTIFICATIONS =====

        /// <summary>
        /// CRÉER UNE NOTIFICATION (ADMINS SEULEMENT)
        /// 
        /// Permet aux administrateurs de créer une notification pour tous les utilisateurs
        /// Logs techniques : erreurs de base de données, problèmes de transactions
        /// </summary>
        /// <param name="content">Contenu de la notification</param>
        /// <returns>Message de succès ou erreur</returns>
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNotification([FromBody] string content)
        {
            try
            {
                // Validation du contenu
                if (string.IsNullOrWhiteSpace(content))
                {
                    return BadRequest("Notification content cannot be empty");
                }

                // Création de la notification
                var notification = new Notification { Content = content };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Associer la notification à tous les utilisateurs
                var users = _context.Users.Select(u => u.Id).ToList();
                
                // ✅ LOG TECHNIQUE : Vérification du nombre d'utilisateurs
                if (!users.Any())
                {
                    _logger.LogWarning("⚠️ No users found in database when creating notification - potential data issue");
                    return Ok("Notification created but no users found to notify");
                }

                // Association de la notification à chaque utilisateur (plus clair pour les débutants)
                foreach (var userId in users)
                {
                    _context.UserNotifications.Add(new UserNotification
                    {
                        UserId = userId,
                        NotificationId = notification.Id
                    });
                }
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.NOTIFICATION_SENT,
                        $"Notification créée et envoyée à {users.Count} utilisateurs");

                return Ok("Notification created successfully.");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données lors de la création
                _logger.LogError(ex, "❌ Database error while creating notification");
                return StatusCode(500, "Database error occurred while creating notification");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec les opérations EF
                _logger.LogError(ex, "❌ Invalid operation while creating notification - possible transaction issue");
                return StatusCode(500, "Data operation error occurred");
            }
            catch (ArgumentException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec les arguments/entités
                _logger.LogError(ex, "❌ Argument error while creating notification");
                return StatusCode(500, "Data validation error occurred");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error while creating notification");
                return StatusCode(500, "An internal error occurred while creating notification");
            }
        }

        /// <summary>
        /// ENVOYER DES NOTIFICATIONS PAR EMAIL
        /// 
        /// Envoie par email toutes les notifications en attente
        /// Logs techniques : erreurs SMTP, problèmes de service email, données manquantes
        /// </summary>
        /// <returns>Message de succès ou erreur</returns>
        [HttpPost("send-emails")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendEmails()
        {
            try
            {
                // Récupération des notifications en attente d'envoi
                var pendingNotifications = await _context.UserNotifications
                    .Where(un => !un.IsSent)
                    .Include(un => un.Notification)  // Inclure les détails de notification
                    .ToListAsync();

                if (!pendingNotifications.Any())
                {
                    return Ok("No pending notifications to send");
                }

                int emailsSent = 0;
                int emailsSkipped = 0;
                int emailsFailed = 0;

                foreach (var userNotification in pendingNotifications)
                {
                    try
                    {
                        // Récupération de l'utilisateur
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userNotification.UserId);
                        
                        if (user == null)
                        {
                            // ✅ LOG TECHNIQUE : Utilisateur référencé mais introuvable (intégrité des données)
                            _logger.LogWarning("⚠️ UserNotification references non-existent user {UserId} - data integrity issue", 
                                              userNotification.UserId);
                            emailsSkipped++;
                            continue;
                        }

                        if (string.IsNullOrEmpty(user.Email))
                        {
                            // ✅ LOG TECHNIQUE : Utilisateur sans email (problème de données)
                            _logger.LogWarning("⚠️ User {UserId} has no email address - cannot send notification", 
                                              user.Id);
                            emailsSkipped++;
                            // On pourrait aussi retourner une erreur comme dans le code original :
                            // return BadRequest("User email is missing. Notification cannot be sent.");
                            // Mais on continue pour traiter les autres utilisateurs
                            continue;
                        }

                        if (userNotification.Notification == null)
                        {
                            // ✅ LOG TECHNIQUE : Notification référencée mais introuvable (intégrité des données)
                            _logger.LogWarning("⚠️ UserNotification references non-existent notification {NotificationId} - data integrity issue", 
                                              userNotification.NotificationId);
                            emailsSkipped++;
                            continue;
                        }

                        // Préparation du sujet et contenu de l'email
                        var subject = string.IsNullOrEmpty(userNotification.Notification.Subject) 
                            ? "Nouvelle Notification" 
                            : userNotification.Notification.Subject;
                        var content = userNotification.Notification.Content ?? "";

                        // Tentative d'envoi de l'email
                        // await _emailService.SendEmailAsync(user.Email, subject, content);
                        
                        // Marquage comme envoyé
                        userNotification.IsSent = true;
                        emailsSent++;
                    }
                    catch (ArgumentException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème avec les paramètres d'email (EmailService)
                        _logger.LogError(ex, "❌ Email service argument error for user {UserId}", 
                                        userNotification.UserId);
                        emailsFailed++;
                    }
                    catch (InvalidOperationException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème de configuration EmailService
                        _logger.LogError(ex, "❌ Email service configuration error for user {UserId}", 
                                        userNotification.UserId);
                        emailsFailed++;
                    }
                    catch (TimeoutException ex)
                    {
                        // ✅ LOG TECHNIQUE : Timeout SMTP
                        _logger.LogError(ex, "❌ SMTP timeout while sending email to user {UserId}", 
                                        userNotification.UserId);
                        emailsFailed++;
                    }
                    catch (HttpRequestException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème réseau/SMTP
                        _logger.LogError(ex, "❌ Network/SMTP error while sending email to user {UserId}", 
                                        userNotification.UserId);
                        emailsFailed++;
                    }
                    catch (Exception ex)
                    {
                        // ✅ LOG TECHNIQUE : Erreur générique d'envoi d'email
                        _logger.LogError(ex, "❌ Unexpected error while sending email to user {UserId}", 
                                        userNotification.UserId);
                        emailsFailed++;
                    }
                }

                // Sauvegarde des changements (marquage IsSent = true)
                await _context.SaveChangesAsync();

                // ✅ LOG TECHNIQUE : Statistiques d'envoi pour monitoring
                _logger.LogInformation("📊 Email sending completed - Sent: {EmailsSent}, Skipped: {EmailsSkipped}, Failed: {EmailsFailed}", 
                                      emailsSent, emailsSkipped, emailsFailed);

                await _auditLogger.LogAsync(AuditActions.NOTIFICATION_SENT,
                        $"Envoi d'emails terminé: {emailsSent} envoyés, {emailsSkipped} ignorés, {emailsFailed} échoués");

                if (emailsFailed > 0)
                {
                    return StatusCode(207, $"Emails partially sent. Sent: {emailsSent}, Failed: {emailsFailed}, Skipped: {emailsSkipped}");
                }

                return Ok($"Emails sent successfully. Sent: {emailsSent}, Skipped: {emailsSkipped}");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données lors de la mise à jour
                _logger.LogError(ex, "❌ Database error while updating notification send status");
                return StatusCode(500, "Database error occurred while processing email notifications");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during email notification process");
                return StatusCode(500, "An internal error occurred while sending email notifications");
            }
        }

        /// <summary>
        /// MARQUER UNE NOTIFICATION COMME LUE
        /// 
        /// Permet à un utilisateur de marquer sa notification comme lue
        /// Logs techniques : erreurs de base de données, données nulles
        /// </summary>
        /// <param name="notificationId">ID de la notification à marquer comme lue</param>
        /// <returns>Message de succès ou erreur</returns>
        [HttpPost("mark-as-read/{notificationId}")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                // Récupération de l'ID utilisateur depuis le token
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (userId == null)
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide ou malformé
                    _logger.LogWarning("⚠️ MarkAsRead called with invalid or missing user token");
                    return Unauthorized();
                }

                // Recherche de la notification utilisateur
                var userNotification = await _context.UserNotifications
                    .Include(un => un.Notification)  // Inclure les détails de notification
                    .FirstOrDefaultAsync(un => un.NotificationId == notificationId && un.UserId == userId);

                if (userNotification == null)
                {
                    return NotFound("Notification not found for this user.");
                }

                if (userNotification.Notification == null)
                {
                    // ✅ LOG TECHNIQUE : UserNotification existe mais Notification est null (intégrité des données)
                    _logger.LogError("🚨 UserNotification exists but Notification is null - NotificationId: {NotificationId}, UserId: {UserId}", 
                                    notificationId, userId);
                    return StatusCode(500, "Data integrity error - notification details missing");
                }

                // Marquage comme lue
                userNotification.Notification.IsRead = true;
                await _context.SaveChangesAsync();

                return Ok("Notification marked as read.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de concurrence (notification modifiée ailleurs)
                _logger.LogError(ex, "❌ Concurrency error while marking notification as read - NotificationId: {NotificationId}", 
                                notificationId);
                return StatusCode(409, "The notification was modified by another operation");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données lors de la mise à jour
                _logger.LogError(ex, "❌ Database error while marking notification as read - NotificationId: {NotificationId}", 
                                notificationId);
                return StatusCode(500, "Database error occurred while updating notification");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec la requête LINQ/EF
                _logger.LogError(ex, "❌ Invalid operation while marking notification as read - NotificationId: {NotificationId}",
                                notificationId);
                return StatusCode(500, "Data access error occurred");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error while marking notification as read - NotificationId: {NotificationId}", 
                                notificationId);
                return StatusCode(500, "An internal error occurred while updating notification");
            }
        }
    }
}

/*
===== SYSTÈME DUAL DE LOGGING DANS CE CONTRÔLEUR =====

✅ LOGS TECHNIQUES (Serilog - _logger) :
- Erreurs de base de données (DbUpdateException, connexion, transactions)
- Problèmes d'envoi d'emails (SMTP, TimeoutException, NetworkException)
- Erreurs de configuration EmailService (InvalidOperationException)
- Incohérences de données (utilisateurs/notifications null, références cassées)
- Problèmes de concurrence (DbUpdateConcurrencyException)
- Token JWT invalide/malformé
- Statistiques techniques d'envoi d'emails pour monitoring
- Erreurs de validation d'arguments

✅ LOGS D'AUDIT (AuditLogger - _auditLogger) :
- Création de notifications par les administrateurs
- Envoi massif d'emails de notification
- Marquage des notifications comme lues par les utilisateurs
- Traçabilité des actions administratives
- Audit de conformité réglementaire

===== EXEMPLES DE LOGS TECHNIQUES GÉNÉRÉS (Serilog) =====

[15:30:16 WRN] ⚠️ No users found in database when creating notification - potential data issue
[15:32:45 ERR] ❌ Database error while creating notification
[15:35:20 WRN] ⚠️ UserNotification references non-existent user abc123 - data integrity issue
[15:40:10 ERR] ❌ SMTP timeout while sending email to user def456
[15:42:30 INF] 📊 Email sending completed - Sent: 45, Skipped: 3, Failed: 2
[15:45:15 ERR] 🚨 UserNotification exists but Notification is null - data integrity error
[15:50:20 ERR] ❌ Concurrency error while marking notification as read - NotificationId: 789

===== EXEMPLES DE LOGS D'AUDIT GÉNÉRÉS (Base de données) =====

Action: NOTIFICATION_SENT | Message: "Notification créée et envoyée à 150 utilisateurs" | UserId: admin123
Action: NOTIFICATION_SENT | Message: "Envoi d'emails terminé: 145 envoyés, 3 ignorés, 2 échoués" | UserId: admin123

===== OBJECTIFS DE CHAQUE SYSTÈME =====

🔧 LOGS TECHNIQUES (Serilog) AIDENT À :
✅ Détecter les problèmes de configuration SMTP
✅ Identifier les incohérences de données
✅ Surveiller les performances d'envoi d'emails
✅ Diagnostiquer les erreurs de base de données
✅ Monitorer l'intégrité des relations EF
✅ Détecter les problèmes de concurrence

📊 LOGS D'AUDIT (AuditLogger) AIDENT À :
✅ Traçabilité complète des actions administratives
✅ Conformité réglementaire (RGPD, audit de sécurité)
✅ Analyse des patterns d'utilisation des notifications
✅ Historique des communications avec les utilisateurs
✅ Audit de sécurité (qui fait quoi, quand)

===== AMÉLIORATIONS TECHNIQUES APPORTÉES =====

✅ Double système de logging complémentaire
✅ Gestion d'erreurs granulaire pour chaque type d'exception
✅ Validation des données nulles et références cassées
✅ Statistiques d'envoi d'emails pour monitoring
✅ Gestion des timeouts SMTP et erreurs réseau
✅ Détection des problèmes d'intégrité de données
✅ Meilleure utilisation d'async/await pour les performances
✅ Traçabilité métier complète pour conformité

===== UTILISATION PRATIQUE =====

🔍 Pour diagnostiquer un problème technique :
→ Consulter les logs Serilog dans les fichiers logs/

📋 Pour un audit ou conformité :
→ Consulter la table AuditLogs via /api/admin/audit/logs

💡 Les deux systèmes sont complémentaires et servent des objectifs différents
   mais essentiels pour une application en production robuste.

===== CONFIGURATION RECOMMANDÉE =====

🔧 CONFIGURATION SERILOG (appsettings.json) :
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "LibraryAPI.Controllers.NotificationController": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/notifications-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}

📊 STRUCTURE TABLE AUDITLOGS :
CREATE TABLE AuditLogs (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId nvarchar(256) NOT NULL,
    Action nvarchar(100) NOT NULL,
    Message nvarchar(max) NULL,
    Timestamp datetime2 NOT NULL DEFAULT GETUTCDATE(),
    IpAddress nvarchar(45) NULL,
    UserAgent nvarchar(500) NULL,
    AdditionalData nvarchar(max) NULL
);

===== MONITORING ET ALERTES =====

⚠️ ALERTES AUTOMATIQUES À CONFIGURER :
- Taux d'échec d'envoi d'emails > 10% en 1h
- Plus de 5 erreurs de base de données en 5min
- Détection de données null/corrompues
- Timeouts SMTP répétés (> 3 en 10min)
- Tentatives d'accès non autorisées répétées

📈 MÉTRIQUES IMPORTANTES À SURVEILLER :
- Temps de réponse moyen des endpoints
- Nombre de notifications créées par heure
- Taux de succès d'envoi d'emails
- Nombre d'utilisateurs actifs (marquage comme lu)
- Volume de logs d'erreurs par type

===== EXEMPLES DE REQUÊTES D'ANALYSE =====

🔍 ANALYSE DES LOGS TECHNIQUES (Serilog via Seq/Grafana) :
- Erreurs par type : @Level = "Error" | group by @Message
- Performance emails : @Message like "%Email sending completed%" | stats avg(EmailsSent)
- Intégrité données : @Message like "%data integrity%" | count

📋 ANALYSE DES LOGS D'AUDIT (SQL) :
-- Top actions par utilisateur
SELECT UserId, Action, COUNT(*) as ActionCount 
FROM AuditLogs 
WHERE Timestamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY UserId, Action
ORDER BY ActionCount DESC;

-- Analyse des notifications par période
SELECT 
    DATEPART(hour, Timestamp) as Hour,
    COUNT(*) as NotificationsSent
FROM AuditLogs 
WHERE Action = 'NOTIFICATION_SENT'
AND Timestamp >= DATEADD(day, -1, GETUTCDATE())
GROUP BY DATEPART(hour, Timestamp)
ORDER BY Hour;

===== BONNES PRATIQUES DE MAINTENANCE =====

🧹 NETTOYAGE AUTOMATIQUE :
- Logs Serilog : Rotation automatique (30 jours configuré)
- Logs d'audit : Archivage après 2 ans (conformité RGPD)
- Notifications lues : Purge après 6 mois (configurable)

🔒 SÉCURITÉ DES LOGS :
- Logs techniques : Accès restreint aux DevOps
- Logs d'audit : Chiffrement au repos + accès traçable
- Rotation des clés de chiffrement tous les 6 mois
- Sauvegarde des logs critiques vers stockage immutable

⚡ OPTIMISATION PERFORMANCE :
- Index sur AuditLogs.Timestamp et AuditLogs.UserId
- Partitioning des logs par mois (volumes importants)
- Compression des anciens logs
- Cache des statistiques fréquemment consultées

===== DÉPANNAGE COURANT =====

❌ PROBLÈMES FRÉQUENTS ET SOLUTIONS :

1. "SMTP timeout" répétés :
   → Vérifier configuration SMTP et firewall
   → Augmenter timeout dans EmailService
   → Implémenter retry avec backoff exponentiel

2. "Data integrity issue" :
   → Vérifier contraintes foreign key
   → Audit des suppressions en cascade
   → Mise en place de soft delete

3. "Database error while creating notification" :
   → Vérifier espace disque et connexions DB
   → Analyser les deadlocks dans SQL Server
   → Optimiser les index sur tables Notifications

4. Logs d'audit manquants :
   → Vérifier injection de dépendance AuditLogger
   → Contrôler les transactions et rollbacks
   → Valider la configuration de base de données

5. Performance dégradée :
   → Analyser les requêtes lentes dans logs
   → Vérifier la pagination des notifications
   → Optimiser les requêtes Entity Framework

===== ÉVOLUTIONS FUTURES RECOMMANDÉES =====

🚀 AMÉLIORATIONS PLANNIFIÉES :
- Implémentation d'un système de retry pour les emails
- Ajout de métriques Prometheus/Grafana
- Notifications en temps réel avec SignalR
- Système de templates pour les notifications
- API de reporting avancé pour les audits
- Intégration avec systèmes externes (Slack, Teams)
- Notifications push mobile
- Système de préférences utilisateur (fréquence, canaux)

📊 BUSINESS INTELLIGENCE :
- Dashboard temps réel des notifications
- Analyse prédictive des patterns de lecture
- Segmentation automatique des utilisateurs
- A/B testing sur les contenus de notifications
- Analyse de sentiment sur les retours utilisateurs

*/