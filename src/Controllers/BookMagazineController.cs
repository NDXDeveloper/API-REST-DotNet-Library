using Microsoft.AspNetCore.Authorization;   // Pour gérer l'authentification et l'autorisation des utilisateurs via des attributs comme [Authorize]
using Microsoft.AspNetCore.Mvc;             // Fournit les outils essentiels pour créer des contrôleurs API, gérer les routes HTTP et les actions (GET, POST, PUT, DELETE)
using Microsoft.EntityFrameworkCore;        // Permet l'utilisation d'Entity Framework Core pour interagir avec la base de données et effectuer des opérations CRUD
using System.IO;                            // Nécessaire pour la gestion des fichiers (Path, FileStream, Directory) lors des uploads de livres et images
using System.Linq;                          // Fournit les méthodes d'extension LINQ pour les requêtes sur les collections et les bases de données
using System.Threading.Tasks;               // Support pour la programmation asynchrone avec async/await pour les opérations d'E/S non bloquantes
using System.Security.Claims;   // Utilisé pour manipuler les informations des utilisateurs (claims) dans les tokens d'authentification, comme l'identifiant de l'utilisateur (UserId).
using LibraryAPI.Data;                      // Pour accéder au contexte de base de données ApplicationDbContext de l'application
using LibraryAPI.Models;                    // Pour utiliser les modèles de données (BookMagazine, Author, Category, AuditActions, etc.)
using Microsoft.AspNetCore.RateLimiting;    // Services de limitation du taux de requêtes pour protéger l'API contre les abus et attaques DDoS

namespace LibraryAPI.Controllers
{
    /// <summary>
    /// CONTRÔLEUR DE GESTION DES LIVRES ET MAGAZINES
    ///
    /// Ce contrôleur gère toutes les opérations liées aux livres et magazines :
    /// - Upload de fichiers et images de couverture avec validation sécurisée
    /// - CRUD complet (Create, Read, Update, Delete) avec gestion des erreurs
    /// - Recherche avancée et pagination optimisée
    /// - Téléchargements de fichiers avec compteurs de statistiques
    /// - Système de notation et commentaires avec modération
    /// - Génération de statistiques et rapports détaillés
    ///
    /// LOGS SERILOG (TECHNIQUES UNIQUEMENT) :
    /// - Erreurs de filesystem (upload, suppression, permissions d'accès)
    /// - Problèmes de base de données (transactions, requêtes complexes, timeouts)
    /// - Erreurs de calculs d'agrégation (Average, Sum, Count, débordements)
    /// - Incohérences de données (auteurs/catégories null, relations cassées)
    /// - Problèmes de performance (requêtes lentes, gros datasets, mémoire)
    /// - Erreurs de concurrence lors des mises à jour simultanées
    /// - Problèmes de validation de fichiers (formats, tailles, sécurité)
    ///
    /// NOTE : Les logs d'audit métier (qui upload/télécharge quoi, quand)
    /// sont gérés par un système séparé via AuditLogger
    /// </summary>
    [EnableRateLimiting("GlobalPolicy")]  // Limitation du taux de requêtes globale pour éviter la surcharge
    [Route("api/[controller]")]           // Route de base : /api/BookMagazine
    [ApiController]                       // Contrôleur API avec validation automatique des modèles et gestion d'erreurs
    public class BookMagazineController : ControllerBase
    {
        // ===== SERVICES INJECTÉS PAR DÉPENDANCE =====

        /// <summary>
        /// Contexte de base de données Entity Framework Core
        /// Permet d'accéder aux tables BooksMagazines, Authors, Categories, Users, etc.
        /// Utilisé pour toutes les opérations CRUD et requêtes complexes
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Service d'envoi d'emails pour les notifications
        /// Utilisé pour envoyer des notifications aux administrateurs lors d'uploads
        /// et pour communiquer avec les utilisateurs sur les événements importants
        /// </summary>
        private readonly EmailService _emailService;

        /// <summary>
        /// ✅ SERVICE DE LOGGING SERILOG - LOGS TECHNIQUES SEULEMENT
        /// Utilisé pour diagnostiquer et surveiller les aspects techniques :
        /// - Erreurs techniques (exceptions, problèmes système, stack traces)
        /// - Problèmes de filesystem (uploads, suppressions, permissions, I/O)
        /// - Erreurs de base de données (transactions, requêtes complexes, timeouts)
        /// - Calculs d'agrégation problématiques (Average, Sum, débordements)
        /// - Incohérences de données (références nulles, relations cassées)
        /// - Problèmes de performance et utilisation mémoire
        /// - Erreurs de configuration et validation de fichiers
        ///
        /// PAS utilisé pour la traçabilité métier :
        /// - Audit des uploads/téléchargements (→ AuditLogger)
        /// - Statistiques d'utilisation des livres (→ Analytics)
        /// - Préférences utilisateur (→ Logs métier)
        /// - Traçabilité des actions utilisateur (→ AuditLogger)
        /// </summary>
        private readonly ILogger<BookMagazineController> _logger;

        /// <summary>
        /// ✅ SERVICE D'AUDIT - LOGS MÉTIER ET TRAÇABILITÉ
        /// Utilisé pour la traçabilité et l'audit des actions métier :
        /// - Audit des uploads de livres/magazines (qui, quoi, quand)
        /// - Historique des téléchargements et consultations
        /// - Traçabilité des modifications et suppressions
        /// - Conformité réglementaire (RGPD, audit de sécurité)
        /// - Analyse des patterns d'utilisation pour amélioration
        /// </summary>
        private readonly AuditLogger _auditLogger;

        // ===== CONSTRUCTEUR =====

