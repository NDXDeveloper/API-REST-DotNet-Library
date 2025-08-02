 

## Tutoriel pour la Partie 6 - Statistiques et rapports

Dans cette section, nous allons :

1. Suivre les téléchargements de chaque livre ou magazine.
2. Collecter des statistiques d’utilisation.
3. Générer des rapports d’activité pour obtenir des insights sur les contenus populaires et l’engagement des utilisateurs.

### Étapes pour implémenter les statistiques et rapports

### 1. **Ajout de champs pour les statistiques dans le modèle `BookMagazine`**

Nous allons d’abord mettre à jour le modèle `BookMagazine` pour ajouter des compteurs de vues (`ViewCount`) et de téléchargements (`DownloadCount`). Ces compteurs enregistrent les actions des utilisateurs pour chaque contenu.

#### Mise à jour du modèle `BookMagazine`

```csharp
public class BookMagazine
{
    // Propriétés existantes
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    // Nouvelles propriétés pour les statistiques
    public int DownloadCount { get; set; }  // Suivi des téléchargements
    public int ViewCount { get; set; }      // Suivi des vues
}
```

Ensuite, appliquez cette modification dans la base de données :

```bash
dotnet ef migrations add AddViewAndDownloadCountToBookMagazine
dotnet ef database update
```

### 2. **Incrémentation du compteur de téléchargements**

Mettez à jour la méthode `DownloadBookMagazine` pour incrémenter `DownloadCount` chaque fois qu’un utilisateur télécharge un livre ou un magazine.

#### Code pour incrémenter `DownloadCount`

```csharp
[HttpGet("download/{id}")]
public async Task<IActionResult> DownloadBookMagazine(int id)
{
    var bookMagazine = await _context.BooksMagazines.FindAsync(id);
    if (bookMagazine == null)
        return NotFound();

    // Incrémenter le compteur de téléchargements
    bookMagazine.DownloadCount++;
    _context.BooksMagazines.Update(bookMagazine);
    await _context.SaveChangesAsync();

    // Code pour gérer le téléchargement du fichier
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", bookMagazine.FilePath.TrimStart('/'));
    if (!System.IO.File.Exists(filePath))
        return NotFound("File not found on server.");

    var fileBytes = System.IO.File.ReadAllBytes(filePath);
    var fileName = Path.GetFileName(filePath);

    return File(fileBytes, "application/octet-stream", fileName);
}
```

### 3. **Récupération des statistiques d’utilisation d’un livre ou magazine**

Ajoutez une route API pour afficher les statistiques d’un livre ou magazine spécifique, notamment le nombre de vues, le nombre de téléchargements, la moyenne des notes, et le nombre de commentaires.

#### Code pour la méthode `GetBookMagazineStats`

```csharp
[HttpGet("{id}/stats")]
public IActionResult GetBookMagazineStats(int id)
{
    var bookMagazine = _context.BooksMagazines
        .Where(b => b.Id == id)
        .Select(b => new {
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

    return Ok(bookMagazine);
}
```

Cette méthode renvoie un objet contenant les statistiques d’utilisation du contenu, ce qui permet de visualiser l’engagement utilisateur.

### 4. **Création de rapports sur les contenus populaires**

Ajoutez une route API pour générer un rapport des livres et magazines les plus populaires en fonction du nombre de vues et de téléchargements.

#### Code pour la méthode `GetPopularBooksMagazines`

```csharp
[HttpGet("reports/popular")]
public IActionResult GetPopularBooksMagazines()
{
    var popularBooksMagazines = _context.BooksMagazines
        .OrderByDescending(b => b.ViewCount + b.DownloadCount) // Trier par popularité
        .Select(b => new {
            b.Title,
            b.ViewCount,
            b.DownloadCount,
            AverageRating = _context.Ratings
                .Where(r => r.BookMagazineId == b.Id)
                .Average(r => (double?)r.RatingValue) ?? 0
        })
        .Take(10) // Limiter à 10 résultats
        .ToList();

    return Ok(popularBooksMagazines);
}
```

### 5. **Génération d’un rapport d’activité utilisateur**

Ajoutez une route API pour générer un rapport d’activité par utilisateur, montrant l’interaction de chaque utilisateur avec les livres et magazines.

#### Code pour le rapport d’activité utilisateur `GetUserActivityReport`

