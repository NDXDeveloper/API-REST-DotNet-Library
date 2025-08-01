using Microsoft.AspNetCore.Authorization;   // Nécessaire pour gérer l'authentification et l'autorisation des utilisateurs dans l'application via des attributs comme [Authorize].
using Microsoft.AspNetCore.Mvc;             // Fournit les outils essentiels pour créer des contrôleurs API, gérer les routes HTTP et les actions telles que GET, POST, PUT, DELETE.
using Microsoft.EntityFrameworkCore;        // Permet l'utilisation d'Entity Framework Core pour interagir avec la base de données et effectuer des opérations CRUD.
//using System.IdentityModel.Tokens.Jwt;    // Commenté car non utilisé. Ce namespace est utile pour manipuler les JWT (JSON Web Tokens) directement si nécessaire.
using System.Security.Claims;               // Utilisé pour extraire des informations de l'utilisateur connecté (via les claims, comme l'identifiant d'utilisateur) à partir de son token d'authentification.
using LibraryAPI.Data;                      // Namespace contenant le contexte de base de données ApplicationDbContext pour l'accès aux données
using LibraryAPI.Models;                    // Namespace contenant les modèles de données (entités) comme ReadingHistory, Book, Magazine, etc.
using Microsoft.AspNetCore.RateLimiting;    // Permet l'utilisation de la limitation du taux de requêtes pour prévenir les abus et surcharges


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
    [EnableRateLimiting("GlobalPolicy")]    // Limitation du taux de requêtes pour éviter les abus
    [ApiController]                         // Contrôleur API avec validation automatique
    [Route("api/[controller]")]             // Route de base : /api/ReadingHistory
    [Authorize]                             // Nécessite que l'utilisateur soit authentifié pour toutes les actions
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
        
        /// <summary>
        /// Service d'audit spécialisé pour la traçabilité métier
        /// Utilisé pour :
        /// - Enregistrer les activités de lecture des utilisateurs
        /// - Tracer les accès aux ressources (livres, magazines)
        /// - Générer des statistiques de consultation
        /// - Audit de conformité et historique des actions utilisateur
        /// </summary>
        private readonly AuditLogger _auditLogger;

        // ===== CONSTRUCTEUR =====

        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        /// <param name="context">Contexte de base de données</param>
        /// <param name="logger">✅ Service de logging pour aspects techniques</param>
        /// <param name="auditLogger">Service d'audit pour la traçabilité métier</param>
        public ReadingHistoryController(ApplicationDbContext context, ILogger<ReadingHistoryController> logger, AuditLogger auditLogger)
        {
            _context = context; // Initialisation du contexte de base de données
            _logger = logger; // ✅ Ajout du service de logging technique
            _auditLogger = auditLogger; // Initialisation du service d'audit métier
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
===============================================================================
DOCUMENTATION TECHNIQUE - ReadingHistoryController
===============================================================================

Vue d'ensemble du contrôleur
-----------------------------
Le ReadingHistoryController gère l'historique de lecture des utilisateurs avec 
une approche de logging dual :
- Logs techniques (Serilog) : Surveillance système et performance
- Logs d'audit (AuditLogger) : Traçabilité métier et conformité

===============================================================================
LOGS TECHNIQUES IMPLÉMENTÉS (SERILOG)
===============================================================================

🔐 AUTHENTIFICATION ET SÉCURITÉ
-------------------------------
_logger.LogWarning("⚠️ UpdateReadingHistory called with invalid or missing user token");
_logger.LogWarning("⚠️ GetReadingHistory called with invalid or missing user token");

💾 ERREURS DE BASE DE DONNÉES
-----------------------------
// Concurrence
_logger.LogError(ex, "❌ Concurrency error while updating reading history - BookMagazineId: {BookMagazineId}", bookMagazineId);

// Erreurs de mise à jour
_logger.LogError(ex, "❌ Database error while updating reading history - BookMagazineId: {BookMagazineId}", bookMagazineId);

// Timeouts
_logger.LogError(ex, "❌ Database timeout during reading history retrieval - possible performance issue");

// Problèmes de connexion
_logger.LogError(ex, "❌ Database connection error during reading history retrieval");

🔍 INTÉGRITÉ DES DONNÉES
------------------------
// Détection d'incohérences
_logger.LogWarning("⚠️ Found {NullCount} reading history entries with null BookMagazine references for user {UserId} - data integrity issue", nullBookMagazines, userId);

// Erreurs de navigation EF
_logger.LogError(ex, "❌ Invalid operation during reading history retrieval - possible navigation property issue");

⚡ PERFORMANCE ET RESSOURCES
---------------------------
// Datasets volumineux
_logger.LogWarning("⚠️ User {UserId} has {HistoryCount} reading history entries - potential performance impact", userId, history.Count);

// Mémoire insuffisante
_logger.LogError(ex, "❌ Out of memory error during reading history retrieval - dataset too large");

🛠️ ERREURS DE CONFIGURATION
---------------------------
// Arguments invalides
_logger.LogError(ex, "❌ Argument error while updating reading history - BookMagazineId: {BookMagazineId}", bookMagazineId);

// Configuration manquante
_logger.LogError(ex, "❌ Null argument error during reading history retrieval");

===============================================================================
LOGS D'AUDIT IMPLÉMENTÉS (AUDITLOGGER)
===============================================================================

📚 TRAÇABILITÉ MÉTIER
--------------------
await _auditLogger.LogAsync(AuditActions.BOOK_VIEWED, 
    $"Historique de lecture mis à jour pour le livre ID {bookMagazineId}");

Utilisé pour :
- Enregistrer les activités de lecture des utilisateurs
- Tracer les accès aux ressources (livres, magazines)
- Générer des statistiques de consultation
- Audit de conformité et historique des actions utilisateur

===============================================================================
EXEMPLES DE LOGS GÉNÉRÉS
===============================================================================

LOGS TECHNIQUES (FORMAT SERILOG)
--------------------------------
[2025-08-01 15:30:16 WRN] ⚠️ UpdateReadingHistory called with invalid or missing user token
[2025-08-01 15:32:45 ERR] ❌ Database error while updating reading history - BookMagazineId: 123
[2025-08-01 15:35:20 WRN] ⚠️ Found 5 reading history entries with null BookMagazine references for user abc123 - data integrity issue
[2025-08-01 15:40:10 WRN] ⚠️ User def456 has 1250 reading history entries - potential performance impact
[2025-08-01 15:42:30 ERR] ❌ Database timeout during reading history retrieval - possible performance issue
[2025-08-01 15:45:15 ERR] ❌ Out of memory error during reading history retrieval - dataset too large
[2025-08-01 15:50:20 ERR] ❌ Concurrency error while updating reading history - BookMagazineId: 789
[2025-08-01 15:55:30 ERR] ❌ Invalid operation during reading history retrieval - possible navigation property issue

LOGS D'AUDIT (FORMAT PERSONNALISÉ)
----------------------------------
[2025-08-01 15:33:12] BOOK_VIEWED - User: user123 - Historique de lecture mis à jour pour le livre ID 456
[2025-08-01 15:44:25] BOOK_VIEWED - User: user789 - Historique de lecture mis à jour pour le livre ID 234

===============================================================================
SURVEILLANCE ET MONITORING
===============================================================================

🎯 INDICATEURS DE PERFORMANCE SURVEILLÉS
---------------------------------------
- Historiques volumineux : Alerte si > 1000 entrées par utilisateur
- Timeouts de requêtes : Surveillance des requêtes lentes
- Utilisation mémoire : Détection des datasets trop volumineux
- Erreurs de concurrence : Monitoring des conflits de mise à jour

🔧 PROBLÈMES TECHNIQUES DÉTECTÉS
-------------------------------
- Tokens JWT invalides : Problèmes d'authentification système
- Relations EF cassées : Incohérences dans les données liées
- Requêtes LINQ complexes : Erreurs dans les jointures Include/ThenInclude
- Problèmes de connectivité : Erreurs de connexion à la base de données

===============================================================================
ARCHITECTURE DE LOGGING
===============================================================================

📊 SÉPARATION DES RESPONSABILITÉS
--------------------------------
Type de log     | Service              | Usage                                    | Exemples
----------------|---------------------|------------------------------------------|---------------------------
Technique       | ILogger<T> (Serilog)| Surveillance système, débogage, perf    | Erreurs DB, timeouts, concurrence
Audit           | AuditLogger         | Traçabilité métier, conformité, stats   | Actions utilisateur, accès ressources

🎚️ NIVEAUX DE LOGGING
--------------------
- Error : Erreurs critiques nécessitant une intervention
- Warning : Situations anormales mais non bloquantes
- Information : Actions métier importantes (audit uniquement)

===============================================================================
AMÉLIORATIONS TECHNIQUES IMPLÉMENTÉES
===============================================================================

✅ ROBUSTESSE
- Gestion spécifique de 7 types d'exceptions différentes
- Validation automatique de l'intégrité des données
- Protection contre les problèmes de mémoire
- Surveillance proactive des performances

✅ OBSERVABILITÉ
- Logs structurés avec paramètres typés
- Emojis pour classification visuelle rapide
- Métadonnées contextuelles (IDs, compteurs)
- Corrélation entre logs techniques et d'audit

✅ MAINTENABILITÉ
- Documentation inline exhaustive
- Séparation claire des responsabilités
- Gestion d'erreurs granulaire
- Code auto-documenté par les logs

===============================================================================
MÉTRIQUES ET ALERTES RECOMMANDÉES
===============================================================================

🚨 ALERTES CRITIQUES
-------------------
- Taux d'erreur > 5% sur 5 minutes
- Timeouts de base de données > 3 par minute
- Utilisateurs avec > 2000 entrées d'historique
- Erreurs de mémoire

📈 MÉTRIQUES À SURVEILLER
------------------------
- Temps de réponse moyen par endpoint
- Nombre d'entrées nulles détectées
- Fréquence des erreurs de concurrence
- Volume de logs d'audit générés

🔍 TABLEAUX DE BORD SUGGÉRÉS
---------------------------
- Performance : Temps de réponse, timeouts, mémoire
- Qualité des données : Relations nulles, incohérences
- Sécurité : Tokens invalides, tentatives non autorisées
- Usage métier : Activité de lecture, statistiques d'accès

===============================================================================
NOTES IMPORTANTES
===============================================================================

⚠️ SEUILS DE PERFORMANCE : Le contrôleur surveille automatiquement les 
   utilisateurs avec > 1000 entrées d'historique

🔒 SÉCURITÉ : Tous les logs excluent les données sensibles 
   (mots de passe, tokens complets)

📊 CONFORMITÉ : Les logs d'audit respectent les exigences 
   de traçabilité métier

🚀 ÉVOLUTIVITÉ : Architecture préparée pour l'ajout de nouveaux 
   types de surveillance

===============================================================================
*/