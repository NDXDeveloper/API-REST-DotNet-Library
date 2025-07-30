using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims; // Utilisé pour manipuler les informations des utilisateurs (claims) dans les tokens d'authentification, comme l'identifiant de l'utilisateur (UserId).
using LibraryAPI.Data;
using LibraryAPI.Models;
using Microsoft.AspNetCore.RateLimiting;

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

        // ===== CONSTRUCTEUR =====
        
        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        /// <param name="context">Contexte de base de données</param>
        /// <param name="emailService">Service d'envoi d'emails</param>
        /// <param name="logger">✅ Service de logging pour aspects techniques</param>
        public NotificationController(ApplicationDbContext context, EmailService emailService, ILogger<NotificationController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;  // ✅ Ajout du service de logging technique
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
                        await _emailService.SendEmailAsync(user.Email, subject, content);
                        
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
===== LOGS TECHNIQUES AJOUTÉS DANS CE CONTRÔLEUR =====

✅ LOGS TECHNIQUES (Serilog) :
- Erreurs de base de données (DbUpdateException, connexion, transactions)
- Problèmes d'envoi d'emails (SMTP, TimeoutException, NetworkException)
- Erreurs de configuration EmailService (InvalidOperationException)
- Incohérences de données (utilisateurs/notifications null, références cassées)
- Problèmes de concurrence (DbUpdateConcurrencyException)
- Token JWT invalide/malformé
- Statistiques techniques d'envoi d'emails pour monitoring
- Erreurs de validation d'arguments

❌ LOGS D'AUDIT NON INCLUS :
- Qui crée quelles notifications
- Qui lit quelles notifications et quand
- Statistiques d'utilisation des notifications
- Historique des envois d'emails
- Analytics métier

===== EXEMPLES DE LOGS TECHNIQUES GÉNÉRÉS =====

[15:30:16 WRN] ⚠️ No users found in database when creating notification - potential data issue
[15:32:45 ERR] ❌ Database error while creating notification
[15:35:20 WRN] ⚠️ UserNotification references non-existent user abc123 - data integrity issue
[15:40:10 ERR] ❌ SMTP timeout while sending email to user def456
[15:42:30 INF] 📊 Email sending completed - Sent: 45, Skipped: 3, Failed: 2
[15:45:15 ERR] 🚨 UserNotification exists but Notification is null - data integrity error
[15:50:20 ERR] ❌ Concurrency error while marking notification as read - NotificationId: 789

CES LOGS AIDENT À :
✅ Détecter les problèmes de configuration SMTP
✅ Identifier les incohérences de données
✅ Surveiller les performances d'envoi d'emails
✅ Diagnostiquer les erreurs de base de données
✅ Monitorer l'intégrité des relations EF
✅ Détecter les problèmes de concurrence

AMÉLIORATIONS APPORTÉES :
✅ Gestion d'erreurs granulaire pour chaque type d'exception
✅ Validation des données nulles et références cassées
✅ Statistiques d'envoi d'emails pour monitoring
✅ Gestion des timeouts SMTP et erreurs réseau
✅ Détection des problèmes d'intégrité de données
✅ Meilleure utilisation d'async/await pour les performances

*/