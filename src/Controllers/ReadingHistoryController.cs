using Microsoft.AspNetCore.Authorization; // Nécessaire pour gérer l'authentification et l'autorisation des utilisateurs dans l'application via des attributs comme [Authorize].
using Microsoft.AspNetCore.Mvc; // Fournit les outils essentiels pour créer des contrôleurs API, gérer les routes HTTP et les actions telles que GET, POST, PUT, DELETE.
using Microsoft.EntityFrameworkCore; // Permet l'utilisation d'Entity Framework Core pour interagir avec la base de données et effectuer des opérations CRUD.
//using System.IdentityModel.Tokens.Jwt; // Commenté car non utilisé. Ce namespace est utile pour manipuler les JWT (JSON Web Tokens) directement si nécessaire.
using System.Security.Claims; // Utilisé pour extraire des informations de l'utilisateur connecté (via les claims, comme l'identifiant d'utilisateur) à partir de son token d'authentification.


namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Nécessite que l'utilisateur soit authentifié pour accéder à toutes les actions de ce contrôleur.
    public class ReadingHistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Dépendance injectée pour interagir avec la base de données via le contexte ApplicationDbContext.

        public ReadingHistoryController(ApplicationDbContext context) // Constructeur permettant d'injecter le contexte de base de données dans le contrôleur via l'injection de dépendances.
        {
            _context = context;
        }

        // // Ajouter un livre/magazine à l'historique de lecture
        // [HttpPost("update-history/{bookMagazineId}")]
        // public async Task<IActionResult> UpdateReadingHistory(int bookMagazineId)
        // {
        //     // Récupérer l'identifiant de l'utilisateur à partir de la claim "sub"
        //     // var userId = User.FindFirst("sub")?.Value;
        //     // var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        //     var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //     if (userId == null) return Unauthorized();

        //     var readingHistory = await _context.UserReadingHistory
        //         .FirstOrDefaultAsync(rh => rh.UserId == userId && rh.BookMagazineId == bookMagazineId);

        //     if (readingHistory == null)
        //     {
        //         readingHistory = new UserReadingHistory
        //         {
        //             UserId = userId,
        //             BookMagazineId = bookMagazineId,
        //             LastReadDate = DateTime.UtcNow
        //         };
        //         _context.UserReadingHistory.Add(readingHistory);
        //     }
        //     else
        //     {
        //         readingHistory.LastReadDate = DateTime.UtcNow;
        //         _context.UserReadingHistory.Update(readingHistory);
        //     }

        //     await _context.SaveChangesAsync();

        //     return Ok(new { Message = "Reading history updated successfully!" });
        // }

        // Ajouter un livre/magazine à l'historique de lecture
        [HttpPost("update-history/{bookMagazineId}")]
        public async Task<IActionResult> UpdateReadingHistory(int bookMagazineId)
        {
            // Récupérer l'identifiant de l'utilisateur à partir des Claims dans le token d'authentification.
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Si l'utilisateur n'est pas authentifié, retourner un statut 401 (Unauthorized).
            if (userId == null)
            {
                return Unauthorized();
            }

            // Vérifier si le livre ou magazine existe dans la base de données avant de l'ajouter à l'historique de lecture.
            var bookMagazine = await _context.BooksMagazines.FindAsync(bookMagazineId);
            if (bookMagazine == null)
            {
                // Si le livre/magazine n'existe pas, retourner une réponse 404 (Not Found).
                return NotFound(new { message = $"Book or magazine with ID {bookMagazineId} not found." });
            }

            // Vérifier si cet utilisateur a déjà une entrée d'historique pour ce livre/magazine.
            var readingHistory = await _context.UserReadingHistory
                .FirstOrDefaultAsync(rh => rh.UserId == userId && rh.BookMagazineId == bookMagazineId);

            if (readingHistory == null)
            {
                // Si aucune entrée n'existe, créer une nouvelle entrée pour cet utilisateur dans l'historique de lecture.
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
                // Si une entrée existe déjà, mettre à jour la date de dernière lecture.
                readingHistory.LastReadDate = DateTime.UtcNow;
                _context.UserReadingHistory.Update(readingHistory);
            }

            // Sauvegarder les modifications dans la base de données.
            await _context.SaveChangesAsync();

            // Retourner une réponse avec un message de succès.
            return Ok(new { message = "Reading history updated successfully!" });
        }

        // // Récupérer l'historique de lecture de l'utilisateur
        // [HttpGet("reading-history")]
        // public async Task<IActionResult> GetReadingHistory()
        // {
        //     //var userId = User.FindFirst("id")?.Value;
        //     var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //     if (userId == null) return Unauthorized();

        //     var history = await _context.UserReadingHistory
        //         .Where(rh => rh.UserId == userId)
        //         .Include(rh => rh.BookMagazine) // Inclure les détails des livres/magazines
        //         .OrderByDescending(rh => rh.LastReadDate)
        //         .ToListAsync();

        //     return Ok(history);
        // }

        // Récupérer l'historique de lecture de l'utilisateur
        [HttpGet("reading-history")]
        public async Task<IActionResult> GetReadingHistory()
        {
            // Récupérer l'identifiant de l'utilisateur à partir des Claims dans le token d'authentification.
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Si l'utilisateur n'est pas authentifié, retourner un statut 401 (Unauthorized).
            if (userId == null)
            {
                return Unauthorized();
            }

            // Récupérer l'historique de lecture de l'utilisateur depuis la base de données, y compris les détails des livres/magazines et de leurs auteurs.
            var history = await _context.UserReadingHistory
                .Where(rh => rh.UserId == userId)
                .Include(rh => rh.BookMagazine) // Inclure les informations du livre ou magazine associé.
                .ThenInclude(b => b.Author) // Inclure également l'auteur du livre/magazine.
                .OrderByDescending(rh => rh.LastReadDate) // Trier l'historique par date de dernière lecture (le plus récent en premier).
                .ToListAsync();

            // Si l'utilisateur n'a aucun historique de lecture, retourner une réponse 404 (Not Found).
            if (history == null || !history.Any())
            {
                return NotFound(new { message = "No reading history found for the user." });
            }

            // Créer une réponse personnalisée en s'assurant que les informations sur les livres/magazines ne sont pas nulles.
            var response = history
                .Where(rh => rh.BookMagazine != null) // Vérifier que chaque livre/magazine n'est pas null.
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

            // Retourner l'historique de lecture de l'utilisateur avec un statut 200 (OK).
            return Ok(response);
        }


    }
}
