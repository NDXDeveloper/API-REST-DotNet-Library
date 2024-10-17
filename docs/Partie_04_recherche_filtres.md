
---

## Tutoriel : Implémenter une recherche avancée et un tri par popularité dans une API REST avec ASP.NET Core

Dans ce tutoriel, nous allons créer des fonctionnalités pour une API REST .NET permettant la gestion de livres et magazines. Nous allons notamment :

1. **Ajouter et gérer des compteurs de vues et de téléchargements** pour les livres/magazines.
2. **Implémenter une recherche avancée** avec filtres par auteur, catégorie, mots-clés, et date.
3. **Trier les résultats par popularité** (vues ou téléchargements).
4. **Proposer des suggestions** basées sur l'historique de lecture de l'utilisateur.

### Prérequis
- Avoir une API REST .NET Core fonctionnelle avec Entity Framework.
- Un modèle de base de données pour les livres/magazines (`BooksMagazines`), auteurs (`Author`), catégories (`Category`), et l'historique de lecture.

---

### 1. **Ajouter un compteur de vues et de téléchargements à la base de données**

Dans ce tutoriel, chaque livre/magazine aura deux compteurs :
- **ViewCount** : Nombre de vues.
- **DownloadCount** : Nombre de téléchargements.

#### Étape 1.1 : Modifier le modèle `BookMagazine`

Commencez par modifier le modèle `BookMagazine` pour y ajouter les champs `ViewCount` et `DownloadCount`.

```csharp
public class BookMagazine
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Tags { get; set; }
    public string FilePath { get; set; }
    public string CoverImagePath { get; set; }
    public DateTime UploadDate { get; set; }

    public int AuthorId { get; set; }
    public Author Author { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; }

    // Compteur de vues
    public int ViewCount { get; set; } = 0;

    // Compteur de téléchargements
    public int DownloadCount { get; set; } = 0;
}
```

#### Étape 1.2 : Créer et appliquer une migration

Générez et appliquez une migration pour ajouter ces champs à votre base de données.

```bash
dotnet ef migrations add AddViewAndDownloadCountsToBooksMagazines
dotnet ef database update
```

---

### 2. **Incrémenter le compteur de vues et de téléchargements**

À chaque fois qu'un utilisateur consulte ou télécharge un livre ou magazine, nous devons incrémenter les compteurs correspondants.

#### Étape 2.1 : Incrémenter le compteur de vues

