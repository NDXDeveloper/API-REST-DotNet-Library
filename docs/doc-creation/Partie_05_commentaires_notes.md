 
# Tutoriel : Ajout des fonctionnalités de Notes et Commentaires à votre API REST .NET

Dans ce tutoriel, nous allons ajouter les fonctionnalités de **Notes** et **Commentaires** à une API REST .NET qui permet aux utilisateurs de stocker, consulter et télécharger des livres ou des magazines. Ces fonctionnalités permettront aux utilisateurs d'évaluer les livres/magazines et de partager leurs avis, créant ainsi une expérience communautaire enrichie.

## Prérequis

- Connaissances de base en C# et ASP.NET Core.
- Un projet d'API REST .NET déjà configuré avec Entity Framework Core et une base de données (par exemple, SQL Server ou MySQL).
- L'authentification des utilisateurs mise en place (par exemple, avec Identity).

## Objectifs du tutoriel

- Créer des modèles pour gérer les notes et les commentaires.
- Mettre à jour le contexte de base de données pour inclure les nouveaux modèles.
- Ajouter des migrations et mettre à jour la base de données.
- Créer des routes d'API pour permettre aux utilisateurs de soumettre des notes et des commentaires.
- Gérer l'affichage des notes moyennes et des commentaires associés aux livres/magazines.
- S'assurer que seules les personnes authentifiées peuvent interagir avec ces fonctionnalités.

## Étape 1 : Création des modèles de données

### 1.1 Modèle `Rating`

Le modèle `Rating` permettra de stocker les notes que chaque utilisateur attribue à un livre ou magazine.

**Créer un fichier `Rating.cs` dans le dossier `Models` :**

```csharp
using System;
using System.ComponentModel.DataAnnotations;

public class Rating
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookMagazineId { get; set; }

    [Required]
    public string UserId { get; set; }  // ID de l'utilisateur

    [Required]
    [Range(1, 5)]
    public int RatingValue { get; set; }  // La note (1 à 5 étoiles)

    public DateTime RatingDate { get; set; } = DateTime.Now;

    // Relations (facultatives)
    public BookMagazine BookMagazine { get; set; }
    public ApplicationUser User { get; set; }
}
```

### 1.2 Modèle `Comment`

Le modèle `Comment` permettra de gérer les commentaires et les réponses aux commentaires pour chaque livre ou magazine.

**Créer un fichier `Comment.cs` dans le dossier `Models` :**

```csharp
using System;
using System.ComponentModel.DataAnnotations;

public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookMagazineId { get; set; }

    [Required]
    public string UserId { get; set; }  // ID de l'utilisateur

    [Required]
    public string Content { get; set; }  // Contenu du commentaire

    public DateTime CommentDate { get; set; } = DateTime.Now;

    public int? ParentCommentId { get; set; }  // Pour les réponses (facultatif)

    // Relations (facultatives)
    public BookMagazine BookMagazine { get; set; }
    public ApplicationUser User { get; set; }
    public Comment ParentComment { get; set; }
}
```

## Étape 2 : Mise à jour du contexte de base de données

Pour que Entity Framework Core puisse créer les tables correspondantes, nous devons ajouter les nouveaux modèles au contexte de base de données.

**Ouvrez `ApplicationDbContext.cs` et ajoutez les `DbSet` suivants :**

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // Constructeur existant...

    public DbSet<BookMagazine> BooksMagazines { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Category> Categories { get; set; }

    // Ajouter les DbSet pour Rating et Comment
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<Comment> Comments { get; set; }
}
```

## Étape 3 : Ajout de migrations et mise à jour de la base de données

Après avoir ajouté les nouveaux modèles, nous devons créer une migration pour mettre à jour la base de données.

**Dans la console du gestionnaire de packages (ou via la ligne de commande), exécutez :**

```bash
dotnet ef migrations add AddRatingsAndComments
dotnet ef database update
```

Ces commandes vont créer les tables `Ratings` et `Comments` dans la base de données.

## Étape 4 : Ajout des fonctionnalités de notation

### 4.1 Modification du modèle `BookMagazine`

Nous allons ajouter un champ `AverageRating` pour stocker la note moyenne de chaque livre ou magazine.

**Ouvrez `BookMagazine.cs` et ajoutez la propriété suivante :**

```csharp
public class BookMagazine
{
    // Autres propriétés existantes...

