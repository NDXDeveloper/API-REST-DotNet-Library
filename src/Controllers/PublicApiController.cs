using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using LibraryAPI.Data;
using LibraryAPI.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryAPI.Controllers
{
    /// <summary>
    /// CONTRÔLEUR API PUBLIQUE
    /// 
    /// Ce contrôleur fournit des endpoints publics (sans authentification) pour :
    /// - Récupération des livres/magazines les plus populaires
    /// - Statistiques générales de l'API (totaux)
    /// - Commentaires récents sur les livres/magazines
    /// 
    /// LOGS SERILOG (TECHNIQUES UNIQUEMENT) :
    /// - Erreurs de base de données (connexion, requêtes)
    /// - Problèmes de performance (requêtes lentes, gros datasets)
    /// - Incohérences de données (auteurs/livres null, relations cassées)
    /// - Erreurs de calculs d'agrégation (Sum, Count)
    /// - Timeouts sur les requêtes publiques
    /// - Problèmes de mémoire avec de gros volumes
    /// 
    /// NOTE : Les logs d'usage public (qui consulte quoi, quand)
    /// sont gérés par un système séparé (analytics web)
    /// </summary>
    [EnableRateLimiting("PublicPolicy")]  // Rate limiting plus permissif pour API publique
    [Route("api/public")]                 // Route publique : /api/public
    [ApiController]                       // Contrôleur API avec validation automatique
    public class PublicApiController : ControllerBase
    {
        // ===== SERVICES INJECTÉS =====
        
        /// <summary>
        /// Contexte de base de données pour les requêtes publiques
        /// </summary>
        private readonly ApplicationDbContext _context;
        
        /// <summary>
        /// ✅ SERVICE DE LOGGING SERILOG - LOGS TECHNIQUES SEULEMENT
        /// Utilisé pour :
        /// - Erreurs techniques (exceptions, problèmes de BDD)
        /// - Problèmes de performance (requêtes lentes, datasets volumineux)
        /// - Erreurs de calculs d'agrégation (Sum, Count, Average)
        /// - Incohérences de données (relations nulles)
        /// - Timeouts de requêtes publiques
        /// - Problèmes de configuration
        /// 
        /// PAS utilisé pour :
        /// - Analytics d'usage public (qui consulte quoi)
        /// - Statistiques de trafic web
        /// - Métriques métier d'utilisation
        /// - Géolocalisation des requêtes
        /// </summary>
        private readonly ILogger<PublicApiController> _logger;

        // ===== CONSTRUCTEUR =====
        
        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        /// <param name="context">Contexte de base de données</param>
        /// <param name="logger">✅ Service de logging pour aspects techniques</param>
        public PublicApiController(ApplicationDbContext context, ILogger<PublicApiController> logger)
        {
            _context = context;
            _logger = logger;  // ✅ Ajout du service de logging technique
        }

        // ===== ENDPOINTS PUBLICS =====

        /// <summary>
        /// OBTENIR LES LIVRES/MAGAZINES LES PLUS POPULAIRES
        /// 
        /// Endpoint public pour récupérer les contenus les plus consultés
        /// Logs techniques : erreurs de requêtes, problèmes de performance, données nulles
        /// </summary>
        /// <param name="count">Nombre d'éléments à retourner (par défaut: 10)</param>
        /// <returns>Liste des livres/magazines populaires ou erreur</returns>
        [HttpGet("top-books-magazines")]
        public async Task<IActionResult> GetTopBooksMagazines([FromQuery] int count = 10)
        {
            try
            {
                // Validation des paramètres d'entrée
                if (count <= 0 || count > 100)
                {
                    return BadRequest("Count must be between 1 and 100");
                }

                // ✅ LOG TECHNIQUE : Détection de requêtes potentiellement coûteuses
                if (count > 50)
                {
                    _logger.LogWarning("⚠️ Large dataset requested for top books/magazines: {Count} items - potential performance impact", count);
                }

                // Requête pour récupérer les livres/magazines les plus populaires
                var topBooksMagazines = await _context.BooksMagazines
                    .OrderByDescending(b => b.ViewCount)
                    .Take(count)
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        Author = b.Author != null ? b.Author.Name : "Unknown Author",  // Protection contre Author null
                        b.Description,
                        b.CoverImagePath,
                        b.ViewCount
                    })
                    .ToListAsync();

                // ✅ LOG TECHNIQUE : Détection de problèmes de données
                var itemsWithNullAuthor = await _context.BooksMagazines
                    .Where(b => b.Author == null)
                    .CountAsync();
                
                if (itemsWithNullAuthor > 0)
                {
                    _logger.LogWarning("⚠️ Found {NullAuthorCount} books/magazines with null Author - data integrity issue", itemsWithNullAuthor);
                }

                return Ok(topBooksMagazines);
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans la requête LINQ (OrderBy, Select, etc.)
                _logger.LogError(ex, "❌ Invalid operation during top books/magazines query - possible LINQ issue");
                return StatusCode(500, "Data query error occurred while retrieving popular content");
            }
            catch (TimeoutException ex)
            {
                // ✅ LOG TECHNIQUE : Timeout de requête (problème de performance)
                _logger.LogError(ex, "❌ Database timeout during top books/magazines query - performance issue");
                return StatusCode(500, "Database timeout - the query took too long to execute");
            }
            catch (ArgumentException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec les paramètres de requête
                _logger.LogError(ex, "❌ Argument error during top books/magazines query - Count: {Count}", count);
                return StatusCode(500, "Query parameter error occurred");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de connexion à la base de données
                _logger.LogError(ex, "❌ Database connection error during top books/magazines query");
                return StatusCode(500, "Database connectivity issue");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during top books/magazines query - Count: {Count}", count);
                return StatusCode(500, "An internal error occurred while retrieving popular content");
            }
        }

        /// <summary>
        /// OBTENIR LES STATISTIQUES GÉNÉRALES DE L'API
        /// 
        /// Endpoint public pour les statistiques globales (totaux, compteurs)
        /// Logs techniques : erreurs de calculs d'agrégation, problèmes de performance
        /// </summary>
        /// <returns>Statistiques générales ou erreur</returns>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                // Requêtes d'agrégation pour les statistiques
                var totalBooksMagazines = await _context.BooksMagazines.CountAsync();
                var totalDownloads = await _context.BooksMagazines.SumAsync(b => b.DownloadCount);
                var totalViews = await _context.BooksMagazines.SumAsync(b => b.ViewCount);

                // ✅ LOG TECHNIQUE : Détection de valeurs anormales dans les statistiques
                if (totalDownloads < 0 || totalViews < 0)
                {
                    _logger.LogWarning("⚠️ Negative values detected in statistics - TotalDownloads: {Downloads}, TotalViews: {Views} - data integrity issue", 
                                      totalDownloads, totalViews);
                }

                // ✅ LOG TECHNIQUE : Surveillance de la croissance des données
                if (totalBooksMagazines > 10000)
                {
                    _logger.LogInformation("📊 Large dataset detected - {TotalItems} books/magazines - consider performance optimization", totalBooksMagazines);
                }

                return Ok(new
                {
                    TotalBooksMagazines = totalBooksMagazines,
                    TotalDownloads = totalDownloads,
                    TotalViews = totalViews
                });
            }
            catch (OverflowException ex)
            {
                // ✅ LOG TECHNIQUE : Débordement lors des calculs Sum() (valeurs trop grandes)
                _logger.LogError(ex, "❌ Overflow error during statistics calculation - values too large for sum operations");
                return StatusCode(500, "Statistics calculation overflow - dataset too large");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les opérations d'agrégation
                _logger.LogError(ex, "❌ Invalid operation during statistics calculation - possible aggregation issue");
                return StatusCode(500, "Statistics calculation error occurred");
            }
            catch (TimeoutException ex)
            {
                // ✅ LOG TECHNIQUE : Timeout lors des calculs d'agrégation
                _logger.LogError(ex, "❌ Database timeout during statistics calculation - performance issue");
                return StatusCode(500, "Statistics calculation timeout - dataset too large");
            }
            catch (ArithmeticException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur arithmétique lors des Sum()
                _logger.LogError(ex, "❌ Arithmetic error during statistics calculation");
                return StatusCode(500, "Mathematical calculation error in statistics");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de connexion/transaction
                _logger.LogError(ex, "❌ Database connection error during statistics calculation");
                return StatusCode(500, "Database connectivity issue while calculating statistics");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during statistics calculation");
                return StatusCode(500, "An internal error occurred while calculating statistics");
            }
        }

        /// <summary>
        /// OBTENIR LES COMMENTAIRES RÉCENTS
        /// 
        /// Endpoint public pour les commentaires récents sur les livres/magazines
        /// Logs techniques : erreurs de jointures complexes, données nulles, performance
        /// </summary>
        /// <param name="count">Nombre de commentaires à retourner (par défaut: 10)</param>
        /// <returns>Liste des commentaires récents ou erreur</returns>
        [HttpGet("recent-comments")]
        public async Task<IActionResult> GetRecentComments([FromQuery] int count = 10)
        {
            try
            {
                // Validation des paramètres d'entrée
                if (count <= 0 || count > 50)
                {
                    return BadRequest("Count must be between 1 and 50");
                }

                // ✅ LOG TECHNIQUE : Surveillance des requêtes coûteuses
                if (count > 25)
                {
                    _logger.LogWarning("⚠️ Large dataset requested for recent comments: {Count} items - potential performance impact", count);
                }

                // Requête avec jointures complexes pour les commentaires récents
                var recentComments = await _context.Comments
                    .OrderByDescending(c => c.CommentDate)
                    .Take(count)
                    .Select(c => new
                    {
                        c.Content,
                        c.CommentDate,
                        User = c.User != null ? c.User.UserName : "Unknown User",           // Protection contre User null
                        BookMagazineTitle = c.BookMagazine != null ? c.BookMagazine.Title : "Unknown Title"  // Protection contre BookMagazine null
                    })
                    .ToListAsync();

                // ✅ LOG TECHNIQUE : Détection de problèmes d'intégrité relationnelle
                var commentsWithNullUser = await _context.Comments
                    .Where(c => c.User == null)
                    .CountAsync();
                
                var commentsWithNullBookMagazine = await _context.Comments
                    .Where(c => c.BookMagazine == null)
                    .CountAsync();

                if (commentsWithNullUser > 0)
                {
                    _logger.LogWarning("⚠️ Found {NullUserCount} comments with null User - data integrity issue", commentsWithNullUser);
                }

                if (commentsWithNullBookMagazine > 0)
                {
                    _logger.LogWarning("⚠️ Found {NullBookMagazineCount} comments with null BookMagazine - data integrity issue", commentsWithNullBookMagazine);
                }

                return Ok(recentComments);
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans la requête avec jointures (navigation properties)
                _logger.LogError(ex, "❌ Invalid operation during recent comments query - possible navigation property issue");
                return StatusCode(500, "Data query error occurred while retrieving recent comments");
            }
            catch (ArgumentNullException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec les paramètres null dans les jointures
                _logger.LogError(ex, "❌ Null argument error during recent comments query");
                return StatusCode(500, "Data access configuration error");
            }
            catch (TimeoutException ex)
            {
                // ✅ LOG TECHNIQUE : Timeout sur requête avec jointures multiples
                _logger.LogError(ex, "❌ Database timeout during recent comments query - performance issue with joins");
                return StatusCode(500, "Database timeout - comments query with joins took too long");
            }
            catch (OutOfMemoryException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de mémoire avec beaucoup de commentaires
                _logger.LogError(ex, "❌ Out of memory error during recent comments query - dataset too large");
                return StatusCode(500, "Server memory issue - comments dataset too large");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de connexion
                _logger.LogError(ex, "❌ Database connection error during recent comments query");
                return StatusCode(500, "Database connectivity issue while retrieving comments");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during recent comments query - Count: {Count}", count);
                return StatusCode(500, "An internal error occurred while retrieving recent comments");
            }
        }
    }
}

