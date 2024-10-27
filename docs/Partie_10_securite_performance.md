
## Partie 10 : Tutoriel de Sécurité et Performance pour l’API REST

Dans cette partie, nous allons ajouter des mesures de sécurité supplémentaires et optimiser les performances de l’API.

### 1. **Authentification JWT**

Nous avons déjà configuré l'authentification JWT. Rappelons brièvement le fonctionnement et les composants nécessaires pour cette configuration.

#### Fichier `appsettings.json`

Ajoutez ou vérifiez les paramètres de configuration pour le JWT :

```json
"Jwt": {
  "Key": "YourSecureKeyHere",
  "Issuer": "YourIssuer",
  "Audience": "YourAudience"
}
```

#### Configuration dans `Program.cs`

Vérifiez que l’authentification JWT est correctement configurée dans `Program.cs` :

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
```

### 2. **Protection contre les attaques courantes**

#### Protection contre l'Injection SQL

En utilisant Entity Framework, les paramètres sont automatiquement protégés contre l'injection SQL, car toutes les requêtes sont préparées de manière sécurisée. Il est néanmoins conseillé d’utiliser les filtres LINQ pour sécuriser les requêtes.

#### Protection contre CSRF (Cross-Site Request Forgery)

Dans une API REST, CSRF est moins problématique, mais si votre API est utilisée par des applications frontales, vous pouvez ajouter une vérification de l’origine des requêtes (mécanisme CORS) pour restreindre les requêtes venant de domaines spécifiques.

Ajoutez cette configuration dans `Program.cs` :

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("https://yourfrontend.com") // Remplacez par votre domaine
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

app.UseCors("CorsPolicy");
```

### 3. **Mise en Cache**

Pour améliorer les performances de l'API, vous pouvez mettre en cache les réponses des endpoints les plus sollicités. La mise en cache permet de limiter les appels à la base de données pour les requêtes qui peuvent être servies avec des données qui ne changent pas fréquemment.

#### a) Configuration du Service de Cache

Ajoutez le service de cache en mémoire dans `Program.cs` :

```csharp
builder.Services.AddMemoryCache();
```

#### b) Mise en Cache dans les Méthodes

Implémentons la mise en cache sur plusieurs méthodes comme `GetBookMagazine`, `GetBooksMagazinesList`, et `GetPopularBooksMagazines`.

---

#### **Méthode : `GetBookMagazine` (détails d'un livre)**

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetBookMagazine(int id)
{
    if (!_cache.TryGetValue($"BookMagazine_{id}", out var bookMagazine))
    {
        bookMagazine = await _context.BooksMagazines
            .Include(b => b.Author)
            .Include(b => b.Category)
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
                b.UploadDate,
                b.ViewCount,
                b.DownloadCount,
                AverageRating = _context.Ratings
                    .Where(r => r.BookMagazineId == id)
                    .Average(r => (double?)r.RatingValue) ?? 0,
                CommentCount = _context.Comments.Count(c => c.BookMagazineId == id)
            })
            .FirstOrDefaultAsync();

        if (bookMagazine == null)
            return NotFound();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        _cache.Set($"BookMagazine_{id}", bookMagazine, cacheOptions);
    }

    return Ok(bookMagazine);
}
```

#### **Méthode : `GetBooksMagazinesList` (liste de livres/magazines)**

```csharp
[HttpGet("list")]
public async Task<IActionResult> GetBooksMagazinesList()
{
    if (!_cache.TryGetValue("BooksMagazinesList", out var booksMagazines))
    {
        booksMagazines = await _context.BooksMagazines
            .Select(b => new
            {
                b.Id,
                b.Title,
                Author = b.Author.Name,
                Category = b.Category.Name,
                b.CoverImagePath,
                b.UploadDate
            })
            .ToListAsync();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        _cache.Set("BooksMagazinesList", booksMagazines, cacheOptions);
    }

    return Ok(booksMagazines);
}
```

#### **Méthode : `GetPopularBooksMagazines` (contenus populaires)**

```csharp
[HttpGet("search/popular")]
public async Task<IActionResult> GetPopularBooksMagazines()
{
    if (!_cache.TryGetValue("PopularBooksMagazines", out var popularBooksMagazines))
    {
        popularBooksMagazines = await _context.BooksMagazines
            .OrderByDescending(b => b.ViewCount)
            .Select(b => new
            {
                b.Id,
                b.Title,
                Author = b.Author.Name,
                b.CoverImagePath,
                b.UploadDate,
                b.ViewCount
            })
            .ToListAsync();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };

        _cache.Set("PopularBooksMagazines", popularBooksMagazines, cacheOptions);
    }

    return Ok(popularBooksMagazines);
}
```

### 4. **Résumé des Étapes de Test**

Une fois la configuration en place, voici comment tester les fonctionnalités ajoutées :

1. **Tester l'authentification JWT** : Assurez-vous que l'authentification est bien en place et que seuls les utilisateurs authentifiés peuvent accéder aux endpoints protégés.
2. **Vérifier les protections contre les attaques courantes** : Utilisez des outils comme OWASP ZAP pour vérifier les failles potentielles (injections, CSRF, etc.).
3. **Test de la mise en cache** :
   - Faites plusieurs appels à un endpoint mis en cache (par exemple, `GetBookMagazine`) et vérifiez que les requêtes suivantes sont plus rapides.
   - Testez l'invalidation et la mise à jour du cache en effectuant des modifications (ex. : ajout d'un commentaire) et en observant les mises à jour.


### Conclusion

En suivant ce tutoriel, vous avez maintenant amélioré la sécurité et optimisé les performances de l’API avec :
- **L’authentification JWT** pour sécuriser les endpoints.
- **La protection contre les attaques courantes** avec des filtres, des configurations CORS, et des vérifications de l’entrée.
- **La mise en cache** sur plusieurs endpoints stratégiques pour réduire la charge de la base de données et améliorer la rapidité de réponse de l’API.

Cela contribuera à rendre votre application plus robuste, sécurisée et performante.
