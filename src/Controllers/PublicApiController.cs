using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[Route("api/public")]
[ApiController]
public class PublicApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PublicApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // *** Endpoint pour obtenir les livres ou magazines les plus populaires ***
    [HttpGet("top-books-magazines")]
    public async Task<IActionResult> GetTopBooksMagazines([FromQuery] int count = 10)
    {
        var topBooksMagazines = await _context.BooksMagazines
            .OrderByDescending(b => b.ViewCount)
            .Take(count)
            .Select(b => new {
                b.Id,
                b.Title,
                Author = b.Author.Name,
                b.Description,
                b.CoverImagePath,
                b.ViewCount
            })
            .ToListAsync();

        return Ok(topBooksMagazines);
    }

    // *** Endpoint pour obtenir les statistiques générales de l'API ***
    [HttpGet("stats")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalBooksMagazines = await _context.BooksMagazines.CountAsync();
        var totalDownloads = await _context.BooksMagazines.SumAsync(b => b.DownloadCount);
        var totalViews = await _context.BooksMagazines.SumAsync(b => b.ViewCount);

        return Ok(new
        {
            TotalBooksMagazines = totalBooksMagazines,
            TotalDownloads = totalDownloads,
            TotalViews = totalViews
        });
    }

    // *** Endpoint pour obtenir les commentaires récents sur les livres ou magazines ***
    [HttpGet("recent-comments")]
    public async Task<IActionResult> GetRecentComments([FromQuery] int count = 10)
    {
        var recentComments = await _context.Comments
            .OrderByDescending(c => c.CommentDate)
            .Take(count)
            .Select(c => new {
                c.Content,
                c.CommentDate,
                User = c.User.UserName,
                BookMagazineTitle = c.BookMagazine.Title
            })
            .ToListAsync();

        return Ok(recentComments);
    }
}