Modifiez l'action `GetBookMagazine` dans le contrôleur `BookMagazineController` pour incrémenter le compteur de vues.

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetBookMagazine(int id)
{
    var bookMagazine = await _context.BooksMagazines
        .Include(b => b.Author)
        .Include(b => b.Category)
        .FirstOrDefaultAsync(b => b.Id == id);

    if (bookMagazine == null)
        return NotFound();

    // Vérifier que les entités liées existent
    if (bookMagazine.Author == null || bookMagazine.Category == null)
        return StatusCode(500, "Invalid data: Author or Category not found.");

    // Incrémenter le compteur de vues
    bookMagazine.ViewCount++;
    _context.BooksMagazines.Update(bookMagazine);
    await _context.SaveChangesAsync();

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
        bookMagazine.ViewCount
    });
}
```

#### Étape 2.2 : Incrémenter le compteur de téléchargements

Ajoutez une action `DownloadBookMagazine` qui gère le téléchargement des fichiers et incrémente le compteur de téléchargements.

```csharp
[HttpGet("download/{id}")]
public async Task<IActionResult> DownloadBookMagazine(int id)
{
    var bookMagazine = await _context.BooksMagazines
        .FirstOrDefaultAsync(b => b.Id == id);

    if (bookMagazine == null)
        return NotFound();

    // Incrémenter le compteur de téléchargements
    bookMagazine.DownloadCount++;
    _context.BooksMagazines.Update(bookMagazine);
    await _context.SaveChangesAsync();

    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", bookMagazine.FilePath.TrimStart('/'));
    if (!System.IO.File.Exists(filePath))
        return NotFound("File not found on server.");

    var fileBytes = System.IO.File.ReadAllBytes(filePath);
    var fileName = Path.GetFileName(filePath);

    return File(fileBytes, "application/octet-stream", fileName);
}
```

---

### 3. **Recherche avancée avec tri par popularité**

Nous allons maintenant ajouter des fonctionnalités de recherche avancée avec la possibilité de trier les résultats par popularité (vues ou téléchargements).

#### Étape 3.1 : Recherche simple par mots-clés

Cette méthode permet de rechercher des livres/magazines par titre, description, auteur, ou tags.

```csharp
[HttpGet("search")]
public IActionResult SearchBooksMagazines([FromQuery] string keyword)
{
    var booksMagazines = _context.BooksMagazines
        .Where(b => b.Title.Contains(keyword) ||
                    b.Description.Contains(keyword) ||
                    b.Author.Name.Contains(keyword) ||
                    b.Tags.Contains(keyword))
        .Select(b => new {
            b.Id,
            b.Title,
            Author = b.Author.Name,
            b.CoverImagePath,
            b.UploadDate,
            b.ViewCount
        })
        .ToList();

    return Ok(booksMagazines);
}
```

#### Étape 3.2 : Recherche avancée avec filtres et tri par popularité

Nous ajoutons une méthode de recherche plus avancée avec la possibilité de filtrer par catégorie, auteur, mots-clés, et date de publication. On peut également trier par popularité (vues).

```csharp
[HttpGet("advanced-search")]
public IActionResult SearchBooksMagazines([FromQuery] string keyword, [FromQuery] string category, [FromQuery] string author, [FromQuery] DateTime? publishDate, [FromQuery] bool sortByPopularity = false)
{
    var query = _context.BooksMagazines
        .Include(b => b.Author)
        .Include(b => b.Category)
        .AsQueryable();

    if (!string.IsNullOrEmpty(keyword))
    {
        query = query.Where(b => b.Title.Contains(keyword) ||
                                 b.Description.Contains(keyword) ||
                                 b.Tags.Contains(keyword));
    }

    if (!string.IsNullOrEmpty(category))
    {
        query = query.Where(b => b.Category.Name == category);
    }

    if (!string.IsNullOrEmpty(author))
    {
        query = query.Where(b => b.Author.Name == author);
    }

    if (publishDate.HasValue)
    {
        query = query.Where(b => b.UploadDate >= publishDate.Value);
    }

    // Trier par popularité (ViewCount) si demandé
    if (sortByPopularity)
    {
        query = query.OrderByDescending(b => b.ViewCount);
    }

    var results = query.Select(b => new {
        b.Id,
        b.Title,
        Author = b.Author.Name,
        b.CoverImagePath,
        b.UploadDate,
        b.ViewCount
    }).ToList();

    return Ok(results);
}
```

#### Étape 3.3 : Requête pour trier par vues et téléchargements

Ajoutez des méthodes spécifiques pour trier les livres/magazines par popularité (vues ou téléchargements).

- **Trier par vues** :

```csharp
[HttpGet("search/popular")]
public IActionResult SearchBooksMagazinesByPopularity()
{
    var booksMagazines = _context.BooksMagazines
        .OrderByDescending(b => b.ViewCount)
        .Select(b => new {
            b.Id,
            b.Title,
            Author = b.Author.Name,
            b.CoverImagePath,
            b.UploadDate,
            b.ViewCount
        })
        .ToList();

    return Ok(booksMagazines);
}
```

- **Trier par téléchargements** :

```csharp
[HttpGet("search/popular-downloads")]
public IActionResult SearchBooksMagazinesByDownloads()
{
    var booksMagazines = _context.BooksMagazines
        .OrderByDescending(b => b.DownloadCount)
        .Select(b => new {
            b.Id,
            b.Title,
            Author = b.Author.Name,
            b.CoverImagePath,
            b.UploadDate,
            b.DownloadCount
        })
        .ToList();

    return Ok(booksMagazines);
}
```

---

### 4. **Suggestions basées sur l'historique de lecture**

Enfin, nous implémentons une fonctionnalité de suggestions pour l'utilisateur basée sur son historique de lecture.

#### Étape 4.1 : Suggestions bas

ées sur les catégories déjà lues

Cette méthode va suggérer des livres ou magazines à l'utilisateur en fonction des catégories des livres qu'il a déjà consultés.

```csharp
[HttpGet("suggestions")]
[Authorize]
public IActionResult GetSuggestions()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    // Obtenez les catégories des livres déjà lus par l'utilisateur
    var categories = _context.UserReadingHistory
        .Where(ur => ur.UserId == userId)
        .Select(ur => ur.BookMagazine.CategoryId)
        .Distinct()
        .ToList();

    // Obtenez les suggestions basées sur ces catégories
    var suggestions = _context.BooksMagazines
        .Where(b => categories.Contains(b.CategoryId))
        .Select(b => new {
            b.Id,
            b.Title,
            Author = b.Author.Name,
            b.CoverImagePath,
            b.UploadDate
        })
        .ToList();

    return Ok(suggestions);
}
```

---

### 5. **Tester les fonctionnalités avec Swagger**

Après avoir implémenté les différentes fonctionnalités de votre API, vous devez les tester. L'interface **Swagger** est un excellent outil pour cela, car elle permet de simuler des requêtes HTTP directement dans le navigateur.

Swagger expose tous les endpoints de votre API et permet de tester les requêtes avec différents paramètres, y compris l'authentification avec un token JWT pour les actions protégées.

#### Étape 5.1 : Accéder à Swagger

Si Swagger est correctement configuré dans votre projet ASP.NET Core, vous pouvez y accéder via l'URL suivante :

```
http://localhost:5000/swagger/index.html
```

Remplacez `localhost:5000` par l'URL et le port sur lequel votre API s'exécute.

---

### 5.2 : **Test des fonctionnalités**

#### 5.2.1 : **Authentification JWT avec Bearer Token**

Pour les actions protégées par l'authentification (comme les suggestions basées sur l'historique de lecture), vous devez fournir un **Bearer Token** dans les en-têtes de vos requêtes. Voici comment obtenir ce token et l'utiliser dans Swagger.

##### Obtenir le token JWT
- Allez dans l'endpoint `POST /api/Auth/login` dans Swagger.
- Fournissez vos identifiants d'utilisateur (email et mot de passe) dans le corps de la requête.
- Cliquez sur **Execute** pour envoyer la requête.

Swagger vous renverra une réponse JSON contenant un **token JWT** similaire à ceci :

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI5YWQ1MjRkYS05ZmIyLTRlY2EtOWQ1My1kOGVhOTQzMTgyZTIiLCJlbWFpbCI6Im5peEBuaXguZnIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoibml4QG5peC5mciIsImV4cCI6MTY4MjIxMTU5MCwiaXNzIjoiTGlicmFyeUFwaSIsImF1ZCI6IkxpYnJhcnlBcGlVc2VycyJ9.abc1234xyz5678"
}
```

