 
Les étapes 1 et 2 que nous avons explorées jusqu'à présent couvrent effectivement deux grands aspects de notre application :

1. **La gestion des utilisateurs** à travers **ASP.NET Identity** :
   - Inscription, connexion, et gestion des rôles.
   - Autorisations basées sur les rôles pour limiter l'accès à certaines fonctionnalités de l'application.

2. **La gestion des livres et magazines** :
   - Un ensemble de contrôleurs API permettant de gérer les livres et magazines, incluant l'ajout, la modification, la suppression, et la consultation des ouvrages.
   - Ces fonctionnalités permettent de construire une base de données d'ouvrages accessible via des requêtes HTTP.

### Étape 3 : Bibliothèque Personnelle

Pour aller plus loin, nous allons maintenant enrichir l'application avec une **bibliothèque personnelle**. L'idée est de permettre à chaque utilisateur d'ajouter des livres ou des magazines à ses favoris et de suivre l'historique de ses lectures. Ces fonctionnalités introduisent des aspects personnalisés dans l'application, renforçant l'interaction utilisateur avec le contenu.

Chaque fonctionnalité sera envisagée comme suit :

#### 1. **Favoris**

##### Idée d'implémentation :
- **Conception de la base de données** : 
  - Créer une nouvelle table dans la base de données appelée `UserFavorites`, qui liera les utilisateurs aux livres ou magazines qu'ils souhaitent ajouter à leurs favoris.
  - Cette table contiendra les colonnes suivantes : `UserId` (qui fait référence à la table des utilisateurs `AspNetUsers`), `BookMagazineId` (qui référence la table des livres ou magazines `BooksMagazines`), et une **clé primaire composite** basée sur `UserId` et `BookMagazineId`.

- **API pour les favoris** : 
  - Un nouveau contrôleur API (`FavoritesController`) sera créé pour permettre aux utilisateurs de gérer leurs favoris. Les principales actions incluront l'ajout d'un favori, la suppression d'un favori et la consultation de la liste des favoris d'un utilisateur.

##### Exemple de méthodes dans `FavoritesController.cs` :
- **Ajouter un favori** : `[HttpPost("add-favorite/{bookMagazineId}")]` pour permettre à l'utilisateur d'ajouter un livre ou magazine à sa liste de favoris.
- **Récupérer les favoris** : `[HttpGet("my-favorites")]` pour permettre à l'utilisateur de récupérer la liste des ouvrages qu'il a ajoutés à ses favoris.

#### 2. **Historique de lecture**

##### Idée d'implémentation :
- **Conception de la base de données** :
  - Créer une table `UserReadingHistory` qui enregistrera l'historique des lectures de chaque utilisateur. Les colonnes incluront : `UserId`, `BookMagazineId`, `LastReadDate`, pour suivre quel utilisateur a consulté quel livre/magazine et à quel moment.
  - Cette fonctionnalité permettra aux utilisateurs de visualiser quels livres ou magazines ils ont déjà consultés et à quelle date.

- **API pour l'historique de lecture** :
  - Un contrôleur API (`ReadingHistoryController`) sera mis en place pour permettre aux utilisateurs de suivre leur historique de lecture et de mettre à jour cet historique lorsqu'ils consultent un livre ou un magazine.

##### Exemple de méthodes dans `ReadingHistoryController.cs` :
- **Récupérer l'historique de lecture** : `[HttpGet("reading-history")]` pour afficher à l'utilisateur la liste des livres ou magazines qu'il a consultés.
- **Mettre à jour l'historique de lecture** : `[HttpPost("update-history/{bookMagazineId}")]` pour ajouter un livre/magazine à l'historique de lecture d'un utilisateur une fois qu'il l'a consulté.


### Tutoriel : Ajouter une fonctionnalité de Favoris et d'Historique de lecture (Partie 3)