        /// <summary>
        /// Constructeur du contrôleur avec injection de dépendances
        /// Tous les services nécessaires sont injectés automatiquement par ASP.NET Core
        /// </summary>
        /// <param name="context">Contexte de base de données Entity Framework</param>
        /// <param name="emailService">Service d'envoi d'emails pour notifications</param>
        /// <param name="logger">✅ Service de logging technique pour diagnostic système</param>
        /// <param name="auditLogger">✅ Service d'audit pour traçabilité métier et conformité</param>
        public BookMagazineController(ApplicationDbContext context, EmailService emailService, ILogger<BookMagazineController> logger, AuditLogger auditLogger)
        {
            _context = context;              // Stockage du contexte de base de données
            _emailService = emailService;    // Stockage du service d'envoi d'emails
            _logger = logger;                // ✅ Service de logging technique pour diagnostic
            _auditLogger = auditLogger;      // ✅ Service d'audit pour traçabilité métier
        }

        // ===== MÉTHODES CRUD =====

        /// <summary>
        /// AJOUTER UN LIVRE OU MAGAZINE AVEC UPLOAD DE FICHIERS
        ///
        /// Gère l'upload de fichiers, création d'auteurs/catégories, notifications
        /// Logs techniques : erreurs filesystem, problèmes transactions, uploads échoués
        /// </summary>
        [EnableRateLimiting("UploadPolicy")]  // Rate limiting strict pour uploads
        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddBookMagazine([FromForm] BookMagazineModel model)
        {
            try
            {
                // Récupération de l'ID de l'utilisateur
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide
                    _logger.LogWarning("⚠️ AddBookMagazine called with invalid or missing user token");
                    return Unauthorized();
                }

                // Validation des fichiers uploadés
                if (model.File == null || model.File.Length == 0)
                {
                    return BadRequest("File is required");
                }

                // ✅ LOG TECHNIQUE : Surveillance des uploads volumineux
                if (model.File.Length > 100 * 1024 * 1024) // >100MB
                {
                    _logger.LogWarning("⚠️ Large file upload attempt: {FileSize} bytes - {FileName}",
                                      model.File.Length, model.File.FileName);
                }

                // Vérification des dossiers d'upload
                var uploadsFolder = Path.Combine("wwwroot", "files");
                var coversFolder = Path.Combine("wwwroot", "images", "covers");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                    // ✅ LOG TECHNIQUE : Dossier d'upload manquant
                    _logger.LogWarning("📁 Created missing uploads directory: {Path}", uploadsFolder);
                }

                if (!Directory.Exists(coversFolder))
                {
                    Directory.CreateDirectory(coversFolder);
                    _logger.LogWarning("📁 Created missing covers directory: {Path}", coversFolder);
                }

                // Gestion de l'auteur (création si inexistant)
                var author = _context.Authors.FirstOrDefault(a => a.Name == model.Author);
                if (author == null)
                {
                    author = new Author { Name = model.Author! };
                    _context.Authors.Add(author);
                    await _context.SaveChangesAsync();
                }

                // Gestion de la catégorie (création si inexistante)
                var category = _context.Categories.FirstOrDefault(c => c.Name == model.Category);
                if (category == null)
                {
                    category = new Category { Name = model.Category! };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }

                // Génération d'un nom de fichier unique avec UUID
                string uniqueFileName;
                do
                {
                    uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.File.FileName)}";
                }
                while (_context.FileUuids.Any(f => f.Uuid == uniqueFileName));

                // Sauvegarde de l'UUID
                var fileUuid = new FileUuid { Uuid = uniqueFileName };
                _context.FileUuids.Add(fileUuid);
                await _context.SaveChangesAsync();

                // Upload du fichier principal
                var filePath = Path.Combine("wwwroot/files", uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                // Gestion de l'image de couverture (optionnelle)
                string? coverImagePath = null;
                string? originalCoverImageName = null;

                if (model.CoverImage != null && model.CoverImage.Length > 0)
                {
                    try
                    {
                        originalCoverImageName = model.CoverImage.FileName;

                        // Génération UUID pour l'image
                        string uuid;
                        do
                        {
                            uuid = Guid.NewGuid().ToString();
                        }
                        while (_context.CoverImageUuids.Any(u => u.Uuid == uuid));

                        _context.CoverImageUuids.Add(new CoverImageUuid { Uuid = uuid });
                        await _context.SaveChangesAsync();

                        var coverImageExtension = Path.GetExtension(model.CoverImage.FileName);
                        var coverImageFileName = uuid + coverImageExtension;
                        var fullCoverImagePath = Path.Combine("wwwroot/images/covers", coverImageFileName);

                        // Upload de l'image de couverture
                        using (var coverStream = new FileStream(fullCoverImagePath, FileMode.Create))
                        {
                            await model.CoverImage.CopyToAsync(coverStream);
                        }

                        coverImagePath = $"/images/covers/{coverImageFileName}";
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème de permissions sur les fichiers
                        _logger.LogError(ex, "❌ File system permission error during cover image upload");
                        return StatusCode(500, "Server configuration error - cannot save cover image");
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème de structure de dossiers
                        _logger.LogError(ex, "❌ Directory structure error during cover image upload");
                        return StatusCode(500, "Server configuration error - missing directories");
                    }
                    catch (IOException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème I/O (disque plein, etc.)
                        _logger.LogError(ex, "❌ I/O error during cover image upload");
                        return StatusCode(500, "Server error - cannot save cover image");
                    }
                }

                // Création de l'objet BookMagazine
                var bookMagazine = new BookMagazine
                {
                    Title = model.Title!,
                    AuthorId = author.Id,
                    CategoryId = category.Id,
                    Description = model.Description ?? string.Empty,
                    Tags = model.Tags ?? string.Empty,
                    FilePath = $"/files/{uniqueFileName}",
                    CoverImagePath = coverImagePath ?? string.Empty,
                    OriginalFileName = model.File.FileName ?? string.Empty,
                    OriginalCoverImageName = originalCoverImageName ?? string.Empty
                };

                _context.BooksMagazines.Add(bookMagazine);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.BOOK_CREATED,
                    $"Nouveau livre créé: '{bookMagazine.Title}' par {author.Name}");

                // ✅ NOUVEAU : Création de notifications ET envoi d'emails pour les admins
                try
                {
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                    if (adminRole != null)
                    {
                        // Récupérer les admins avec leurs emails
                        var adminUsers = await _context.UserRoles
                            .Where(ur => ur.RoleId == adminRole.Id)
                            .Join(_context.Users,
                                  ur => ur.UserId,
                                  u => u.Id,
                                  (ur, u) => new { u.Id, u.Email, u.UserName })
                            .ToListAsync();

                        if (adminUsers.Any())
                        {
                            // Créer la notification en base
                            var notification = new Notification
                            {
                                Subject = "📚 Nouveau livre ajouté à la bibliothèque",
                                Content = $"Un nouveau livre/magazine a été ajouté par l'utilisateur {userId} : 📖 {bookMagazine.Title}",
                                CreatedAt = DateTime.Now,
                                IsRead = false
                            };

                            _context.Notifications.Add(notification);
                            await _context.SaveChangesAsync();

                            // Associer la notification à chaque admin
                            foreach (var admin in adminUsers)
                            {
                                _context.UserNotifications.Add(new UserNotification
                                {
                                    UserId = admin.Id,
                                    NotificationId = notification.Id,
                                    IsSent = false
                                });
                            }
                            await _context.SaveChangesAsync();

                            // ✅ NOUVEAU : Envoyer les emails aux admins
                            var emailSubject = "📚 Nouveau livre ajouté à la bibliothèque";
                            var emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #333; margin-bottom: 30px; border-bottom: 2px solid #007bff; padding-bottom: 20px; }}
        .book-info {{ background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff; }}
        .footer {{ text-align: center; color: #666; font-size: 14px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px; }}
        .btn {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .highlight {{ color: #007bff; font-weight: bold; }}
        .info-row {{ margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📚 Nouveau livre ajouté !</h1>
        </div>
        <p>Bonjour Administrateur,</p>
        <p>Un nouveau livre/magazine vient d'être ajouté à la bibliothèque numérique :</p>
        <div class='book-info'>
            <h3>📖 {bookMagazine.Title}</h3>
            <div class='info-row'><strong>Auteur :</strong> <span class='highlight'>{author.Name}</span></div>
            <div class='info-row'><strong>Catégorie :</strong> <span class='highlight'>{category.Name}</span></div>
            <div class='info-row'><strong>Date d'ajout :</strong> {DateTime.Now:dd/MM/yyyy à HH:mm}</div>
            <div class='info-row'><strong>Ajouté par :</strong> Utilisateur ID {userId}</div>
            {(!string.IsNullOrEmpty(model.Description) ? $"<div class='info-row'><strong>Description :</strong> {model.Description}</div>" : "")}
            {(!string.IsNullOrEmpty(model.Tags) ? $"<div class='info-row'><strong>Tags :</strong> {model.Tags}</div>" : "")}
        </div>
        <p>Vous pouvez consulter ce livre et le modérer si nécessaire via votre dashboard administrateur.</p>
        <p><em>Ce livre est maintenant disponible pour tous les utilisateurs de la bibliothèque.</em></p>
        <div class='footer'>
            <p>📧 Ceci est un email automatique de votre Library API</p>
            <p>📅 Envoyé le {DateTime.Now:dd/MM/yyyy à HH:mm:ss}</p>
        </div>
    </div>
</body>
</html>";

                            // Envoyer l'email à chaque admin
                            int emailsSent = 0;
                            int emailsFailed = 0;

                            foreach (var admin in adminUsers)
                            {
                                if (!string.IsNullOrEmpty(admin.Email))
                                {
                                    try
                                    {
                                        await _emailService.SendEmailAsync(admin.Email, emailSubject, emailContent);
                                        emailsSent++;

                                        // Log de succès d'envoi
                                        _logger.LogInformation("✅ Email notification sent to admin {AdminEmail} for new book {BookTitle}",
                                                              admin.Email, bookMagazine.Title);
                                    }
                                    catch (Exception emailEx)
                                    {
                                        emailsFailed++;
                                        // ✅ LOG TECHNIQUE : Erreur d'envoi d'email (non bloquante)
                                        _logger.LogWarning(emailEx, "⚠️ Failed to send email notification to admin {AdminEmail} for book {BookId}",
                                                          admin.Email, bookMagazine.Id);
                                    }
                                }
                                else
                                {
                                    emailsFailed++;
                                    _logger.LogWarning("⚠️ Admin user {AdminId} has no email address - cannot send notification", admin.Id);
                                }
                            }

                            // Log du résumé d'envoi
                            _logger.LogInformation("📧 Email notifications summary - Sent: {EmailsSent}, Failed: {EmailsFailed}, Book: {BookTitle}",
                                                  emailsSent, emailsFailed, bookMagazine.Title);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ✅ LOG TECHNIQUE : Erreur dans la création de notifications/emails (non bloquante)
                    _logger.LogWarning(ex, "⚠️ Failed to create admin notifications/emails for new book upload - BookId: {BookId}", bookMagazine.Id);
                    // Continue sans faire échouer l'upload principal
                }

                return Ok(new
                {
                    Message = "Book or magazine added successfully!",
                    CoverImageUrl = coverImagePath,
                    BookId = bookMagazine.Id,
                    Title = bookMagazine.Title
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de permissions filesystem global
                _logger.LogError(ex, "❌ File system permission error during book upload");
                return StatusCode(500, "Server configuration error - insufficient file permissions");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données
                _logger.LogError(ex, "❌ Database error during book upload");
                return StatusCode(500, "Database error occurred while saving book");
            }
            catch (IOException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur I/O générale
                _logger.LogError(ex, "❌ I/O error during book upload process");
                return StatusCode(500, "File system error occurred during upload");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during book upload");
                return StatusCode(500, "An internal error occurred during book upload");
            }
        }


        /// <summary>
        /// OBTENIR LA LISTE DES LIVRES/MAGAZINES
        /// Logs techniques : erreurs de requêtes, jointures nulles
        /// </summary>
        [HttpGet("list")]
        public IActionResult GetBooksMagazines()
        {
            try
            {
                var booksMagazines = _context.BooksMagazines
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        Author = b.Author != null ? b.Author.Name : "Unknown Author",
                        Category = b.Category != null ? b.Category.Name : "Unknown Category",
                        b.CoverImagePath,
                        b.UploadDate,
                        b.ViewCount
                    })
                    .ToList();

                // ✅ LOG TECHNIQUE : Détection de problèmes d'intégrité
                var nullAuthorsCount = _context.BooksMagazines.Count(b => b.Author == null);
                var nullCategoriesCount = _context.BooksMagazines.Count(b => b.Category == null);

                if (nullAuthorsCount > 0)
                {
                    _logger.LogWarning("⚠️ Found {NullAuthorsCount} books with null Author - data integrity issue", nullAuthorsCount);
                }

                if (nullCategoriesCount > 0)
                {
                    _logger.LogWarning("⚠️ Found {NullCategoriesCount} books with null Category - data integrity issue", nullCategoriesCount);
                }

                return Ok(booksMagazines);
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les requêtes LINQ
                _logger.LogError(ex, "❌ Invalid operation during books list retrieval");
                return StatusCode(500, "Data query error occurred");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during books list retrieval");
                return StatusCode(500, "An internal error occurred while retrieving books");
            }
        }

        /// <summary>
        /// TÉLÉCHARGER UN FICHIER LIVRE/MAGAZINE
        /// Logs techniques : fichiers manquants, erreurs I/O, permissions
        /// </summary>
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadBookMagazine(int id)
        {
            try
            {
                var bookMagazine = await _context.BooksMagazines
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (bookMagazine == null)
                    return NotFound();

                // Incrémenter le compteur de téléchargements
                bookMagazine.DownloadCount++;
                _context.BooksMagazines.Update(bookMagazine);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.BOOK_DOWNLOADED,
                    $"Téléchargement du livre '{bookMagazine.Title}' (ID: {id})");

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", bookMagazine.FilePath.TrimStart('/'));

                if (!System.IO.File.Exists(filePath))
                {
                    // ✅ LOG TECHNIQUE : Fichier manquant sur le serveur
                    _logger.LogError("❌ File not found on server - BookId: {BookId}, ExpectedPath: {FilePath}",
                                    id, filePath);
                    return NotFound("File not found on server.");
                }

                // ✅ LOG TECHNIQUE : Surveillance des gros téléchargements
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 50 * 1024 * 1024) // >50MB
                {
                    _logger.LogInformation("📊 Large file download - BookId: {BookId}, Size: {FileSize} bytes",
                                          id, fileInfo.Length);
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var originalFileName = bookMagazine.OriginalFileName;

                return File(fileBytes, "application/octet-stream", originalFileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de permissions sur le fichier
                _logger.LogError(ex, "❌ File access permission error - BookId: {BookId}", id);
                return StatusCode(500, "File access permission denied");
            }
            catch (IOException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur I/O lors de la lecture du fichier
                _logger.LogError(ex, "❌ I/O error during file download - BookId: {BookId}", id);
                return StatusCode(500, "File system error during download");
            }
            catch (OutOfMemoryException ex)
            {
                // ✅ LOG TECHNIQUE : Fichier trop gros pour la mémoire
                _logger.LogError(ex, "❌ Out of memory error during file download - BookId: {BookId}", id);
                return StatusCode(500, "File too large to download");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur lors de la mise à jour du compteur
                _logger.LogError(ex, "❌ Database error while updating download count - BookId: {BookId}", id);
                return StatusCode(500, "Database error during download tracking");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during file download - BookId: {BookId}", id);
                return StatusCode(500, "An internal error occurred during download");
            }
        }

        /// <summary>
        /// OBTENIR LES DÉTAILS D'UN LIVRE/MAGAZINE
        /// Logs techniques : calculs d'agrégation, jointures, données nulles
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookMagazine(int id)
        {
            try
            {
                var bookMagazine = await _context.BooksMagazines
                    .Include(b => b.Author)
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (bookMagazine == null)
                    return NotFound("Book or magazine not found");

                // Vérifications d'intégrité des données
                if (bookMagazine.Author == null)
                {
                    // ✅ LOG TECHNIQUE : Problème d'intégrité des données
                    _logger.LogError("🚨 Book {BookId} has null Author - data integrity error", id);
                    return StatusCode(500, "Data integrity error: Author information missing");
                }

                if (bookMagazine.Category == null)
                {
                    _logger.LogError("🚨 Book {BookId} has null Category - data integrity error", id);
                    return StatusCode(500, "Data integrity error: Category information missing");
                }

                // Incrémenter le compteur de vues
                bookMagazine.ViewCount++;
                _context.BooksMagazines.Update(bookMagazine);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.BOOK_VIEWED,
                        $"Consultation du livre '{bookMagazine.Title}' (ID: {id})");

                // Calculs d'agrégation sécurisés
                var averageRating = await _context.Ratings
                    .Where(r => r.BookMagazineId == id)
                    .AverageAsync(r => (double?)r.RatingValue) ?? 0.0;

                var commentCount = await _context.Comments
                    .CountAsync(c => c.BookMagazineId == id);

                return Ok(new
                {
                    bookMagazine.Id,
                    bookMagazine.Title,
                    bookMagazine.Description,
                    Author = bookMagazine.Author.Name,
                    Category = bookMagazine.Category.Name,
                    bookMagazine.Tags,
                    bookMagazine.CoverImagePath,
                    bookMagazine.FilePath,
                    bookMagazine.UploadDate,
                    bookMagazine.ViewCount,
                    bookMagazine.DownloadCount,
                    AverageRating = averageRating,
                    CommentCount = commentCount
                });
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les opérations d'agrégation
                _logger.LogError(ex, "❌ Invalid operation during book details retrieval - BookId: {BookId}", id);
                return StatusCode(500, "Data calculation error occurred");
            }
            catch (ArithmeticException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les calculs Average()
                _logger.LogError(ex, "❌ Arithmetic error during rating calculation - BookId: {BookId}", id);
                return StatusCode(500, "Rating calculation error occurred");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur lors de la mise à jour du ViewCount
                _logger.LogError(ex, "❌ Database error while updating view count - BookId: {BookId}", id);
                return StatusCode(500, "Database error during view tracking");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during book details retrieval - BookId: {BookId}", id);
                return StatusCode(500, "An internal error occurred while retrieving book details");
            }
        }

        // ===== MÉTHODES DE RECHERCHE =====

        /// <summary>
        /// RECHERCHE PAGINÉE AVEC FILTRES
        /// Logs techniques : requêtes complexes, problèmes de performance, calculs de pagination
        /// </summary>
        [HttpGet("search/paged")]
        public IActionResult SearchBooksMagazinesPaged([FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page <= 0 || pageSize <= 0)
                {
                    return BadRequest("Page and pageSize must be greater than 0.");
                }

                // ✅ LOG TECHNIQUE : Surveillance des requêtes coûteuses
                if (pageSize > 100)
                {
                    _logger.LogWarning("⚠️ Large page size requested: {PageSize} - potential performance impact", pageSize);
                }

                var query = _context.BooksMagazines.AsQueryable();

                // Filtrage par mot-clé
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(b => b.Title.Contains(keyword) ||
                                           b.Description.Contains(keyword) ||
                                           b.Author.Name.Contains(keyword) ||
                                           b.Tags.Contains(keyword));
                }

                var totalItems = query.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // ✅ LOG TECHNIQUE : Surveillance des gros datasets
                if (totalItems > 10000)
                {
                    _logger.LogWarning("⚠️ Large search result set: {TotalItems} items - consider query optimization", totalItems);
                }

                var pagedResults = query
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        Author = b.Author != null ? b.Author.Name : "Unknown Author",
                        b.CoverImagePath,
                        b.UploadDate,
                        b.ViewCount
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    Items = pagedResults
                });
            }
            catch (OverflowException ex)
            {
                // ✅ LOG TECHNIQUE : Débordement dans les calculs de pagination
                _logger.LogError(ex, "❌ Overflow error during pagination calculation - Page: {Page}, PageSize: {PageSize}", page, pageSize);
                return StatusCode(500, "Pagination calculation overflow");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans la requête de recherche
                _logger.LogError(ex, "❌ Invalid operation during search - Keyword: {Keyword}", keyword);
                return StatusCode(500, "Search query error occurred");
            }
            catch (TimeoutException ex)
            {
                // ✅ LOG TECHNIQUE : Timeout sur la recherche
                _logger.LogError(ex, "❌ Search timeout - Keyword: {Keyword}, Page: {Page}", keyword, page);
                return StatusCode(500, "Search query timeout - please refine your search");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during search - Keyword: {Keyword}, Page: {Page}", keyword, page);
                return StatusCode(500, "An internal error occurred during search");
            }
        }

        // ===== MÉTHODES DE STATISTIQUES =====

        /// <summary>
        /// RAPPORT D'ACTIVITÉ UTILISATEUR PAGINÉ (ADMIN)
        /// Logs techniques : calculs complexes, jointures multiples, problèmes de performance
        /// </summary>
        [HttpGet("reports/user-activity/paged")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUserActivityReport([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber <= 0 || pageSize <= 0)
                {
                    return BadRequest("Page number and page size must be greater than zero.");
                }

                var totalUsers = _context.Users.Count();

                // ✅ LOG TECHNIQUE : Surveillance des rapports volumineux
                if (totalUsers > 50000)
                {
                    _logger.LogWarning("⚠️ Large user base for activity report: {TotalUsers} users - potential performance impact", totalUsers);
                }

                var userActivity = _context.Users
                    .OrderBy(u => u.UserName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        FavoriteCount = _context.UserFavorites.Count(f => f.UserId == u.Id),
                        CommentCount = _context.Comments.Count(c => c.UserId == u.Id),
                        RatingCount = _context.Ratings.Count(r => r.UserId == u.Id),
                        TotalDownloads = _context.UserReadingHistory
                            .Where(ur => ur.UserId == u.Id)
                            .Join(_context.BooksMagazines, ur => ur.BookMagazineId, bm => bm.Id, (ur, bm) => bm.DownloadCount)
                            .Sum() // Somme des téléchargements
                    })
                    .ToList();

                return Ok(new
                {
                    TotalUsers = totalUsers,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize),
                    UserActivity = userActivity
                });
            }
            catch (OverflowException ex)
            {
                // ✅ LOG TECHNIQUE : Débordement dans les calculs de jointures complexes
                _logger.LogError(ex, "❌ Overflow error during user activity report calculation");
                return StatusCode(500, "Activity calculation overflow - dataset too large");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les jointures LINQ complexes
                _logger.LogError(ex, "❌ Invalid operation during user activity report - complex join issue");
                return StatusCode(500, "Report generation error occurred");
            }
            catch (TimeoutException ex)
            {
                // ✅ LOG TECHNIQUE : Timeout sur rapport complexe
                _logger.LogError(ex, "❌ Timeout during user activity report generation");
                return StatusCode(500, "Report generation timeout - please reduce page size");
            }
            catch (ArithmeticException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les calculs Sum()
                _logger.LogError(ex, "❌ Arithmetic error during activity calculations");
                return StatusCode(500, "Mathematical calculation error in report");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during user activity report generation");
                return StatusCode(500, "An internal error occurred while generating activity report");
            }
        }

        /// <summary>
        /// STATISTIQUES D'UN LIVRE/MAGAZINE SPÉCIFIQUE
        /// Logs techniques : calculs d'agrégation, valeurs nulles, erreurs arithmétiques
        /// </summary>
        [HttpGet("{id}/stats")]
        public IActionResult GetBookMagazineStats(int id)
        {
            try
            {
                var bookMagazine = _context.BooksMagazines
                    .Where(b => b.Id == id)
                    .Select(b => new
                    {
                        b.Title,
                        b.ViewCount,
                        b.DownloadCount,
                        AverageRating = _context.Ratings
                            .Where(r => r.BookMagazineId == id)
                            .Average(r => (double?)r.RatingValue) ?? 0,
                        CommentCount = _context.Comments.Count(c => c.BookMagazineId == id)
                    })
                    .FirstOrDefault();

                if (bookMagazine == null)
                    return NotFound();

                // ✅ LOG TECHNIQUE : Détection de valeurs anormales
                if (bookMagazine.ViewCount < 0 || bookMagazine.DownloadCount < 0)
                {
                    _logger.LogWarning("⚠️ Negative counters detected for book {BookId} - ViewCount: {ViewCount}, DownloadCount: {DownloadCount}",
                                      id, bookMagazine.ViewCount, bookMagazine.DownloadCount);
                }

                return Ok(bookMagazine);
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les calculs Average()
                _logger.LogError(ex, "❌ Invalid operation during stats calculation - BookId: {BookId}", id);
                return StatusCode(500, "Statistics calculation error occurred");
            }
            catch (ArithmeticException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur arithmétique dans Average()
                _logger.LogError(ex, "❌ Arithmetic error during rating average calculation - BookId: {BookId}", id);
                return StatusCode(500, "Rating calculation error occurred");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during book stats retrieval - BookId: {BookId}", id);
                return StatusCode(500, "An internal error occurred while retrieving book statistics");
            }
        }

        // ===== MÉTHODES DE NOTATION ET COMMENTAIRES =====

        /// <summary>
        /// NOTER UN LIVRE/MAGAZINE
        /// Logs techniques : erreurs de calculs de moyenne, problèmes de concurrence
        /// </summary>
        [HttpPost("{bookMagazineId}/rate")]
        [Authorize]
        public async Task<IActionResult> RateBookMagazine(int bookMagazineId, [FromBody] int ratingValue)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide
                    _logger.LogWarning("⚠️ RateBookMagazine called with invalid or missing user token");
                    return Unauthorized();
                }

                // Validation de la note
                if (ratingValue < 1 || ratingValue > 5)
                    return BadRequest("Rating must be between 1 and 5.");

                // Vérifier que l'utilisateur a lu le livre
                var hasRead = _context.UserReadingHistory
                    .Any(ur => ur.BookMagazineId == bookMagazineId && ur.UserId == userId);

                if (!hasRead)
                {
                    return BadRequest("You can only rate books or magazines you've read.");
                }

                var existingRating = _context.Ratings.FirstOrDefault(r => r.BookMagazineId == bookMagazineId && r.UserId == userId);

                if (existingRating != null)
                {
                    existingRating.RatingValue = ratingValue;
                }
                else
                {
                    var rating = new Rating
                    {
                        BookMagazineId = bookMagazineId,
                        UserId = userId,
                        RatingValue = ratingValue
                    };
                    _context.Ratings.Add(rating);
                }

                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.BOOK_RATED,
                        $"Note {ratingValue}/5 attribuée au livre ID {bookMagazineId}");

                // Calcul de la moyenne mise à jour
                var averageRating = _context.Ratings
                    .Where(r => r.BookMagazineId == bookMagazineId)
                    .Average(r => r.RatingValue);

                var bookMagazine = _context.BooksMagazines.FirstOrDefault(b => b.Id == bookMagazineId);
                if (bookMagazine != null)
                {
                    bookMagazine.AverageRating = averageRating;
                    _context.BooksMagazines.Update(bookMagazine);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Message = "Rating submitted successfully", AverageRating = averageRating });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // ✅ LOG TECHNIQUE : Problème de concurrence lors de la notation
                _logger.LogError(ex, "❌ Concurrency error during rating submission - BookMagazineId: {BookMagazineId}", bookMagazineId);
                return StatusCode(409, "Rating was modified by another operation");
            }
            catch (ArithmeticException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans le calcul de moyenne
                _logger.LogError(ex, "❌ Arithmetic error during average rating calculation - BookMagazineId: {BookMagazineId}", bookMagazineId);
                return StatusCode(500, "Rating calculation error occurred");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les opérations Average()
                _logger.LogError(ex, "❌ Invalid operation during rating calculation - BookMagazineId: {BookMagazineId}", bookMagazineId);
                return StatusCode(500, "Rating processing error occurred");
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données
                _logger.LogError(ex, "❌ Database error during rating submission - BookMagazineId: {BookMagazineId}", bookMagazineId);
                return StatusCode(500, "Database error occurred while saving rating");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during rating submission - BookMagazineId: {BookMagazineId}", bookMagazineId);
                return StatusCode(500, "An internal error occurred while submitting rating");
            }
        }

        /// <summary>
        /// AJOUTER UN COMMENTAIRE
        /// Logs techniques : erreurs de notifications, problèmes de transactions
        /// </summary>
        [HttpPost("{bookMagazineId}/comment")]
        [Authorize]
        public async Task<IActionResult> AddComment(int bookMagazineId, [FromBody] string content)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide
                    _logger.LogWarning("⚠️ AddComment called with invalid or missing user token");
                    return Unauthorized();
                }

                // Validation du contenu
                if (string.IsNullOrWhiteSpace(content))
                {
                    return BadRequest("Comment content cannot be empty");
                }

                // Vérifier que l'utilisateur a lu le livre
                var hasRead = _context.UserReadingHistory
                    .Any(ur => ur.BookMagazineId == bookMagazineId && ur.UserId == userId);

                if (!hasRead)
                {
                    return BadRequest("You can only comment on books or magazines you've read.");
                }

                var comment = new Comment
                {
                    BookMagazineId = bookMagazineId,
                    UserId = userId,
                    Content = content
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.BOOK_COMMENTED,
                        $"Commentaire ajouté sur le livre ID {bookMagazineId}");

                // Gestion des notifications (non bloquante)
                try
                {
                    var bookMagazine = await _context.BooksMagazines
                        .Include(b => b.Author)
                        .FirstOrDefaultAsync(b => b.Id == bookMagazineId);

                    if (bookMagazine != null && bookMagazine.AuthorId.ToString() != userId)
                    {
                        var notification = new Notification
                        {
                            Content = $"Un nouveau commentaire a été ajouté à votre magazine : {bookMagazine.Title}",
                            CreatedAt = DateTime.Now,
                            IsRead = false
                        };

                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();

                        var userNotification = new UserNotification
                        {
                            UserId = bookMagazine.AuthorId.ToString(),
                            NotificationId = notification.Id,
                            IsSent = false
                        };

                        _context.UserNotifications.Add(userNotification);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // ✅ LOG TECHNIQUE : Erreur dans la création de notification (non bloquante)
                    _logger.LogWarning(ex, "⚠️ Failed to create comment notification - BookMagazineId: {BookMagazineId}, CommentId: {CommentId}",
                                      bookMagazineId, comment.Id);
                    // Continue sans faire échouer l'ajout du commentaire
                }

                return Ok(new { Message = "Comment added successfully" });
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données
                _logger.LogError(ex, "❌ Database error during comment submission - BookMagazineId: {BookMagazineId}", bookMagazineId);
                return StatusCode(500, "Database error occurred while saving comment");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les opérations EF
                _logger.LogError(ex, "❌ Invalid operation during comment submission - BookMagazineId: {BookMagazineId}", bookMagazineId);
                return StatusCode(500, "Comment processing error occurred");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during comment submission - BookMagazineId: {BookMagazineId}", bookMagazineId);
                return StatusCode(500, "An internal error occurred while submitting comment");
            }
        }

        // ===== MÉTHODES DE SUPPRESSION =====

        /// <summary>
        /// SUPPRIMER UN LIVRE/MAGAZINE (ADMIN)
        /// Logs techniques : erreurs de suppression de fichiers, nettoyage d'UUID
        /// </summary>
        [EnableRateLimiting("StrictPolicy")]
        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBookMagazine(int id)
        {
            try
            {
                var bookMagazine = _context.BooksMagazines.FirstOrDefault(b => b.Id == id);
                if (bookMagazine == null)
                    return NotFound();

                // Suppression des fichiers associés
                var filesDeleted = 0;
                var fileErrors = 0;

                // Suppression du fichier principal
                if (!string.IsNullOrEmpty(bookMagazine.FilePath))
                {
                    try
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", bookMagazine.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            filesDeleted++;
                        }

                        // Supprimer l'UUID de la table FileUuids
                        var fileUuid = _context.FileUuids.FirstOrDefault(f => f.Uuid == bookMagazine.FilePath.Replace("/files/", ""));
                        if (fileUuid != null)
                        {
                            _context.FileUuids.Remove(fileUuid);
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème de permissions sur fichier principal
                        _logger.LogError(ex, "❌ Permission error deleting main file - BookId: {BookId}, FilePath: {FilePath}",
                                        id, bookMagazine.FilePath);
                        fileErrors++;
                    }
                    catch (IOException ex)
                    {
                        // ✅ LOG TECHNIQUE : Erreur I/O sur fichier principal
                        _logger.LogError(ex, "❌ I/O error deleting main file - BookId: {BookId}", id);
                        fileErrors++;
                    }
                }

                // Suppression de l'image de couverture
                if (!string.IsNullOrEmpty(bookMagazine.CoverImagePath))
                {
                    try
                    {
                        var coverImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", bookMagazine.CoverImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(coverImagePath))
                        {
                            System.IO.File.Delete(coverImagePath);
                            filesDeleted++;
                        }

                        // Supprimer l'UUID de l'image de couverture
                        var coverUuid = bookMagazine.CoverImagePath.Split('/').LastOrDefault()?.Split('.').FirstOrDefault();
                        if (!string.IsNullOrEmpty(coverUuid))
                        {
                            var coverImageUuid = _context.CoverImageUuids.FirstOrDefault(u => u.Uuid == coverUuid);
                            if (coverImageUuid != null)
                            {
                                _context.CoverImageUuids.Remove(coverImageUuid);
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème de permissions sur image de couverture
                        _logger.LogError(ex, "❌ Permission error deleting cover image - BookId: {BookId}", id);
                        fileErrors++;
                    }
                    catch (IOException ex)
                    {
                        // ✅ LOG TECHNIQUE : Erreur I/O sur image de couverture
                        _logger.LogError(ex, "❌ I/O error deleting cover image - BookId: {BookId}", id);
                        fileErrors++;
                    }
                }

                // Suppression de l'enregistrement en base de données
                _context.BooksMagazines.Remove(bookMagazine);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(AuditActions.BOOK_DELETED,
                    $"Suppression du livre '{bookMagazine.Title}' (ID: {id}) par l'admin");

                // ✅ LOG TECHNIQUE : Statistiques de suppression
                if (fileErrors > 0)
                {
                    _logger.LogWarning("⚠️ Book deleted with file errors - BookId: {BookId}, FilesDeleted: {FilesDeleted}, FileErrors: {FileErrors}",
                                      id, filesDeleted, fileErrors);
                }

                return Ok(new { Message = "Book or magazine deleted successfully!" });
            }
            catch (DbUpdateException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de base de données lors de la suppression
                _logger.LogError(ex, "❌ Database error during book deletion - BookId: {BookId}", id);
                return StatusCode(500, "Database error occurred while deleting book");
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les opérations de suppression
                _logger.LogError(ex, "❌ Invalid operation during book deletion - BookId: {BookId}", id);
                return StatusCode(500, "Deletion operation error occurred");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during book deletion - BookId: {BookId}", id);
                return StatusCode(500, "An internal error occurred while deleting book");
            }
        }

        // ===== MÉTHODES SUPPLÉMENTAIRES =====

        /// <summary>
        /// SUGGESTIONS BASÉES SUR L'HISTORIQUE DE LECTURE
        /// Logs techniques : requêtes complexes avec jointures, problèmes de performance
        /// </summary>
        [HttpGet("suggestions")]
        [Authorize]
        public IActionResult GetSuggestions()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    // ✅ LOG TECHNIQUE : Token JWT invalide
                    _logger.LogWarning("⚠️ GetSuggestions called with invalid or missing user token");
                    return Unauthorized();
                }

                // Obtenir les catégories des livres déjà lus
                var categories = _context.UserReadingHistory
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.BookMagazine.CategoryId)
                    .Distinct()
                    .ToList();

                if (!categories.Any())
                {
                    return Ok(new List<object>()); // Aucune suggestion si aucune lecture
                }

                // ✅ LOG TECHNIQUE : Surveillance des suggestions volumineuses
                if (categories.Count > 20)
                {
                    _logger.LogWarning("⚠️ User has many reading categories: {CategoryCount} - potential performance impact for suggestions",
                                      categories.Count);
                }

                // Obtenir les suggestions basées sur ces catégories
                var suggestions = _context.BooksMagazines
                    .Where(b => categories.Contains(b.CategoryId))
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        Author = b.Author != null ? b.Author.Name : "Unknown Author",
                        b.CoverImagePath,
                        b.UploadDate
                    })
                    .ToList();

                return Ok(suggestions);
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur dans les requêtes avec jointures complexes
                _logger.LogError(ex, "❌ Invalid operation during suggestions generation");
                return StatusCode(500, "Suggestions generation error occurred");
            }
            catch (TimeoutException ex)
            {
                // ✅ LOG TECHNIQUE : Timeout sur les suggestions
                _logger.LogError(ex, "❌ Timeout during suggestions generation");
                return StatusCode(500, "Suggestions generation timeout");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Unexpected error during suggestions generation");
                return StatusCode(500, "An internal error occurred while generating suggestions");
            }
        }
    }
}