/*
===== LOGS TECHNIQUES AJOUTÉS DANS CE CONTRÔLEUR =====

✅ LOGS TECHNIQUES (Serilog) :
- Problèmes de performance (requêtes lentes, gros datasets, timeouts)
- Erreurs de calculs d'agrégation (OverflowException, ArithmeticException)
- Incohérences de données (auteurs/utilisateurs/livres null)
- Erreurs de requêtes LINQ (OrderBy, Select, jointures)
- Problèmes de mémoire (OutOfMemoryException)
- Surveillance de la croissance des données (>10k items)
- Erreurs de configuration et paramètres
- Problèmes de connectivité base de données

❌ LOGS D'AUDIT NON INCLUS :
- Analytics d'usage public (qui consulte quoi)
- Statistiques de trafic web
- Géolocalisation des requêtes
- Métriques métier d'utilisation
- Patterns d'usage des endpoints

===== EXEMPLES DE LOGS TECHNIQUES GÉNÉRÉS =====

[15:30:16 WRN] ⚠️ Large dataset requested for top books/magazines: 75 items - potential performance impact
[15:32:45 WRN] ⚠️ Found 12 books/magazines with null Author - data integrity issue
[15:35:20 ERR] ❌ Overflow error during statistics calculation - values too large for sum operations
[15:40:10 INF] 📊 Large dataset detected - 15000 books/magazines - consider performance optimization
[15:42:30 WRN] ⚠️ Negative values detected in statistics - TotalDownloads: -50 - data integrity issue
[15:45:15 WRN] ⚠️ Found 8 comments with null User - data integrity issue
[15:50:20 ERR] ❌ Database timeout during recent comments query - performance issue with joins

CES LOGS AIDENT À :
✅ Détecter les problèmes de performance sur API publique
✅ Surveiller l'intégrité des données exposées publiquement
✅ Identifier les calculs d'agrégation problématiques
✅ Monitorer la croissance des datasets
✅ Détecter les requêtes publiques coûteuses
✅ Surveiller les timeouts sur endpoints publics

SPÉCIFICITÉS API PUBLIQUE :
✅ Surveillance renforcée des performances (endpoints très sollicités)
✅ Détection des valeurs négatives dans les statistiques
✅ Protection contre les débordements de calculs
✅ Monitoring de l'intégrité des données exposées
✅ Gestion des requêtes avec jointures multiples
✅ Validation stricte des paramètres d'entrée


*/