##### Ajouter le Bearer Token dans Swagger

1. Copiez le token reçu lors de l'authentification.
2. En haut à droite de Swagger, vous verrez un bouton **Authorize**. Cliquez dessus.
3. Dans le champ `Authorization`, entrez le token avec le format suivant :
   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI5YWQ1MjRkYS05ZmIyLTRlY2EtOWQ1My1kOGVhOTQzMTgyZTIiLCJlbWFpbCI6Im5peEBuaXguZnIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yYy9jbGFpbXMvcm9sZSI6IlVzZXIiLCJleHAiOjE3MjkxOTM1MzcsImlzcyI6IkxpYnJhcnlBcGkiLCJhdWQiOiJMaWJyYXJ5QXBpVXNlcnMifQ.n-pzBOigRerO51e4PmAs3ET27ktpumCpfJO34BIS3S4
   ```
4. Cliquez sur **Authorize**. Swagger utilisera maintenant ce token pour toutes les requêtes nécessitant une authentification.

---

#### 5.2.2 : **Tester les différents endpoints**

Voici comment tester chaque fonctionnalité que vous avez implémentée avec Swagger.

##### a) **Ajouter un livre ou magazine**

- Allez à l'endpoint `POST /api/BookMagazine/add`.
- Remplissez les champs requis :
  - **Title** : Le titre du livre ou magazine.
  - **Description** : Une description.
  - **Tags** : Des mots-clés associés.
  - **AuthorId** et **CategoryId** : Les ID de l'auteur et de la catégorie.
  - **FilePath** : Le chemin du fichier.
  - **CoverImagePath** : Le chemin de l'image de couverture.
- Cliquez sur **Execute** pour envoyer la requête.

##### b) **Lister les livres ou magazines**

- Allez à l'endpoint `GET /api/BookMagazine/list`.
- Cliquez sur **Execute** pour obtenir la liste des livres/magazines.

##### c) **Détails d'un livre ou magazine**

- Allez à l'endpoint `GET /api/BookMagazine/{id}`.
- Remplacez `{id}` par l'ID du livre/magazine que vous souhaitez consulter.
- Cliquez sur **Execute** pour obtenir les détails.

##### d) **Télécharger un livre ou magazine**

- Allez à l'endpoint `GET /api/BookMagazine/download/{id}`.
- Remplacez `{id}` par l'ID du livre/magazine à télécharger.
- Cliquez sur **Execute** pour tester le téléchargement du fichier.

##### e) **Recherche simple**

- Allez à l'endpoint `GET /api/BookMagazine/search`.
- Saisissez un **keyword** dans les paramètres (ex : un mot-clé dans le titre, la description ou les tags).
- Cliquez sur **Execute** pour obtenir les résultats de la recherche.

##### f) **Recherche avancée**

- Allez à l'endpoint `GET /api/BookMagazine/advanced-search`.
- Remplissez les paramètres de recherche comme le **keyword**, **category**, **author**, **publishDate**, et **sortByPopularity** (ce dernier est un booléen pour trier par popularité).
- Cliquez sur **Execute** pour voir les résultats filtrés.

##### g) **Suggestions personnalisées**

- Allez à l'endpoint `GET /api/BookMagazine/suggestions`.
- Ce endpoint est protégé par un token JWT, donc assurez-vous d'avoir ajouté le **Bearer Token** comme expliqué dans la section précédente.
- Cliquez sur **Execute** pour obtenir des suggestions basées sur l'historique de lecture de l'utilisateur connecté.

##### h) **Recherche par popularité**

- Allez à l'endpoint `GET /api/BookMagazine/search/popular` pour obtenir la liste triée par popularité (vues).
- Ou utilisez `GET /api/BookMagazine/search/popular-downloads` pour trier par téléchargements.

---

### 5.3 : **Vérifier les réponses**

Swagger affichera les réponses de votre API sous forme de JSON, ce qui vous permettra de vérifier si les fonctionnalités fonctionnent correctement.

Exemple de réponse pour une recherche :

```json
[
  {
    "id": 1,
    "title": "Livre Exemple",
    "author": "John Doe",
    "coverImagePath": "/images/book1.jpg",
    "uploadDate": "2023-10-17T00:00:00",
    "viewCount": 123
  },
  {
    "id": 2,
    "title": "Magazine Exemple",
    "author": "Jane Doe",
    "coverImagePath": "/images/magazine1.jpg",
    "uploadDate": "2023-10-17T00:00:00",
    "viewCount": 98
  }
]
```

---

### Conclusion

Nous avons implémenté plusieurs fonctionnalités dans cette API REST en ASP.NET Core :
- Ajout de compteurs de vues et de téléchargements pour les livres/magazines.
- Recherche avancée avec filtres par auteur, catégorie, et mots-clés.
- Tri par popularité (vues ou téléchargements).
- Suggestions personnalisées basées sur l'historique de lecture.

Ces fonctionnalités sont idéales pour améliorer l'expérience utilisateur d'une bibliothèque numérique. Vous pouvez maintenant tester chacune d'entre elles en utilisant **Swagger** ou tout autre outil pour faire des requêtes HTTP (comme Postman).

Swagger est un excellent outil pour tester et documenter vos API REST. Il permet d'interagir avec vos endpoints directement via l'interface web, facilitant ainsi le développement et le débogage. Avec le support du token JWT, vous pouvez également tester les fonctionnalités protégées par l'authentification dans un environnement sécurisé.

N'oubliez pas d'exécuter chaque fonctionnalité que vous avez développée, de vérifier les réponses JSON et de vous assurer que tout fonctionne comme prévu.