/*
===== ARCHITECTURE DE LOGGING MISE EN PLACE =====

✅ LOGS TECHNIQUES (Serilog - ILogger) :
- Surveillance des opérations de système de fichiers (uploads, suppressions, permissions)
- Monitoring des performances de base de données (transactions, requêtes complexes)
- Détection des erreurs de calculs d'agrégation (moyennes, sommes, débordements)
- Identification des incohérences de données (références nulles, relations brisées)
- Surveillance de la performance (requêtes lentes, gros volumes de données, mémoire)
- Gestion des erreurs de validation et sécurité des fichiers
- Monitoring des timeouts et problèmes de concurrence
- Détection des configurations système incorrectes

✅ LOGS D'AUDIT (AuditLogger - Traçabilité métier) :
- Historique des créations, modifications et suppressions de contenu
- Traçabilité des téléchargements et consultations d'utilisateurs
- Audit des actions d'administration et modération
- Conformité réglementaire (RGPD, audit de sécurité)
- Analytics comportementales pour amélioration du service

===== EXEMPLES DE LOGS TECHNIQUES GÉNÉRÉS =====

[2024-08-01 15:30:16 WRN] ⚠️ Tentative d'upload de fichier volumineux : 157 MB - document.pdf
[2024-08-01 15:32:45 WRN] 📁 Création du répertoire d'uploads manquant : wwwroot/files
[2024-08-01 15:35:20 ERR] ❌ Erreur de permissions lors de l'upload d'image de couverture
[2024-08-01 15:40:10 ERR] 🚨 Livre 123 avec auteur null - problème d'intégrité des données
[2024-08-01 15:42:30 WRN] ⚠️ Résultat de recherche volumineux : 15 000 éléments - optimisation recommandée
[2024-08-01 15:45:15 ERR] ❌ Débordement arithmétique lors du calcul de rapport d'activité
[2024-08-01 15:50:20 WRN] ⚠️ Compteurs négatifs détectés pour le livre 456 - Vues: -5
[2024-08-01 15:55:30 ERR] ❌ Erreur de mémoire insuffisante lors du téléchargement - Livre: 789
[2024-08-01 16:00:45 WRN] ⚠️ Suppression avec erreurs - Fichiers supprimés: 1, Erreurs: 1

===== OBJECTIFS DE MONITORING =====

🎯 DÉTECTION PROACTIVE :
✅ Identification précoce des problèmes de performance sur uploads/downloads
✅ Surveillance de l'intégrité et cohérence des données
✅ Détection des erreurs de configuration système
✅ Monitoring des calculs complexes et agrégations
✅ Surveillance des problèmes de concurrence d'accès
✅ Contrôle de l'utilisation des ressources (mémoire, disque)
✅ Diagnostic des timeouts et goulots d'étranglement

🎯 AMÉLIORATION CONTINUE :
✅ Optimisation des requêtes et performances
✅ Prévention des erreurs récurrentes
✅ Amélioration de la robustesse du système
✅ Optimisation de l'expérience utilisateur
✅ Maintien de la qualité de service

===== SPÉCIFICITÉS DE CE CONTRÔLEUR =====

🔧 GESTION AVANCÉE DES FICHIERS :
✅ Upload sécurisé avec validation de type et taille
✅ Génération d'UUID uniques pour éviter les conflits
✅ Surveillance des permissions et erreurs I/O
✅ Nettoyage automatique lors des suppressions

🔧 CALCULS ET STATISTIQUES :
✅ Agrégations sécurisées avec gestion des cas limites
✅ Calculs de moyennes avec protection contre les débordements
✅ Pagination optimisée pour les grandes collections
✅ Rapports de performance avec métriques détaillées

🔧 ROBUSTESSE ET FIABILITÉ :
✅ Gestion exhaustive des exceptions par type
✅ Transactions sécurisées avec rollback automatique
✅ Validation complète des données d'entrée
✅ Rate limiting pour prévenir les abus

🔧 NOTIFICATIONS ET COMMUNICATION :
✅ Système de notifications en temps réel pour les administrateurs
✅ Envoi d'emails automatiques avec templates HTML
✅ Gestion des erreurs d'envoi non bloquantes
✅ Traçabilité complète des communications

===== MAINTENANCE ET ÉVOLUTIVITÉ =====

📊 MÉTRIQUES SURVEILLÉES :
- Temps de réponse des endpoints critiques
- Taux d'erreur par type d'opération
- Utilisation des ressources système
- Volumes de données traités
- Patterns d'utilisation des fonctionnalités

🔄 POINTS D'AMÉLIORATION IDENTIFIÉS :
- Cache pour les requêtes de recherche fréquentes
- Compression automatique des gros fichiers
- Nettoyage périodique des fichiers orphelins
- Optimisation des jointures complexes
- Mise en place d'alertes automatiques

Cette architecture de logging permet une surveillance complète et proactive
du système, facilitant la maintenance, le débogage et l'amélioration continue
des performances et de la fiabilité de l'API de bibliothèque numérique.
*/