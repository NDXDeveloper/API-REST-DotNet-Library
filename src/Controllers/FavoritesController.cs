using Microsoft.AspNetCore.Authorization; // Nécessaire pour gérer l'authentification et l'autorisation des utilisateurs.
using Microsoft.AspNetCore.Mvc; // Fournit les outils pour créer des API RESTful, comme les contrôleurs et les actions HTTP (GET, POST, etc.).
using Microsoft.EntityFrameworkCore; // Permet d'utiliser Entity Framework Core pour interagir avec la base de données via le contexte de données (ApplicationDbContext).
using System.Security.Claims; // Utilisé pour manipuler les informations des utilisateurs (claims) dans les tokens d'authentification, comme l'identifiant de l'utilisateur (UserId).
using LibraryAPI.Data; // Référence au contexte de base de données de l'application
using LibraryAPI.Models; // Référence aux modèles de données (entités) de l'application
using Microsoft.AspNetCore.RateLimiting; // Permet d'implémenter la limitation du taux de requêtes pour éviter les abus

namespace LibraryAPI.Controllers
{
    /// <summary>
    /// CONTRÔLEUR DE GESTION DES FAVORIS
    /// 
    /// Ce contrôleur gère toutes les opérations liées aux favoris des utilisateurs :
    /// - Ajout d'un livre/magazine aux favoris
    /// - Récupération de la liste des favoris de l'utilisateur
    /// - Suppression d'un livre/magazine des favoris
    /// 
    /// LOGS SERILOG (TECHNIQUES UNIQUEMENT) :
    /// - Erreurs de base de données (connexion, requêtes)
    /// - Incohérences de données (données nulles, relations cassées)
    /// - Problèmes de performance (requêtes lentes)
    /// - Erreurs d'accès aux données
    /// 
    /// NOTE : Les logs d'audit utilisateur (qui ajoute quoi aux favoris) 
    /// sont gérés par un système séparé
    /// </summary>
    [EnableRateLimiting("GlobalPolicy")]  // Limitation du taux de requêtes pour éviter les abus
    [ApiController]                       // Indique que c'est un contrôleur API avec validation automatique
    [Route("api/[controller]")]           // Route de base : /api/Favorites
    [Authorize]                          // Toutes les actions nécessitent une authentification
    public class FavoritesController : ControllerBase
    {
        // ===== SERVICES INJECTÉS =====
        
        /// <summary>
        /// Contexte de base de données pour interagir avec les tables de favoris
        /// </summary>
        private readonly ApplicationDbContext _context;
        
        /// <summary>
        /// ✅ SERVICE DE LOGGING SERILOG - LOGS TECHNIQUES SEULEMENT
        /// Utilisé pour :
        /// - Erreurs techniques (exceptions, problèmes de BDD)
        /// - Incohérences de données (relations cassées, données nulles)
        /// - Problèmes de performance (requêtes lentes)
        /// - Erreurs de configuration
        /// 
        /// PAS utilisé pour :
        /// - Audit utilisateur (qui ajoute/supprime quoi des favoris)
        /// - Traçabilité métier
        /// - Statistiques d'utilisation
        /// </summary>
        private readonly ILogger<FavoritesController> _logger;
        
        /// <summary>
        /// Service d'audit dédié pour tracer les actions métier des utilisateurs
        /// Utilisé pour enregistrer qui fait quoi dans les favoris (ajout/suppression)
        /// </summary>
        private readonly AuditLogger _auditLogger;

        // ===== CONSTRUCTEUR =====

        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        /// <param name="context">Contexte de base de données</param>
        /// <param name="logger">✅ Service de logging pour aspects techniques</param>
        /// <param name="auditLogger">Service d'audit pour tracer les actions utilisateur</param>
        public FavoritesController(ApplicationDbContext context, ILogger<FavoritesController> logger, AuditLogger auditLogger)
        {
            _context = context; // Initialisation du contexte de base de données
            _logger = logger; // ✅ Ajout du service de logging technique
            _auditLogger = auditLogger; // Initialisation du service d'audit métier
        }

        // ===== MÉTHODES DE GESTION DES FAVORIS =====

        /// <summary>
        /// AJOUTER UN LIVRE/MAGAZINE AUX FAVORIS
        /// 
        /// Permet à un utilisateur authentifié d'ajouter un livre ou magazine à ses favoris
        /// Logs techniques : erreurs de base de données, problèmes de données
        /// </summary>
        /// <param name="bookMagazineId">ID du livre/magazine à ajouter aux favoris</param>
        /// <returns>Message de succès ou erreur</returns>
        [HttpPost("add-favorite/{bookMagazineId}")]
        public async Task<IActionResult> AddFavorite(int bookMagazineId)
        {
            try
            {
                // Récupérer l'identifiant de l'utilisateur connecté via les Claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Si l'utilisateur n'est pas authentifié, retourner un statut 401
                if (userId == null)
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide ou malformé (problème système)
                    _logger.LogWarning("⚠️ AddFavorite called with invalid or missing user token");
                    return Unauthorized();
                }

                // Rechercher dans la base de données si le livre/magazine existe
                var bookMagazine = await _context.BooksMagazines.FindAsync(bookMagazineId);

                // Si le livre/magazine n'est pas trouvé, retourner une réponse 404
                if (bookMagazine == null)
                {
                    return NotFound(new { message = $"Book or magazine with ID {bookMagazineId} not found." });
                }

                // Vérifier si ce favori existe déjà pour cet utilisateur
                var existingFavorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.BookMagazineId == bookMagazineId);