Ce tutoriel décrit comment passer de la partie 2 à la partie 3 en ajoutant une bibliothèque personnelle, comprenant la gestion des favoris et de l'historique de lecture dans une API REST en ASP.NET Core. Nous supposerons que vous avez déjà suivi la partie 1 et la partie 2, et que l'environnement de développement et la base de données sont déjà configurés.

#### 1. **Ajouter les modèles pour les favoris et l'historique de lecture**

Dans le dossier **Models**, créez deux nouvelles classes : `UserFavorite` et `UserReadingHistory`.

```csharp
public class UserFavorite
{
    public string UserId { get; set; }  // ID de l'utilisateur
    public ApplicationUser User { get; set; }  // Référence à l'utilisateur

    public int BookMagazineId { get; set; }  // ID du livre ou magazine
    public BookMagazine BookMagazine { get; set; }  // Référence au livre ou magazine
}


public class UserReadingHistory
{
    public string UserId { get; set; }  // ID de l'utilisateur
    public ApplicationUser User { get; set; }  // Référence à l'utilisateur

    public int BookMagazineId { get; set; }  // ID du livre ou magazine
    public BookMagazine BookMagazine { get; set; }  // Référence au livre ou magazine

    public DateTime LastReadDate { get; set; }  // Date de la dernière consultation
}

```

Ces modèles représentent les favoris et l'historique de lecture d'un utilisateur. Ils contiennent une référence à l'utilisateur et au livre ou magazine.

#### 2. **Modifier le contexte de la base de données (ApplicationDbContext)**

Ensuite, ajoutez ces nouveaux modèles dans le contexte de la base de données (**ApplicationDbContext.cs**) et configurez les clés primaires composites.

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


    // Nouvelles tables ajoutées pour l'étape 3 (Bibliothèque Personnelle)
    public DbSet<UserFavorite> UserFavorites { get; set; }  // Ajout de la table pour les favoris de l'utilisateur
    public DbSet<UserReadingHistory> UserReadingHistory { get; set; }  // Ajout de la table pour l'historique de lecture

    // Surcharge de la méthode OnModelCreating pour configurer les clés primaires composites
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);  // Appel de la méthode de la classe de base

        // Configuration de la clé primaire composite pour UserFavorite
        modelBuilder.Entity<UserFavorite>()
            .HasKey(uf => new { uf.UserId, uf.BookMagazineId });

        // Configuration de la clé primaire composite pour UserReadingHistory
        modelBuilder.Entity<UserReadingHistory>()
            .HasKey(urh => new { urh.UserId, urh.BookMagazineId });
    }
}

```

Cela permet d'ajouter les tables `UserFavorites` et `UserReadingHistory` à la base de données, ainsi que la gestion des clés composites.

#### 3. **Créer le contrôleur des favoris**

Créez un nouveau contrôleur nommé `FavoritesController`. Ce contrôleur permettra à l'utilisateur de gérer ses favoris (ajout, récupération et suppression).

```csharp
using Microsoft.AspNetCore.Authorization;  // Nécessaire pour gérer l'authentification et l'autorisation des utilisateurs.
using Microsoft.AspNetCore.Mvc;            // Fournit les outils pour créer des API RESTful, comme les contrôleurs et les actions HTTP (GET, POST, etc.).
using Microsoft.EntityFrameworkCore;       // Permet d'utiliser Entity Framework Core pour interagir avec la base de données via le contexte de données (ApplicationDbContext).
using System.Security.Claims;              // Utilisé pour manipuler les informations des utilisateurs (claims) dans les tokens d'authentification, comme l'identifiant de l'utilisateur (UserId).


namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Cette annotation assure que toutes les actions dans ce contrôleur nécessitent une authentification.
    public class FavoritesController : ControllerBase
    {
        // Dépendance à l'ApplicationDbContext pour interagir avec la base de données.
        private readonly ApplicationDbContext _context;

        // Le constructeur injecte le contexte de la base de données via l'injection de dépendances.
        public FavoritesController(ApplicationDbContext context)
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

```

#### 4. **Créer le contrôleur de l'historique de lecture**

Ajoutez également un contrôleur `ReadingHistoryController` pour gérer l'historique de lecture.

```csharp
// Les directives 'using' ci-dessous permettent d'importer les namespaces nécessaires à la création du contrôleur et à la gestion des autorisations et des accès aux données.

using Microsoft.AspNetCore.Authorization;  // Nécessaire pour gérer l'authentification et l'autorisation des utilisateurs dans l'application via des attributs comme [Authorize].
using Microsoft.AspNetCore.Mvc;            // Fournit les outils essentiels pour créer des contrôleurs API, gérer les routes HTTP et les actions telles que GET, POST, PUT, DELETE.
using Microsoft.EntityFrameworkCore;       // Permet l'utilisation d'Entity Framework Core pour interagir avec la base de données et effectuer des opérations CRUD.
                                           
// using System.IdentityModel.Tokens.Jwt;   // Commenté car non utilisé. Ce namespace est utile pour manipuler les JWT (JSON Web Tokens) directement si nécessaire.
                                           
using System.Security.Claims;              // Utilisé pour extraire des informations de l'utilisateur connecté (via les claims, comme l'identifiant d'utilisateur) à partir de son token d'authentification.


namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Nécessite que l'utilisateur soit authentifié pour accéder à toutes les actions de ce contrôleur.
    public class ReadingHistoryController : ControllerBase
    {
        // Dépendance injectée pour interagir avec la base de données via le contexte ApplicationDbContext.
        private readonly ApplicationDbContext _context;

        // Constructeur permettant d'injecter le contexte de base de données dans le contrôleur via l'injection de dépendances.
        public ReadingHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

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
                .ThenInclude(b => b.Author)     // Inclure également l'auteur du livre/magazine.
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

```

#### 5. **Mise à jour de la base de données**

Après avoir ajouté les nouvelles entités **UserFavorite** et **UserReadingHistory** dans le modèle de votre base de données, il est nécessaire de créer une migration afin que la base de données reflète ces modifications. Voici les étapes à suivre pour effectuer cette migration et mettre à jour la base de données.

##### a. **Créer une migration**

Ouvrez la **console du gestionnaire de package NuGet** ou le **terminal** de votre projet et exécutez la commande suivante pour créer une migration basée sur les nouvelles entités que nous avons ajoutées :

```bash
Add-Migration AddUserFavoritesAndReadingHistory
```

Cette commande va générer un fichier de migration qui inclura la création des tables `UserFavorites` et `UserReadingHistory` dans la base de données.

##### b. **Vérifier la migration**

Après avoir exécuté cette commande, vous verrez un fichier de migration créé dans le dossier **Migrations** de votre projet. Le fichier généré devrait contenir des instructions pour créer les tables `UserFavorites` et `UserReadingHistory`. Assurez-vous que la structure est correcte.

##### c. **Mettre à jour la base de données**

Une fois la migration créée, vous devez appliquer ces modifications à la base de données. Pour cela, exécutez la commande suivante :

```bash
Update-Database
```

Cette commande va appliquer la migration à votre base de données en ajoutant les nouvelles tables.

##### d. **Vérification de la mise à jour**

Vous pouvez vérifier si les tables ont bien été créées dans votre base de données en consultant votre serveur de base de données, par exemple en utilisant **SQL Server Management Studio** ou un outil équivalent pour visualiser la structure de la base de données. Vous devriez maintenant voir les tables `UserFavorites` et `UserReadingHistory` ajoutées.

#### 6. **Tester avec Swagger**

Une fois les contrôleurs et la base de données configurés, vous pouvez tester les nouvelles fonctionnalités à l'aide de **Swagger**, un outil intégré qui vous permet d'explorer et d'interagir avec votre API.

##### a. **Lancer l'application**

Assurez-vous que votre application est bien lancée en mode développement (ou avec l'option Swagger activée). Vous pouvez le faire en exécutant votre projet via Visual Studio, Rider, ou en ligne de commande avec :

```bash
dotnet run
```

Une fois l'application démarrée, Swagger sera généralement accessible à une URL similaire à `http://localhost:5000/swagger` ou `http://localhost:<port>/swagger`.

##### b. **Explorer les nouvelles routes**

Swagger générera automatiquement la documentation de l'API et l'interface utilisateur basée sur les contrôleurs que nous avons ajoutés, notamment :

- **POST** `/api/favorites/add-favorite/{bookMagazineId}` : Ajouter un livre ou magazine aux favoris.
- **GET** `/api/favorites/my-favorites` : Récupérer la liste des favoris de l'utilisateur.
- **DELETE** `/api/favorites/remove-favorite/{bookMagazineId}` : Supprimer un livre ou magazine des favoris.

- **POST** `/api/reading-history/update-history/{bookMagazineId}` : Ajouter ou mettre à jour un livre ou magazine dans l'historique de lecture.
- **GET** `/api/reading-history/reading-history` : Récupérer l'historique de lecture de l'utilisateur.

##### c. **Tester les requêtes**

- **Ajouter un favori** : Utilisez la méthode `POST` avec un ID valide de `bookMagazineId` pour ajouter un livre ou un magazine aux favoris.
- **Récupérer vos favoris** : Testez la méthode `GET` pour afficher la liste de vos favoris.
- **Supprimer un favori** : Utilisez la méthode `DELETE` pour retirer un favori à l'aide de son `bookMagazineId`.
- **Mettre à jour l'historique** : Testez la méthode `POST` pour ajouter ou mettre à jour l'historique de lecture.
- **Récupérer l'historique de lecture** : Utilisez la méthode `GET` pour récupérer l'historique de lecture de l'utilisateur.

##### d. **Vérifier les réponses**

Swagger vous permet de voir les requêtes et réponses associées à chaque méthode. Par exemple :
- Si vous ajoutez un favori, vous devriez recevoir une réponse de type `200 OK` avec un message confirmant l'ajout.
- Si vous tentez d'ajouter un favori déjà existant, vous obtiendrez un code `409 Conflict`.
- Si vous essayez de supprimer un favori inexistant, une réponse `404 Not Found` sera retournée.



### Conclusion de l'étape 3 : Bibliothèque Personnelle

Félicitations ! 🎉 Vous avez désormais ajouté avec succès la fonctionnalité de **bibliothèque personnelle** à votre application. Cette étape a permis d’enrichir l’expérience utilisateur en offrant la possibilité d'ajouter des livres et magazines à une liste de favoris, ainsi que de suivre l’historique de lecture.

Grâce à cette nouvelle fonctionnalité, les utilisateurs peuvent désormais interagir de manière plus personnalisée avec le contenu de l’application, en conservant une trace de leurs lectures passées et en gérant leurs ouvrages préférés.

### Ce que vous avez accompli :
- La mise en place d’une table pour les **favoris** (`UserFavorites`), permettant aux utilisateurs d’ajouter et de gérer leurs ouvrages favoris.
- La création d’une table pour l'**historique de lecture** (`UserReadingHistory`), permettant de suivre les livres et magazines consultés par les utilisateurs.
- L’ajout de deux nouveaux contrôleurs API (`FavoritesController` et `ReadingHistoryController`) pour gérer ces nouvelles fonctionnalités.
- L’utilisation de **Swagger** pour tester et valider vos nouvelles routes API.

Avec ces fondations, votre application est prête à évoluer vers des fonctionnalités encore plus avancées comme la personnalisation de recommandations, l’intégration de notifications ou l’analyse des habitudes de lecture.

Continuez à explorer et à améliorer votre projet. Bon développement ! 🚀