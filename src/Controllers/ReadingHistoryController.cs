using Microsoft.AspNetCore.Authorization; // Nécessaire pour gérer l'authentification et l'autorisation des utilisateurs dans l'application via des attributs comme [Authorize].
using Microsoft.AspNetCore.Mvc; // Fournit les outils essentiels pour créer des contrôleurs API, gérer les routes HTTP et les actions telles que GET, POST, PUT, DELETE.
using Microsoft.EntityFrameworkCore; // Permet l'utilisation d'Entity Framework Core pour interagir avec la base de données et effectuer des opérations CRUD.
//using System.IdentityModel.Tokens.Jwt; // Commenté car non utilisé. Ce namespace est utile pour manipuler les JWT (JSON Web Tokens) directement si nécessaire.
using System.Security.Claims; // Utilisé pour extraire des informations de l'utilisateur connecté (via les claims, comme l'identifiant d'utilisateur) à partir de son token d'authentification.
using LibraryAPI.Data;
using LibraryAPI.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryAPI.Controllers
{
    /// <summary>
    /// CONTRÔLEUR DE GESTION DE L'HISTORIQUE DE LECTURE
    /// 
    /// Ce contrôleur gère toutes les opérations liées à l'historique de lecture des utilisateurs :
    /// - Mise à jour de l'historique de lecture (ajout/modification de dernière lecture)
    /// - Récupération de l'historique de lecture de l'utilisateur connecté
    /// 
    /// LOGS SERILOG (TECHNIQUES UNIQUEMENT) :
    /// - Erreurs de base de données (connexion, transactions, requêtes)
    /// - Incohérences de données (livres/magazines null, relations cassées)
    /// - Problèmes de performance (requêtes lentes, jointures complexes)
    /// - Erreurs d'accès aux données et validations
    /// - Problèmes de concurrence lors des mises à jour
    /// 
    /// NOTE : Les logs d'audit (qui lit quoi, quand, statistiques de lecture)
    /// sont gérés par un système séparé
    /// </summary>
    [EnableRateLimiting("GlobalPolicy")]  // Limitation du taux de requêtes pour éviter les abus
    [ApiController]                       // Contrôleur API avec validation automatique
    [Route("api/[controller]")]           // Route de base : /api/ReadingHistory
    [Authorize]                          // Nécessite que l'utilisateur soit authentifié pour toutes les actions
    public class ReadingHistoryController : ControllerBase
    {
        // ===== SERVICES INJECTÉS =====
        
        /// <summary>
        /// Contexte de base de données pour les opérations sur l'historique de lecture
        /// </summary>
        private readonly ApplicationDbContext _context;
        
        /// <summary>
        /// ✅ SERVICE DE LOGGING SERILOG - LOGS TECHNIQUES SEULEMENT
        /// Utilisé pour :
        /// - Erreurs techniques (exceptions, problèmes de BDD)
        /// - Incohérences de données (livres null, relations cassées)
        /// - Problèmes de performance (requêtes lentes)
        /// - Erreurs de concurrence et transactions
        /// - Problèmes de configuration
        /// 
        /// PAS utilisé pour :
        /// - Audit des lectures (qui lit quoi, quand)
        /// - Statistiques d'utilisation
        /// - Analytics métier
        /// - Traçabilité utilisateur
        /// </summary>
        private readonly ILogger<ReadingHistoryController> _logger;
        
        private readonly AuditLogger _auditLogger;

        // ===== CONSTRUCTEUR =====

        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        /// <param name="context">Contexte de base de données</param>
        /// <param name="logger">✅ Service de logging pour aspects techniques</param>
        public ReadingHistoryController(ApplicationDbContext context, ILogger<ReadingHistoryController> logger, AuditLogger auditLogger)
        {
            _context = context;
            _logger = logger;  // ✅ Ajout du service de logging technique
            _auditLogger = auditLogger;
        }

        // ===== MÉTHODES DE GESTION DE L'HISTORIQUE =====

        /// <summary>
        /// METTRE À JOUR L'HISTORIQUE DE LECTURE
        /// 
        /// Ajoute ou met à jour l'entrée d'historique de lecture pour un utilisateur et un livre/magazine
        /// Logs techniques : erreurs de base de données, problèmes de concurrence
        /// </summary>
        /// <param name="bookMagazineId">ID du livre/magazine lu</param>
        /// <returns>Message de succès ou erreur</returns>
        [HttpPost("update-history/{bookMagazineId}")]
        public async Task<IActionResult> UpdateReadingHistory(int bookMagazineId)
        {
            try
            {
                // Récupérer l'identifiant de l'utilisateur à partir des Claims dans le token d'authentification
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Si l'utilisateur n'est pas authentifié, retourner un statut 401
                if (userId == null)
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide ou malformé (problème système)
                    _logger.LogWarning("⚠️ UpdateReadingHistory called with invalid or missing user token");
                    return Unauthorized();
                }

                // Vérifier si le livre ou magazine existe dans la base de données
                var bookMagazine = await _context.BooksMagazines.FindAsync(bookMagazineId);
                if (bookMagazine == null)
                {
                    return NotFound(new { message = $"Book or magazine with ID {bookMagazineId} not found." });
                }

                // Vérifier si cet utilisateur a déjà une entrée d'historique pour ce livre/magazine
                var readingHistory = await _context.UserReadingHistory
                    .FirstOrDefaultAsync(rh => rh.UserId == userId && rh.BookMagazineId == bookMagazineId);

                if (readingHistory == null)
                {
                    // Si aucune entrée n'existe, créer une nouvelle entrée
                    readingHistory = new UserReadingHistory
                    {
                        UserId = userId,
                        BookMagazineId = bookMagazineId,
                        LastReadDate = DateTime.UtcNow
                    };
                    _context.UserReadingHistory.Add(readingHistory);
                }
                else
                {
                    // Si une entrée existe déjà, mettre à jour la date de dernière lecture
                    readingHistory.LastReadDate = DateTime.UtcNow;
                    _context.UserReadingHistory.Update(readingHistory);
                }

                // Sauvegarder les modifications dans la base de données
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.BOOK_VIEWED,
                        $"Historique de lecture mis à jour pour le livre ID {bookMagazineId}");

                return Ok(new { message = "Reading history updated successfully!" });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de concurrence (historique modifié par ailleurs)
                _logger.LogError(ex, "❌ Concurrency error while updating reading history - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(409, "The reading history was modified by another operation");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données lors de la mise à jour
                _logger.LogError(ex, "❌ Database error while updating reading history - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "Database error occurred while updating reading history");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec la requête LINQ/EF
                _logger.LogError(ex, "❌ Invalid operation while updating reading history - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "Data access error occurred while updating reading history");
            }
            catch (ArgumentException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec les arguments/entités
                _logger.LogError(ex, "❌ Argument error while updating reading history - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "Data validation error occurred");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error while updating reading history - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "An internal error occurred while updating reading history");
            }
        }

        /// <summary>
        /// RÉCUPÉRER L'HISTORIQUE DE LECTURE DE L'UTILISATEUR
        /// 
        /// Retourne l'historique complet de lecture de l'utilisateur connecté
        /// Logs techniques : erreurs de requêtes complexes, données nulles, problèmes de jointures
        /// </summary>
        /// <returns>Liste de l'historique de lecture ou message d'erreur</returns>
        [HttpGet("reading-history")]
        public async Task<IActionResult> GetReadingHistory()
        {
            try
            {
                // Récupérer l'identifiant de l'utilisateur à partir des Claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Si l'utilisateur n'est pas authentifié, retourner un statut 401
                if (userId == null)
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide ou malformé (problème système)
                    _logger.LogWarning("⚠️ GetReadingHistory called with invalid or missing user token");
                    return Unauthorized();
                }

                // Récupérer l'historique de lecture avec jointures complexes
                var history = await _context.UserReadingHistory
                    .Where(rh => rh.UserId == userId)
                    .Include(rh => rh.BookMagazine)           // Inclure les informations du livre/magazine
                        .ThenInclude(b => b!.Author)          // Inclure l'auteur du livre/magazine
                    .OrderByDescending(rh => rh.LastReadDate) // Trier par date de lecture (plus récent en premier)
                    .ToListAsync();

                // Si l'utilisateur n'a aucun historique de lecture, retourner 404
                if (history == null || !history.Any())
                {
                    return NotFound(new { message = "No reading history found for the user." });
                }

                // Créer une réponse personnalisée avec gestion des valeurs nulles
                var response = history
                    .Where(rh => rh.BookMagazine != null)     // Filtrer les livres/magazines nulls
                    .Select(rh => new
                    {
                        BookMagazineId = rh.BookMagazineId,
                        Title = rh.BookMagazine?.Title ?? "Unknown Title",
                        Author = rh.BookMagazine?.Author?.Name ?? "Unknown Author",
                        Description = rh.BookMagazine?.Description ?? "No Description Available",
                        CoverImagePath = rh.BookMagazine?.CoverImagePath ?? "No Cover Image Available",
                        LastReadDate = rh.LastReadDate
                    })
                    .ToList();

                // ✅ LOG TECHNIQUE : Détection de données incohérentes
                var nullBookMagazines = history.Count(rh => rh.BookMagazine == null);
                if (nullBookMagazines > 0)
                {
                    _logger.LogWarning("⚠️ Found {NullCount} reading history entries with null BookMagazine references for user {UserId} - data integrity issue", 
                                      nullBookMagazines, userId);
                }

                // ✅ LOG TECHNIQUE : Détection de problèmes de performance (trop d'entrées)
                if (history.Count > 1000)
                {
                    _logger.LogWarning("⚠️ User {UserId} has {HistoryCount} reading history entries - potential performance impact", 
                                      userId, history.Count);
                }

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans la requête LINQ complexe avec Include/ThenInclude
                _logger.LogError(ex, "❌ Invalid operation during reading history retrieval - possible navigation property issue");
                return StatusCode(500, "Data retrieval error occurred while getting reading history");
            }
            catch (ArgumentNullException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec les paramètres null dans les requêtes
                _logger.LogError(ex, "❌ Null argument error during reading history retrieval");
                return StatusCode(500, "Data access configuration error");
            }
            catch (TimeoutException ex)
            {
                // ✅ LOG TECHNIQUE : Timeout de requête (requête trop lente)
                _logger.LogError(ex, "❌ Database timeout during reading history retrieval - possible performance issue");
                return StatusCode(500, "Database timeout - the query took too long to execute");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de connexion/transaction avec la base de données
                _logger.LogError(ex, "❌ Database connection error during reading history retrieval");
                return StatusCode(500, "Database connectivity issue while retrieving reading history");
            }
            catch (OutOfMemoryException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de mémoire (trop de données chargées)
                _logger.LogError(ex, "❌ Out of memory error during reading history retrieval - dataset too large");
                return StatusCode(500, "Server memory issue - reading history dataset too large");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during reading history retrieval");
                return StatusCode(500, "An internal error occurred while retrieving reading history");
            }
        }
    }
}

/*
===== LOGS TECHNIQUES AJOUTÉS DANS CE CONTRÔLEUR =====

✅ LOGS TECHNIQUES (Serilog) :
- Token JWT invalide/malformé (problème d'authentification système)
- Erreurs de base de données (DbUpdateException, timeouts, connexion)
- Problèmes de concurrence (DbUpdateConcurrencyException)
- Erreurs de requêtes LINQ/EF (InvalidOperationException, navigation properties)
- Incohérences de données (historique avec BookMagazine null)
- Problèmes de performance (trop d'entrées, timeouts, mémoire)
- Erreurs de configuration (ArgumentNullException)
- Problèmes de mémoire (OutOfMemoryException)

❌ LOGS D'AUDIT NON INCLUS :
- Qui lit quoi et quand
- Statistiques de lecture utilisateur
- Historique des habitudes de lecture
- Analytics métier sur les lectures
- Préférences de lecture

===== EXEMPLES DE LOGS TECHNIQUES GÉNÉRÉS =====

[15:30:16 WRN] ⚠️ UpdateReadingHistory called with invalid or missing user token
[15:32:45 ERR] ❌ Database error while updating reading history - BookMagazineId: 123
[15:35:20 WRN] ⚠️ Found 5 reading history entries with null BookMagazine references for user abc123 - data integrity issue
[15:40:10 WRN] ⚠️ User def456 has 1250 reading history entries - potential performance impact
[15:42:30 ERR] ❌ Database timeout during reading history retrieval - possible performance issue
[15:45:15 ERR] ❌ Out of memory error during reading history retrieval - dataset too large
[15:50:20 ERR] ❌ Concurrency error while updating reading history - BookMagazineId: 789

CES LOGS AIDENT À :
✅ Détecter les problèmes de performance avec de gros historiques
✅ Identifier les incohérences de données
✅ Surveiller les timeouts de requêtes
✅ Diagnostiquer les problèmes de mémoire
✅ Détecter les erreurs de concurrence
✅ Monitorer l'intégrité des relations EF

AMÉLIORATIONS TECHNIQUES :
✅ Gestion spécifique des timeouts de base de données
✅ Détection des problèmes de performance (trop d'entrées)
✅ Surveillance de l'intégrité des données
✅ Gestion des erreurs de mémoire
✅ Monitoring des requêtes complexes avec jointures
✅ Validation des tokens JWT

NOTES IMPORTANTES :
- Le contrôleur surveille les performances (>1000 entrées d'historique)
- Détection automatique des incohérences de données
- Gestion robuste des requêtes complexes avec Include/ThenInclude
- Protection contre les problèmes de mémoire sur de gros datasets
*/