                // Si le favori existe déjà, retourner un code 409 (Conflict)
                if (existingFavorite != null)
                {
                    return Conflict(new { message = "This item is already in your favorites." });
                }

                // Ajouter un nouveau favori pour l'utilisateur
                var userFavorite = new UserFavorite
                {
                    UserId = userId,
                    BookMagazineId = bookMagazineId
                };

                // Sauvegarder le nouveau favori dans la base de données
                await _context.UserFavorites.AddAsync(userFavorite);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.FAVORITE_ADDED,
                    $"Livre ajouté aux favoris: ID {bookMagazineId}");


                return Ok(new { message = "Book or magazine successfully added to favorites." });
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données lors de l'ajout
                _logger.LogError(ex, "❌ Database error while adding favorite - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "Database error occurred while adding to favorites");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec la requête LINQ/EF
                _logger.LogError(ex, "❌ Invalid operation while adding favorite - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "Data access error occurred");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique non prévue
                _logger.LogError(ex, "❌ Unexpected error while adding favorite - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "An internal error occurred while adding to favorites");
            }
        }

        /// <summary>
        /// RÉCUPÉRER LA LISTE DES FAVORIS DE L'UTILISATEUR
        /// 
        /// Retourne tous les favoris de l'utilisateur connecté avec les détails des livres/magazines
        /// Logs techniques : erreurs de requêtes, données nulles, problèmes de jointures
        /// </summary>
        /// <returns>Liste des favoris avec détails ou message d'erreur</returns>
        [HttpGet("my-favorites")]
        public async Task<IActionResult> GetMyFavorites()
        {
            try
            {
                // Récupérer l'identifiant de l'utilisateur connecté via les Claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Si l'utilisateur n'est pas authentifié, retourner un statut 401
                if (userId == null)
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide (problème système)
                    _logger.LogWarning("⚠️ GetMyFavorites called with invalid or missing user token");
                    return Unauthorized();
                }

                // Récupérer les favoris de l'utilisateur avec les détails des livres/magazines
                var favorites = await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.BookMagazine)           // Inclure les informations du livre/magazine
                        .ThenInclude(bm => bm!.Author)      // Inclure les informations de l'auteur
                    .ToListAsync();

                // Si aucun favori n'est trouvé, retourner une réponse 404
                if (favorites == null || !favorites.Any())
                {
                    return NotFound(new { message = "No favorites found for the user." });
                }

                // Créer une réponse personnalisée avec gestion des valeurs nulles
                var response = favorites
                    .Where(f => f.BookMagazine != null)     // Filtrer les livres/magazines nulls
                    .Select(f => new
                    {
                        BookMagazineId = f.BookMagazineId,
                        Title = f.BookMagazine?.Title ?? "Unknown Title",
                        Author = f.BookMagazine?.Author?.Name ?? "Unknown Author",
                        Description = f.BookMagazine?.Description ?? "No Description Available",
                        CoverImagePath = f.BookMagazine?.CoverImagePath ?? "No Cover Image Available",
                        UploadDate = f.BookMagazine?.UploadDate ?? DateTime.MinValue
                    })
                    .ToList();

                // ✅ LOG TECHNIQUE : Détection de données incohérentes
                var nullBookMagazines = favorites.Count(f => f.BookMagazine == null);
                if (nullBookMagazines > 0)
                {
                    _logger.LogWarning("⚠️ Found {NullCount} favorites with null BookMagazine references for user {UserId} - data integrity issue", 
                                      nullBookMagazines, userId);
                }

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans la requête LINQ complexe avec Include
                _logger.LogError(ex, "❌ Invalid operation during favorites retrieval - possible navigation property issue");
                return StatusCode(500, "Data retrieval error occurred");
            }
            catch (ArgumentNullException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec les paramètres null dans les requêtes
                _logger.LogError(ex, "❌ Null argument error during favorites retrieval");
                return StatusCode(500, "Data access configuration error");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de connexion/transaction avec la base de données
                _logger.LogError(ex, "❌ Database connection error during favorites retrieval");
                return StatusCode(500, "Database connectivity issue");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during favorites retrieval");
                return StatusCode(500, "An internal error occurred while retrieving favorites");
            }
        }

        /// <summary>
        /// SUPPRIMER UN LIVRE/MAGAZINE DES FAVORIS
        /// 
        /// Permet à un utilisateur de retirer un livre/magazine de ses favoris
        /// Logs techniques : erreurs de suppression, problèmes de transactions
        /// </summary>
        /// <param name="bookMagazineId">ID du livre/magazine à supprimer des favoris</param>
        /// <returns>Message de succès ou erreur</returns>
        [HttpDelete("remove-favorite/{bookMagazineId}")]
        public async Task<IActionResult> RemoveFavorite(int bookMagazineId)
        {
            try
            {
                // Récupérer l'identifiant de l'utilisateur connecté via les Claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Si l'utilisateur n'est pas authentifié, retourner un statut 401
                if (userId == null)
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide (problème système)
                    _logger.LogWarning("⚠️ RemoveFavorite called with invalid or missing user token");
                    return Unauthorized();
                }

                // Rechercher le favori correspondant à cet utilisateur et ce livre/magazine
                var favorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.BookMagazineId == bookMagazineId);

                // Si le favori n'est pas trouvé, retourner une réponse 404
                if (favorite == null)
                {
                    return NotFound(new { message = "The specified book or magazine is not in your favorites." });
                }

                // Supprimer le favori de la base de données
                _context.UserFavorites.Remove(favorite);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.FAVORITE_REMOVED,
                        $"Livre retiré des favoris: ID {bookMagazineId}");

                return Ok(new { message = "Book/Magazine removed from favorites successfully!" });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de concurrence (favori supprimé par ailleurs)
                _logger.LogError(ex, "❌ Concurrency error while removing favorite - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(409, "The favorite was already removed or modified by another operation");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données lors de la suppression
                _logger.LogError(ex, "❌ Database error while removing favorite - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "Database error occurred while removing from favorites");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec la requête ou l'état de l'entité
                _logger.LogError(ex, "❌ Invalid operation while removing favorite - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "Data access error occurred while removing favorite");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error while removing favorite - BookMagazineId: {BookMagazineId}", 
                                bookMagazineId);
                return StatusCode(500, "An internal error occurred while removing from favorites");
            }
        }
    }
}

