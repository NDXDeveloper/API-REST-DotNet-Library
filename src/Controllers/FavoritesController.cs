using Microsoft.AspNetCore.Authorization; // Nécessaire pour gérer l'authentification et l'autorisation des utilisateurs.
using Microsoft.AspNetCore.Mvc; // Fournit les outils pour créer des API RESTful, comme les contrôleurs et les actions HTTP (GET, POST, etc.).
using Microsoft.EntityFrameworkCore; // Permet d'utiliser Entity Framework Core pour interagir avec la base de données via le contexte de données (ApplicationDbContext).
using System.Security.Claims; // Utilisé pour manipuler les informations des utilisateurs (claims) dans les tokens d'authentification, comme l'identifiant de l'utilisateur (UserId).
using LibraryAPI.Data;
using LibraryAPI.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryAPI.Controllers
{
    [EnableRateLimiting("GlobalPolicy")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Cette annotation assure que toutes les actions dans ce contrôleur nécessitent une authentification.
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Dépendance à l'ApplicationDbContext pour interagir avec la base de données.

        public FavoritesController(ApplicationDbContext context) // Le constructeur injecte le contexte de la base de données via l'injection de dépendances.
        {
            _context = context;
        }

        // Ajouter un livre/magazine aux favoris
        [HttpPost("add-favorite/{bookMagazineId}")]
        public async Task<IActionResult> AddFavorite(int bookMagazineId)
        {
            // Récupérer l'identifiant de l'utilisateur connecté via les Claims (informations stockées dans le token d'authentification).
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Si l'utilisateur n'est pas authentifié, retourner un statut 401 (Unauthorized).
            if (userId == null)
            {
                return Unauthorized();
            }

            // Rechercher dans la base de données si le livre/magazine existe avec l'ID fourni.
            var bookMagazine = await _context.BooksMagazines.FindAsync(bookMagazineId);

            // Si le livre/magazine n'est pas trouvé, retourner une réponse 404 (Not Found) avec un message.
            if (bookMagazine == null)
            {
                return NotFound(new { message = $"Book or magazine with ID {bookMagazineId} not found." });
            }

            // Vérifier si ce favori existe déjà pour cet utilisateur (empêcher les doublons).
            var existingFavorite = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BookMagazineId == bookMagazineId);

            // Si le favori existe déjà, retourner un code 409 (Conflict) avec un message approprié.
            if (existingFavorite != null)
            {
                return Conflict(new { message = "This item is already in your favorites." });
            }

            // Ajouter un nouveau favori pour l'utilisateur dans la table UserFavorites.
            var userFavorite = new UserFavorite
            {
                UserId = userId,
                BookMagazineId = bookMagazineId
            };

            // Sauvegarder le nouveau favori dans la base de données.
            await _context.UserFavorites.AddAsync(userFavorite);
            await _context.SaveChangesAsync();

            // Retourner un message de succès avec un statut 200 (OK).
            return Ok(new { message = "Book or magazine successfully added to favorites." });
        }

        // // Récupérer la liste des favoris de l'utilisateur connecté
        // [HttpGet("my-favorites")]
        // public async Task<IActionResult> GetMyFavorites()
        // {
        //     //var userId = User.FindFirst("id")?.Value;
        //     var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //     if (userId == null) return Unauthorized();

        //     var favorites = await _context.UserFavorites
        //         .Where(f => f.UserId == userId)
        //         .Include(f => f.BookMagazine) // Inclure les détails des livres/magazines
        //         .ToListAsync();

        //     return Ok(favorites);
        // }

        // Récupérer la liste des favoris de l'utilisateur connecté
        [HttpGet("my-favorites")]
        public async Task<IActionResult> GetMyFavorites()
        {
            // Récupérer l'identifiant de l'utilisateur connecté via les Claims.
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Si l'utilisateur n'est pas authentifié, retourner un statut 401 (Unauthorized).
            if (userId == null)
            {
                return Unauthorized();
            }

            // Récupérer les favoris de l'utilisateur dans la table UserFavorites et inclure les détails des livres/magazines associés.
            var favorites = await _context.UserFavorites
                .Where(f => f.UserId == userId)
                .Include(f => f.BookMagazine) // Inclure les informations du livre ou magazine lié.
                .ToListAsync();

            // Si aucun favori n'est trouvé, retourner une réponse 404 (Not Found).
            if (favorites == null || !favorites.Any())
            {
                return NotFound(new { message = "No favorites found for the user." });
            }
        
            // Créer une réponse personnalisée, en s'assurant que les informations sur les livres/magazines ne sont pas nulles.
            var response = favorites
                .Where(f => f.BookMagazine != null) // Filtrer les résultats pour s'assurer que les livres/magazines ne sont pas nulls.
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

            // Retourner la liste des favoris avec un statut 200 (OK).
            return Ok(response);
        }

        
        // // Supprimer un livre/magazine des favoris
        // [HttpDelete("remove-favorite/{bookMagazineId}")]
        // public async Task<IActionResult> RemoveFavorite(int bookMagazineId)
        // {
        //     var userId = User.FindFirst("id")?.Value;
        //     if (userId == null) return Unauthorized();

        //     var favorite = await _context.UserFavorites
        //         .FirstOrDefaultAsync(f => f.UserId == userId && f.BookMagazineId == bookMagazineId);
            
        //     if (favorite == null) return NotFound();

        //     _context.UserFavorites.Remove(favorite);
        //     await _context.SaveChangesAsync();

        //     return Ok(new { Message = "Book/Magazine removed from favorites successfully!" });
        // }

        // Supprimer un livre/magazine des favoris
        [HttpDelete("remove-favorite/{bookMagazineId}")]
        public async Task<IActionResult> RemoveFavorite(int bookMagazineId)
        {
            // Récupérer l'identifiant de l'utilisateur connecté via les Claims.
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Si l'utilisateur n'est pas authentifié, retourner un statut 401 (Unauthorized).
            if (userId == null)
            {
                return Unauthorized();
            }

            // Rechercher le favori correspondant à cet utilisateur et ce livre/magazine.
            var favorite = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BookMagazineId == bookMagazineId);

            // Si le favori n'est pas trouvé, retourner une réponse 404 (Not Found).
            if (favorite == null)
            {
                return NotFound(new { message = "The specified book or magazine is not in your favorites." });
            }

            // Supprimer le favori de la base de données.
            _context.UserFavorites.Remove(favorite);
            await _context.SaveChangesAsync();

            // Retourner un message de succès avec un statut 200 (OK).
            return Ok(new { message = "Book/Magazine removed from favorites successfully!" });
        }

    }
}
