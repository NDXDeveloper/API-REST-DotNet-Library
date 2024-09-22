using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class BookMagazineController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BookMagazineController(ApplicationDbContext context)
    {
        _context = context;
    }

    // *** Ajouter un livre ou magazine avec un auteur et une catégorie ***
    [HttpPost("add")]
    [Authorize]
    public async Task<IActionResult> AddBookMagazine([FromForm] BookMagazineModel model)
    {
        // Vérifier si l'auteur existe, sinon le créer
        var author = _context.Authors.FirstOrDefault(a => a.Name == model.Author);
        if (author == null)
        {
            author = new Author { Name = model.Author };
            _context.Authors.Add(author);
            await _context.SaveChangesAsync();
        }

        // Vérifier si la catégorie existe, sinon la créer
        var category = _context.Categories.FirstOrDefault(c => c.Name == model.Category);
        if (category == null)
        {
            category = new Category { Name = model.Category };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        // Enregistrement du fichier du livre/magazine
        var filePath = Path.Combine("wwwroot/files", model.File.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await model.File.CopyToAsync(stream);
        }

        // Enregistrement de l'image de couverture si elle est présente
        string coverImagePath = null;
        if (model.CoverImage != null && model.CoverImage.Length > 0)
        {
            coverImagePath = Path.Combine("wwwroot/images/covers", model.CoverImage.FileName);
            using (var coverStream = new FileStream(coverImagePath, FileMode.Create))
            {
                await model.CoverImage.CopyToAsync(coverStream);
            }
            coverImagePath = $"/images/covers/{model.CoverImage.FileName}";
        }

        // Création de l'objet BookMagazine
        var bookMagazine = new BookMagazine
        {
            Title = model.Title,
            AuthorId = author.Id,  // Association avec l'auteur
            CategoryId = category.Id,  // Association avec la catégorie
            Description = model.Description,
            Tags = model.Tags,
            FilePath = $"/files/{model.File.FileName}",
            CoverImagePath = coverImagePath
        };

        // Enregistrement dans la base de données
        _context.BooksMagazines.Add(bookMagazine);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Book or magazine added successfully!", CoverImageUrl = coverImagePath });
    }

    // *** Obtenir la liste des livres ou magazines ***
    [HttpGet("list")]
    public IActionResult GetBooksMagazines()
    {
        var booksMagazines = _context.BooksMagazines
            .Select(b => new 
            {
                b.Id,
                b.Title,
                Author = b.Author.Name,
                Category = b.Category.Name,
                b.CoverImagePath,
                b.UploadDate
            })
            .ToList();

        return Ok(booksMagazines);
    }

    // *** Obtenir les détails d'un livre ou magazine spécifique ***
    [HttpGet("{id}")]
    public IActionResult GetBookMagazine(int id)
    {
        var bookMagazine = _context.BooksMagazines
            .Where(b => b.Id == id)
            .Select(b => new 
            {
                b.Id,
                b.Title,
                b.Description,
                Author = b.Author.Name,
                Category = b.Category.Name,
                b.Tags,
                b.CoverImagePath,
                b.FilePath,
                b.UploadDate
            })
            .FirstOrDefault();

        if (bookMagazine == null)
            return NotFound();

        return Ok(bookMagazine);
    }

    // *** Télécharger le fichier d'un livre ou magazine ***
    [HttpGet("download/{id}")]
    public IActionResult DownloadBookMagazine(int id)
    {
        var bookMagazine = _context.BooksMagazines.FirstOrDefault(b => b.Id == id);
        if (bookMagazine == null) return NotFound();

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", bookMagazine.FilePath.TrimStart('/'));
        if (!System.IO.File.Exists(filePath))
            return NotFound("File not found on server.");

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var fileName = Path.GetFileName(filePath);

        return File(fileBytes, "application/octet-stream", fileName);
    }

    // *** Mettre à jour un livre ou magazine par l'administrateur ***
    [HttpPut("update/{id}")]
    [Authorize(Roles = "Admin")]  // Seuls les administrateurs peuvent modifier
    public async Task<IActionResult> UpdateBookMagazine(int id, [FromForm] BookMagazineModel model)
    {
        var bookMagazine = _context.BooksMagazines.FirstOrDefault(b => b.Id == id);
        if (bookMagazine == null) return NotFound();

        // Mise à jour des propriétés du livre/magazine
        bookMagazine.Title = model.Title;
        bookMagazine.Description = model.Description;
        bookMagazine.Tags = model.Tags;

        // Gestion de l'auteur et de la catégorie
        var author = _context.Authors.FirstOrDefault(a => a.Name == model.Author);
        if (author == null)
        {
            author = new Author { Name = model.Author };
            _context.Authors.Add(author);
            await _context.SaveChangesAsync();
        }
        bookMagazine.AuthorId = author.Id;

        var category = _context.Categories.FirstOrDefault(c => c.Name == model.Category);
        if (category == null)
        {
            category = new Category { Name = model.Category };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }
        bookMagazine.CategoryId = category.Id;

        // Gestion du fichier (facultatif)
        if (model.File != null)
        {
            var filePath = Path.Combine("wwwroot/files", model.File.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }
            bookMagazine.FilePath = $"/files/{model.File.FileName}";
        }

        // Gestion de l'image de couverture (facultatif)
        if (model.CoverImage != null)
        {
            var coverImagePath = Path.Combine("wwwroot/images/covers", model.CoverImage.FileName);
            using (var coverStream = new FileStream(coverImagePath, FileMode.Create))
            {
                await model.CoverImage.CopyToAsync(coverStream);
            }
            bookMagazine.CoverImagePath = $"/images/covers/{model.CoverImage.FileName}";
        }

        _context.BooksMagazines.Update(bookMagazine);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Book or magazine updated successfully!" });
    }

    // *** Supprimer un livre ou magazine par l'administrateur ***
    [HttpDelete("delete/{id}")]
    [Authorize(Roles = "Admin")]  // Seuls les administrateurs peuvent supprimer
    public async Task<IActionResult> DeleteBookMagazine(int id)
    {
        var bookMagazine = _context.BooksMagazines.FirstOrDefault(b => b.Id == id);
        if (bookMagazine == null) return NotFound();

        // Suppression des fichiers associés (livre/magazine et image de couverture)
        if (!string.IsNullOrEmpty(bookMagazine.FilePath))
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", bookMagazine.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        if (!string.IsNullOrEmpty(bookMagazine.CoverImagePath))
        {
            var coverImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", bookMagazine.CoverImagePath.TrimStart('/'));
            if (System.IO.File.Exists(coverImagePath))
            {
                System.IO.File.Delete(coverImagePath);
            }
        }

        // Suppression du livre/magazine dans la base de données
        _context.BooksMagazines.Remove(bookMagazine);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Book or magazine deleted successfully!" });
    }
}
