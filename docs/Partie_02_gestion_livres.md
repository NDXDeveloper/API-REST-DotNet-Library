
### **Gestion des livres et magazines :**
   - **Ajout de contenu** : Permettre aux utilisateurs d'ajouter des livres ou des magazines (titre, auteur, description, catégorie, fichier PDF ou EPUB, etc.).
   - **Consultation du contenu** : Les utilisateurs peuvent rechercher et consulter les détails des livres ou magazines disponibles.
   - **Téléchargement** : Option de télécharger le fichier du livre ou du magazine en différents formats (PDF, EPUB).
   - **Catégories et tags** : Organisation des contenus par catégories ou tags pour faciliter la recherche.




Pour implémenter la **partie 2 - Gestion des livres et magazines** du projet d'API REST .NET, nous allons ajouter les fonctionnalités suivantes :

1. **Création des tables `Author` et `Category` pour stocker les auteurs et les catégories séparément.**
2. **Ajout des relations entre `BookMagazine` et `Author` ainsi qu'entre `BookMagazine` et `Category` (les deux via des clés étrangères).**
3. **Mise à jour du contrôleur pour vérifier si un auteur ou une catégorie existe déjà avant d'ajouter un nouveau livre ou magazine, et créer les entrées si nécessaire.**

### 1. **Modèles `Author` et `Category`**
Nous allons créer des modèles pour représenter l'auteur et la catégorie du livre.

#### Modèle `Author.cs` :
```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Author
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    // Relation avec BookMagazine (un auteur peut avoir plusieurs livres/magazines)
    public ICollection<BookMagazine> BooksMagazines { get; set; }
}
```

#### Modèle `Category.cs` :
```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    // Relation avec BookMagazine (une catégorie peut avoir plusieurs livres/magazines)
    public ICollection<BookMagazine> BooksMagazines { get; set; }
}
```


### 2. **Modèles pour les livres et magazines :**
Nous allons créer des modèles pour représenter les livres et magazines dans la base de données, avec des informations comme le titre, l'auteur, la description, la catégorie, le fichier (PDF/EPUB), etc.

#### Création du modèle `BookMagazine.cs` :
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BookMagazine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public int AuthorId { get; set; }  // Foreign key vers l'auteur
    [ForeignKey("AuthorId")]
    public Author Author { get; set; }

    public string Description { get; set; }

    [Required]
    public int CategoryId { get; set; }  // Foreign key vers la catégorie
    [ForeignKey("CategoryId")]
    public Category Category { get; set; }

    public string Tags { get; set; }

    [Required]
    public string FilePath { get; set; }

    public string CoverImagePath { get; set; }

    public DateTime UploadDate { get; set; } = DateTime.Now;
}

```

### 3. **Mise à jour du contexte de base de données :**

Ensuite, nous devons enregistrer ce nouveau modèle dans `ApplicationDbContext.cs`.

```csharp
// Importation du namespace nécessaire pour utiliser Identity avec Entity Framework Core.
// Identity permet la gestion des utilisateurs, rôles, connexions, etc.
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
// Importation du namespace pour Entity Framework Core qui permet l'accès aux bases de données.
using Microsoft.EntityFrameworkCore;

// Déclaration de la classe ApplicationDbContext qui hérite de IdentityDbContext.
// IdentityDbContext est une classe spéciale fournie par ASP.NET Core Identity qui
// étend DbContext (le contexte de base de données d'Entity Framework) et inclut
// toutes les entités nécessaires pour la gestion des utilisateurs, rôles, et autres
// fonctionnalités d'ASP.NET Core Identity. Nous utilisons ApplicationUser comme
// modèle utilisateur personnalisé.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // Constructeur de la classe ApplicationDbContext qui appelle le constructeur de la classe parente (base).
    // Il prend en paramètre un DbContextOptions, qui contient les informations nécessaires pour
    // configurer le contexte, comme la chaîne de connexion à la base de données.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        // Le corps du constructeur est vide ici, car toute la logique de configuration
        // est gérée par la classe de base (IdentityDbContext) et les options passées.
    }
    public DbSet<BookMagazine> BooksMagazines { get; set; }  // Ajout de la table pour les livres et magazines
    public DbSet<Author> Authors { get; set; }  // Ajout de la table Author
    public DbSet<Category> Categories { get; set; }  // Ajout de la table Category

}