/*
===== DOCUMENTATION DES LOGS IMPLÉMENTÉS DANS CE CONTRÔLEUR =====

✅ LOGS TECHNIQUES (Serilog - _logger) - Aspects système et infrastructure :
- Tokens JWT invalides/malformés (problèmes d'authentification système)
- Erreurs de base de données (DbUpdateException, problèmes de connexion, transactions)
- Problèmes de requêtes LINQ/EF (InvalidOperationException, propriétés de navigation)
- Incohérences de données (favoris avec BookMagazine null - intégrité référentielle)
- Erreurs de concurrence (DbUpdateConcurrencyException lors des suppressions)
- Erreurs de configuration (ArgumentNullException dans les paramètres)
- Exceptions non prévues (gestion générique des erreurs inattendues)

✅ LOGS D'AUDIT MÉTIER (AuditLogger - _auditLogger) - Actions utilisateur :
- Ajout de livre aux favoris : "Livre ajouté aux favoris: ID {bookMagazineId}"
- Suppression de livre des favoris : "Livre retiré des favoris: ID {bookMagazineId}"
- Traçabilité des actions métier pour compliance et historique

===== EXEMPLES DE LOGS GÉNÉRÉS =====

📊 LOGS D'AUDIT (AuditLogger) :
[2025-08-01 15:30:16] [AUDIT] [FAVORITE_ADDED] User: user123 - Livre ajouté aux favoris: ID 456
[2025-08-01 15:35:42] [AUDIT] [FAVORITE_REMOVED] User: user123 - Livre retiré des favoris: ID 456

🔧 LOGS TECHNIQUES (Serilog) :
[15:30:16 WRN] ⚠️ AddFavorite called with invalid or missing user token
[15:32:45 ERR] ❌ Database error while adding favorite - BookMagazineId: 123
[15:35:20 WRN] ⚠️ Found 3 favorites with null BookMagazine references for user abc123 - data integrity issue
[15:40:10 ERR] ❌ Concurrency error while removing favorite - BookMagazineId: 456
[15:45:30 ERR] ❌ Invalid operation during favorites retrieval - possible navigation property issue

===== UTILITÉ DES LOGS =====
📊 LOGS D'AUDIT : Compliance, historique métier, traçabilité des actions utilisateur
🔧 LOGS TECHNIQUES : Détection problèmes système, monitoring santé application, diagnostic erreurs

===== DOUBLE SYSTÈME DE LOGGING =====
🔧 Serilog Logger (_logger) : Problèmes techniques et infrastructure uniquement
📊 Audit Logger (_auditLogger) : Actions métier et traçabilité utilisateur
*/