    public double AverageRating { get; set; }  // Note moyenne du livre/magazine
}
```

**N'oubliez pas de créer une migration pour cette modification :**

```bash
dotnet ef migrations add AddAverageRatingToBookMagazine
dotnet ef database update
```

### 4.2 Création des routes d'API pour les évaluations

Nous allons permettre aux utilisateurs d'ajouter ou de mettre à jour leur note pour un livre ou magazine, et de récupérer les notes.

**Ouvrez `BookMagazineController.cs` et ajoutez les méthodes suivantes :**

#### Soumettre une note

```csharp
[HttpPost("{bookMagazineId}/rate")]
[Authorize]
public async Task<IActionResult> RateBookMagazine(int bookMagazineId, [FromBody] int ratingValue)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (ratingValue < 1 || ratingValue > 5)
        return BadRequest("La note doit être comprise entre 1 et 5.");

    // Vérifier si l'utilisateur a déjà noté ce livre/magazine
    var existingRating = await _context.Ratings
        .FirstOrDefaultAsync(r => r.BookMagazineId == bookMagazineId && r.UserId == userId);

    if (existingRating != null)
    {
        // Mettre à jour la note existante
        existingRating.RatingValue = ratingValue;
        existingRating.RatingDate = DateTime.Now;
    }
    else
    {
        // Ajouter une nouvelle note
        var rating = new Rating
        {
            BookMagazineId = bookMagazineId,
            UserId = userId,
            RatingValue = ratingValue
        };
        _context.Ratings.Add(rating);
    }

    await _context.SaveChangesAsync();

    // Recalculer la note moyenne
    var averageRating = await _context.Ratings
        .Where(r => r.BookMagazineId == bookMagazineId)
        .AverageAsync(r => r.RatingValue);

    // Mettre à jour la note moyenne dans BookMagazine
    var bookMagazine = await _context.BooksMagazines.FindAsync(bookMagazineId);
    bookMagazine.AverageRating = averageRating;
    _context.BooksMagazines.Update(bookMagazine);
    await _context.SaveChangesAsync();

    return Ok(new { Message = "Note soumise avec succès", AverageRating = averageRating });
}
```

#### Récupérer les notes

```csharp
[HttpGet("{id}/ratings")]
public async Task<IActionResult> GetRatings(int id)
{
    var ratings = await _context.Ratings
        .Where(r => r.BookMagazineId == id)
        .Select(r => new {
            r.UserId,
            r.RatingValue,
            r.RatingDate
        })
        .ToListAsync();

    var averageRating = ratings.Any() ? ratings.Average(r => r.RatingValue) : 0;

    return Ok(new { Ratings = ratings, AverageRating = averageRating });
}
```

## Étape 5 : Ajout des fonctionnalités de commentaires

### 5.1 Création des routes d'API pour les commentaires

Nous allons permettre aux utilisateurs d'ajouter des commentaires et de répondre à des commentaires existants.

**Toujours dans `BookMagazineController.cs`, ajoutez les méthodes suivantes :**

#### Ajouter un commentaire

```csharp
[HttpPost("{bookMagazineId}/comment")]
[Authorize]
public async Task<IActionResult> AddComment(int bookMagazineId, [FromBody] string content)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrWhiteSpace(content))
        return BadRequest("Le contenu du commentaire ne peut pas être vide.");

    var comment = new Comment
    {
        BookMagazineId = bookMagazineId,
        UserId = userId,
        Content = content
    };

    _context.Comments.Add(comment);
    await _context.SaveChangesAsync();

    return Ok(new { Message = "Commentaire ajouté avec succès" });
}
```

#### Répondre à un commentaire

```csharp
[HttpPost("{bookMagazineId}/comment/{commentId}/reply")]
[Authorize]
public async Task<IActionResult> ReplyToComment(int bookMagazineId, int commentId, [FromBody] string content)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrWhiteSpace(content))
        return BadRequest("Le contenu de la réponse ne peut pas être vide.");

    // Vérifier si le commentaire parent existe
    var parentComment = await _context.Comments.FindAsync(commentId);
    if (parentComment == null)
        return NotFound("Le commentaire auquel vous essayez de répondre n'existe pas.");

    var reply = new Comment
    {
        BookMagazineId = bookMagazineId,
        UserId = userId,
        Content = content,
        ParentCommentId = commentId
    };

    _context.Comments.Add(reply);
    await _context.SaveChangesAsync();

    return Ok(new { Message = "Réponse ajoutée avec succès" });
}
```

#### Récupérer les commentaires

```csharp
[HttpGet("{bookMagazineId}/comments")]
public async Task<IActionResult> GetComments(int bookMagazineId)
{
    var comments = await _context.Comments
        .Where(c => c.BookMagazineId == bookMagazineId && c.ParentCommentId == null)
        .OrderByDescending(c => c.CommentDate)
        .Select(c => new {
            c.Id,
            c.Content,
            c.CommentDate,
            c.UserId,
            Replies = _context.Comments
                .Where(r => r.ParentCommentId == c.Id)
                .OrderBy(r => r.CommentDate)
                .Select(r => new {
                    r.Id,
                    r.Content,
                    r.CommentDate,
                    r.UserId
                })
                .ToList()
        })
        .ToListAsync();

    return Ok(comments);
}
```

## Étape 6 : Tests et validations

### 6.1 Vérifier les permissions

Assurez-vous que seules les personnes authentifiées peuvent soumettre des notes et des commentaires. L'attribut `[Authorize]` dans les méthodes du contrôleur s'en charge.

### 6.2 Tester les routes d'API

Utilisez un outil comme **Postman** ou **Swagger** pour tester les routes :

- **Soumettre une note :**

  - Méthode : POST
  - URL : `/api/BookMagazine/{bookMagazineId}/rate`
  - Corps de la requête : un entier entre 1 et 5

- **Ajouter un commentaire :**

  - Méthode : POST
  - URL : `/api/BookMagazine/{bookMagazineId}/comment`
  - Corps de la requête : une chaîne de caractères (le contenu du commentaire)

- **Répondre à un commentaire :**

  - Méthode : POST
  - URL : `/api/BookMagazine/{bookMagazineId}/comment/{commentId}/reply`
  - Corps de la requête : une chaîne de caractères (le contenu de la réponse)

- **Récupérer les notes :**

  - Méthode : GET
  - URL : `/api/BookMagazine/{id}/ratings`

- **Récupérer les commentaires :**

  - Méthode : GET
  - URL : `/api/BookMagazine/{bookMagazineId}/comments`

### 6.3 Vérifier le recalcul de la note moyenne

Après avoir soumis plusieurs notes pour un livre ou magazine, vérifiez que la note moyenne est correctement mise à jour dans la propriété `AverageRating` du modèle `BookMagazine`.

## Étape 7 : Améliorations potentielles

- **Pagination des commentaires :** Si un livre ou magazine reçoit de nombreux commentaires, il peut être utile d'implémenter la pagination pour améliorer les performances.
- **Modération :** Ajouter des fonctionnalités pour que les administrateurs puissent supprimer des commentaires inappropriés.
- **Notifications :** Notifier les utilisateurs lorsque quelqu'un répond à leur commentaire.
- **Votes sur les commentaires :** Permettre aux utilisateurs de voter pour les commentaires, mettant en avant les plus utiles.

## Conclusion

Vous avez maintenant ajouté avec succès les fonctionnalités de notes et commentaires à votre API REST .NET. Ces ajouts permettent d'améliorer l'interaction des utilisateurs avec le contenu et favorisent une communauté active autour des livres et magazines proposés.