```

### 4. **Création d'un contrôleur pour gérer les livres et magazines :**

Créons un contrôleur API pour ajouter, consulter, et télécharger des livres ou magazines.

#### `BookMagazineController.cs` :
```csharp
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
```

### 5. **Modèle pour l'upload du livre/magazine :**
Ajoutons un modèle pour gérer les uploads de fichiers dans le contrôleur.

#### `BookMagazineModel.cs` :
```csharp
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

public class BookMagazineModel
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string Author { get; set; }  // L'auteur est désormais un champ texte

    public string Description { get; set; }

    [Required]
    public string Category { get; set; }  // La catégorie est désormais un champ texte

    public string Tags { get; set; }

    [Required]
    public IFormFile File { get; set; }

    public IFormFile CoverImage { get; set; }  // Optionnel, image de couverture
}
```

### 6. **Ajout de migration et mise à jour de la base de données :**
Il est maintenant nécessaire de créer une migration et d'appliquer cette dernière à la base de données.

```bash
dotnet ef migrations add AddBookMagazine
dotnet ef database update
```

### 6. **Tests des nouvelles routes pour un utilisateur :**

- **POST** `/api/BookMagazine/add` : pour ajouter un livre ou magazine: Lorsque vous ajoutez un livre ou magazine, le contrôleur vérifiera si l'auteur et la catégorie existent déjà. Si non, ils seront créés automatiquement.
- Les champs `Author` et `Category` sont maintenant des entités séparées, et `BookMagazine` contient deux clés étrangères pointant vers ces tables.
Avec cette implémentation, les auteurs et les catégories sont maintenant stockés dans des tables séparées. Lors de l'ajout d'un nouveau livre ou magazine, le système vérifie si l'auteur et la catégorie existent déjà et les crée si nécessaire. Cela permet de mieux organiser les données et d'éviter la duplication des informations.

- **GET** `/api/BookMagazine/list` : pour obtenir la liste des livres et magazines.
- **GET** `/api/BookMagazine/{id}` : pour obtenir les détails d'un livre ou magazine.
- **GET** `/api/BookMagazine/download/{id}` : pour télécharger un fichier.

Cela permettra de gérer les livres et magazines dans l'API.

### Explications des fonctionnalités ADMIN :

1. **Modification d'un livre ou magazine** :
   - Route : `PUT /api/BookMagazine/update/{id}`
   - Seuls les administrateurs peuvent modifier un livre ou un magazine.
   - Les champs comme le titre, l'auteur, la description, la catégorie, les tags, le fichier et l'image de couverture peuvent être mis à jour.
   - L'ID du livre ou du magazine est utilisé pour identifier l'élément à modifier.

2. **Suppression d'un livre ou magazine** :
   - Route : `DELETE /api/BookMagazine/delete/{id}`
   - Seuls les administrateurs peuvent supprimer un livre ou un magazine.
   - Lorsque l'élément est supprimé, les fichiers (le document et l'image de couverture) sont également supprimés du serveur.

### Tests des fonctionnalités ADMIN :

- **PUT** `/api/BookMagazine/update/{id}` : Pour modifier un livre ou magazine en tant qu'administrateur.
- **DELETE** `/api/BookMagazine/delete/{id}` : Pour supprimer un livre ou magazine en tant qu'administrateur.

### Remarques :
- Ces routes nécessitent que l'utilisateur ait le rôle **Admin** pour être accessibles.
- Les fichiers sur le serveur (livre/magazine et image de couverture) sont également supprimés lors de la suppression de l'élément.