```csharp
[HttpGet("reports/user-activity")]
[Authorize(Roles = "Admin")]
public IActionResult GetUserActivityReport()
{
    var userActivity = _context.Users
        .Select(u => new {
            u.Id,
            u.UserName,
            FavoriteCount = _context.UserFavorites.Count(f => f.UserId == u.Id),
            CommentCount = _context.Comments.Count(c => c.UserId == u.Id),
            RatingCount = _context.Ratings.Count(r => r.UserId == u.Id),
            TotalDownloads = _context.BooksMagazines
                .Where(b => _context.UserReadingHistory
                    .Where(ur => ur.UserId == u.Id)
                    .Select(ur => ur.BookMagazineId)
                    .Contains(b.Id))
                .Sum(b => b.DownloadCount)
        })
        .ToList();

    return Ok(userActivity);
}
```

### 6. **Ajouter les statistiques aux réponses de consultation des livres et magazines**

Pour afficher les statistiques d’un livre ou magazine dans les réponses de consultation, modifiez la méthode de consultation pour inclure les compteurs de vues, de téléchargements, et la note moyenne.

#### Exemple de réponse avec statistiques pour `GetBookMagazine`

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetBookMagazine(int id)
{
    var bookMagazine = await _context.BooksMagazines
        .Include(b => b.Author)       // Inclure l'entité 'Author'
        .Include(b => b.Category)     // Inclure l'entité 'Category'
        .FirstOrDefaultAsync(b => b.Id == id);

    if (bookMagazine == null)
        return NotFound();

    // Vérifier que l'entité 'Author' et 'Category' ne sont pas nulles
    if (bookMagazine.Author == null || bookMagazine.Category == null)
        return StatusCode(500, "Invalid data: Author or Category not found.");  // Gérer les cas de données incorrectes

    // Incrémenter le compteur de vues
    bookMagazine.ViewCount++;
    _context.BooksMagazines.Update(bookMagazine);
    await _context.SaveChangesAsync();

    // Calculer la note moyenne et le nombre de commentaires
    var averageRating = await _context.Ratings
        .Where(r => r.BookMagazineId == id)
        .AverageAsync(r => (double?)r.RatingValue) ?? 0;

    var commentCount = await _context.Comments
        .CountAsync(c => c.BookMagazineId == id);

    return Ok(new {
        bookMagazine.Id,
        bookMagazine.Title,
        bookMagazine.Description,
        Author = bookMagazine.Author.Name,
        Category = bookMagazine.Category.Name,
        bookMagazine.Tags,
        bookMagazine.CoverImagePath,
        bookMagazine.FilePath,
        bookMagazine.UploadDate,
        bookMagazine.ViewCount,      // Renvoyer le nombre de vues
        bookMagazine.DownloadCount,   // Renvoyer le nombre de téléchargements
        AverageRating = averageRating,  // Renvoyer la note moyenne
        CommentCount = commentCount     // Renvoyer le nombre de commentaires
    });
}

```

### 7. **Tester les fonctionnalités de statistiques et de rapports**

#### Suivi des téléchargements
   - **Route :** `GET /api/BookMagazine/download/{id}`
   - **Objectif :** Télécharger un livre ou magazine, et vérifier que le `DownloadCount` s’incrémente à chaque téléchargement.

#### Récupération des statistiques d’un livre ou magazine
   - **Route :** `GET /api/BookMagazine/{id}/stats`
   - **Objectif :** Récupérer les statistiques de vues, téléchargements, notes, et commentaires pour un livre spécifique.

#### Contenus populaires
   - **Route :** `GET /api/BookMagazine/reports/popular`
   - **Objectif :** Obtenir les 10 livres ou magazines les plus populaires (en fonction des vues et des téléchargements).

#### Rapport d’activité utilisateur
   - **Route :** `GET /api/BookMagazine/reports/user-activity`
   - **Objectif :** Voir le rapport d’activité pour chaque utilisateur, incluant le nombre de favoris, commentaires, notes, et téléchargements.

---

En suivant ces étapes, nous avons maintenant mis en place une solution de **suivi des téléchargements**, **statistiques d’utilisation**, et **rapports d’activité** pour l'API, offrant une vue d'ensemble de l’engagement des utilisateurs et des contenus populaires. Ces informations peuvent ensuite être utilisées pour des décisions stratégiques, améliorer l’expérience utilisateur, et adapter les contenus.
