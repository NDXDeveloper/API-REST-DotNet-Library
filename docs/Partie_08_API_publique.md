
## Tutoriel : Mise en Place de l’API Publique

### 1. **Configuration des Politiques CORS pour les Endpoints Publics**

CORS (Cross-Origin Resource Sharing) vous permet de contrôler les sites autorisés à effectuer des requêtes vers votre API. Dans cette étape, nous ajoutons une politique CORS spécifique pour les routes publiques.

1. **Ouvrez le fichier `Program.cs`.**

2. **Ajoutez la configuration CORS** dans la section des services pour définir une politique qui autorise l’accès depuis une URL spécifique :

   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("PublicApiPolicy", builder =>
       {
           builder.WithOrigins("https://trustedwebsite.com")  // Remplacez par les domaines que vous souhaitez autoriser
                  .AllowAnyHeader()
                  .AllowAnyMethod();
       });
   });
   ```

3. **Activez la politique CORS** en l’ajoutant dans le pipeline de requêtes de l'application, juste avant `app.UseAuthentication()` et `app.UseAuthorization()` :

   ```csharp
   app.UseCors("PublicApiPolicy");
   ```

Cela garantit que seuls les sites spécifiés auront accès à vos endpoints publics.

---

### 2. **Création des Endpoints Publics**

Dans cette section, nous ajoutons des routes accessibles sans authentification. Par exemple, pour exposer des statistiques sur les téléchargements ou l'utilisation de contenus.

#### Exemple : **Endpoint Public pour les Statistiques de Téléchargement**

1. **Ouvrez le contrôleur concerné**, par exemple `BookMagazineController`.

2. **Ajoutez une route publique** pour récupérer des statistiques sans authentification. Dans cet exemple, nous créons un endpoint qui renvoie la liste des livres ou magazines les plus téléchargés :

   ```csharp
   [HttpGet("public/popular-downloads")]
   [AllowAnonymous]  // Rendre l'endpoint public
   public IActionResult GetPopularDownloads()
   {
       var popularDownloads = _context.BooksMagazines
           .OrderByDescending(b => b.DownloadCount)
           .Select(b => new
           {
               b.Id,
               b.Title,
               b.Author.Name,
               b.DownloadCount
           })
           .ToList();

       return Ok(popularDownloads);
   }
   ```

   - **Explication** :
     - La route `/public/popular-downloads` est marquée avec `[AllowAnonymous]`, ce qui permet un accès sans authentification.
     - La méthode récupère les livres ou magazines les plus téléchargés et retourne des informations de base sur chacun.

#### Exemple : **Endpoint Public pour Obtenir les Détails d'un Livre ou Magazine**

Pour exposer les informations de base sur un livre ou magazine, ajoutez un autre endpoint :

```csharp
[HttpGet("public/book-details/{id}")]
[AllowAnonymous]
public async Task<IActionResult> GetBookDetails(int id)
{
    var bookMagazine = await _context.BooksMagazines
        .Where(b => b.Id == id)
        .Select(b => new
        {
            b.Id,
            b.Title,
            b.Description,
            Author = b.Author.Name,
            b.ViewCount,
            b.DownloadCount
        })
        .FirstOrDefaultAsync();

    if (bookMagazine == null)
        return NotFound();

    return Ok(bookMagazine);
}
```

   - **Explication** :
     - La route `/public/book-details/{id}` permet d’obtenir des informations sur un livre ou magazine spécifique en utilisant son `id`.
     - La méthode retourne les informations de base pour permettre aux utilisateurs de consulter des informations limitées.

---

### 3. **Documentation des Endpoints Publics avec Swagger**

1. **Ouvrez `Program.cs`** et vérifiez que Swagger est configuré pour documenter les routes publiques.

   Dans la configuration Swagger, ces routes seront ajoutées automatiquement. Pour vérifier et tester les routes publiques :

   ```csharp
   // Si Swagger n'est activé qu'en développement, accédez à http://localhost:{port}/swagger pour explorer les endpoints
   if (app.Environment.IsDevelopment())
   {
       app.UseSwagger();
       app.UseSwaggerUI(c =>
       {
           c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryApi v1");
       });
   }
   ```

2. **Accédez à l’interface Swagger** pour vérifier la documentation des endpoints publics.

---

### 4. **Tests des Endpoints Publics**

1. **Depuis un navigateur ou un client API** comme Postman ou Curl, effectuez des requêtes GET sur les routes publiques, par exemple :

   - `/api/BookMagazine/public/popular-downloads`
   - `/api/BookMagazine/public/book-details/{id}`

2. **Vérifiez les restrictions CORS** en accédant à ces endpoints depuis le domaine autorisé dans la configuration CORS. Testez également un domaine non autorisé pour confirmer la restriction.

