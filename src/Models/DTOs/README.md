# DTOs (Data Transfer Objects)

Ce dossier contient les Data Transfer Objects (DTOs) utilisés dans l'API LibraryAPI pour faciliter le transfert de données entre les différentes couches de l'application, en particulier entre les contrôleurs et les services, tout en garantissant la sécurité et l'optimisation des performances.

## 🎯 Qu'est-ce qu'un DTO ?

Un **DTO (Data Transfer Object)** est un objet qui transporte des données entre les processus. Les DTOs sont utilisés pour :

- ✅ **Encapsuler les données** que vous souhaitez envoyer/recevoir sans exposer les détails internes
- ✅ **Optimiser les transferts** en ne transportant que les données nécessaires
- ✅ **Sécuriser l'API** en évitant l'exposition de données sensibles
- ✅ **Valider les entrées** avec des attributs de validation personnalisés
- ✅ **Découpler les couches** pour une meilleure maintenabilité

## 🏗️ Architecture des DTOs

### **Principe de Séparation**

- **Modèles d'entités** : Représentation base de données (Entity Framework)
- **DTOs** : Transfert de données optimisé et sécurisé
- **ViewModels** : Présentation côté client

### **Validation Intégrée**

Tous les DTOs utilisent des **attributs de validation personnalisés** pour garantir la sécurité et l'intégrité des données.

## 📋 Liste des DTOs

### **UserDto.cs**

**Usage** : Transfert sécurisé des informations utilisateur  
**Contexte** : Listes d'utilisateurs, profils publics, recherches admin

```csharp
public class UserDto
{
    public string? Id { get; set; }           // Identifiant unique
    public string? UserName { get; set; }     // Nom d'utilisateur public
    public string? Email { get; set; }        // Email (admin seulement)
    public DateTime? CreatedAt { get; set; }  // Date d'inscription
    public string? Role { get; set; }         // Rôle utilisateur
}
```

**Sécurité implémentée :**

- ❌ **Exclus** : PasswordHash, SecurityStamp, tokens
- ✅ **Inclus** : Données non sensibles uniquement
- 🔒 **Email** : Visible par les admins seulement

**Utilisé dans :**

- `AuthController.GetUsers()` - Liste des utilisateurs
- `AuthController.GetUserById()` - Détails utilisateur
- `AuthController.SearchUsers()` - Recherche d'utilisateurs
- `AuthController.GetUsersByRole()` - Filtrage par rôle

---

### **BookMagazineModel.cs**

**Usage** : Upload et modification de livres/magazines  
**Contexte** : Formulaires de création/édition avec fichiers

```csharp
public class BookMagazineModel
{
    [Required, SafeNameValidation(MinLength = 2, MaxLength = 200)]
    public string? Title { get; set; }

    [Required, SafeNameValidation(MinLength = 2, MaxLength = 100)]
    public string? Author { get; set; }

    [DescriptionValidation(MaxLength = 2000)]
    public string? Description { get; set; }

    [Required, SafeNameValidation(MinLength = 2, MaxLength = 50)]
    public string? Category { get; set; }

    [TagsValidation(MaxTags = 10, MaxTagLength = 30)]
    public string? Tags { get; set; }

    [Required, FileValidation(MaxSize = 100MB, AllowedExtensions = [".pdf", ".epub", ".mobi", ".txt", ".doc", ".docx"])]
    public IFormFile? File { get; set; }

    [FileValidation(MaxSize = 10MB, AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"])]
    public IFormFile? CoverImage { get; set; }
}
```

**Validation renforcée :**

- 📁 **Fichiers** : Taille, extension, type MIME, signature
- 🛡️ **Sécurité** : Noms sécurisés, contenu validé
- 📏 **Limites** : Tailles configurables par type
- 🏷️ **Tags** : Nombre et longueur contrôlés

**Utilisé dans :**

- `BookMagazineController.AddBookMagazine()` - Upload avec validation complète

## 🛡️ Système de Validation Avancé

### **Attributs de Validation Personnalisés**

#### **SafeNameValidation**

```csharp
[SafeNameValidation(MinLength = 2, MaxLength = 100)]
```

- ✅ Caractères autorisés : lettres, chiffres, espaces, ponctuation de base
- ❌ Bloque : Scripts malveillants, caractères spéciaux dangereux
- 🔧 Normalisation : Espaces multiples, trim automatique

#### **FileValidation**

```csharp
[FileValidation(MaxSize = 100MB, AllowedExtensions = [".pdf"], AllowedMimeTypes = ["application/pdf"])]
```

- 📏 **Taille** : Limites configurables par type
- 📄 **Extensions** : Liste blanche stricte
- 🔍 **MIME Types** : Validation du type réel
- 🔒 **Signatures** : Vérification des magic numbers
- 🚫 **Sécurité** : Détection de noms malveillants

#### **DescriptionValidation**

```csharp
[DescriptionValidation(MaxLength = 2000)]
```

- 📝 **Contenu** : Longueur contrôlée
- 🛡️ **XSS** : Protection contre scripts malveillants
- 🧹 **Nettoyage** : Suppression de contenu dangereux

#### **TagsValidation**

```csharp
[TagsValidation(MaxTags = 10, MaxTagLength = 30)]
```

- 🏷️ **Nombre** : Limite du nombre de tags
- 📏 **Longueur** : Taille maximale par tag
- ✂️ **Séparation** : Parsing intelligent par virgules
- 🔤 **Format** : Caractères autorisés contrôlés

## 📊 Utilisation dans l'Architecture

### **Flux de Données Sécurisé**

```
Client Request → DTO Validation → Business Logic → Entity Mapping → Database
```

### **Avantages de l'Approche**

1. **Sécurité** : Validation précoce et contrôle strict
2. **Performance** : Transfert uniquement des données nécessaires
3. **Maintenabilité** : Découplage entre API et modèles internes
4. **Évolutivité** : Versioning possible sans casser l'API

### **Pattern de Conversion**

```csharp
// DTO → Entity (Upload)
var bookMagazine = new BookMagazine
{
    Title = model.Title!,
    AuthorId = author.Id,
    CategoryId = category.Id,
    Description = model.Description ?? string.Empty,
    Tags = model.Tags ?? string.Empty
};

// Entity → DTO (Response)
var userDto = new UserDto
{
    Id = user.Id,
    UserName = user.UserName,
    Email = user.Email,
    Role = userRole
};
```

## 🚀 DTOs Recommandés pour Évolutions

### **Lectures et Recherches**

```csharp
// Résultats de recherche optimisés
public class BookSearchResultDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string AuthorName { get; set; }
    public string CategoryName { get; set; }
    public string CoverImageUrl { get; set; }
    public double AverageRating { get; set; }
    public int ViewCount { get; set; }
}

// Pagination avancée
public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
```

### **Statistiques et Analytics**

```csharp
// Dashboard utilisateur
public class UserDashboardDto
{
    public int FavoritesCount { get; set; }
    public int ReadBooksCount { get; set; }
    public int CommentsCount { get; set; }
    public double AverageRatingGiven { get; set; }
    public IEnumerable<BookSearchResultDto> RecentlyRead { get; set; }
    public IEnumerable<BookSearchResultDto> Recommendations { get; set; }
}

// Statistiques admin
public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalBooks { get; set; }
    public int TotalDownloads { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int NewBooksThisMonth { get; set; }
}
```

### **API Responses Standardisées**

```csharp
// Réponse API unifiée
public class ApiResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; }
}

// Réponse d'erreur détaillée
public class ErrorResponseDto
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
    public string TraceId { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 🔄 Mapping et Conversion

### **AutoMapper Configuration** (Recommandé)

```csharp
// Profile de mapping
public class LibraryMappingProfile : Profile
{
    public LibraryMappingProfile()
    {
        // Entity → DTO
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.Ignore()); // Géré séparément

        CreateMap<BookMagazine, BookSearchResultDto>()
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.Name))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));

        // DTO → Entity
        CreateMap<BookMagazineModel, BookMagazine>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FilePath, opt => opt.Ignore()) // Géré lors de l'upload
            .ForMember(dest => dest.UploadDate, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}
```

### **Extension Methods** pour Simplification

```csharp
public static class DtoExtensions
{
    public static UserDto ToDto(this ApplicationUser user, string? role = null)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            Role = role
        };
    }

    public static PagedResultDto<T> ToPagedResult<T>(this IEnumerable<T> items,
        int totalItems, int currentPage, int pageSize)
    {
        return new PagedResultDto<T>
        {
            Items = items,
            TotalItems = totalItems,
            CurrentPage = currentPage,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            HasNextPage = currentPage * pageSize < totalItems,
            HasPreviousPage = currentPage > 1
        };
    }
}
```

## 📋 Bonnes Pratiques Implémentées

### **1. Sécurité First**

- ✅ **Validation stricte** sur tous les champs
- ✅ **Sanitization** automatique des entrées
- ✅ **Pas d'exposition** de données sensibles
- ✅ **Type safety** avec nullable references

### **2. Performance Optimisée**

- ✅ **Projections** LINQ pour charger uniquement les données nécessaires
- ✅ **Lazy loading** évité avec Select explicites
- ✅ **Pagination** intégrée pour gros datasets
- ✅ **Compression** JSON automatique

### **3. Maintenabilité**

- ✅ **Documentation** exhaustive avec exemples
- ✅ **Naming conventions** cohérentes
- ✅ **Versioning** prêt pour évolutions API
- ✅ **Tests unitaires** facilités

### **4. Expérience Développeur**

- ✅ **IntelliSense** complet avec XML docs
- ✅ **Validation côté client** possible
- ✅ **Messages d'erreur** explicites
- ✅ **Debugging** facilité avec logging

## 🧪 Tests et Validation

### **Tests Unitaires DTOs**

```csharp
[Test]
public void BookMagazineModel_ValidFile_PassesValidation()
{
    // Arrange
    var model = new BookMagazineModel
    {
        Title = "Test Book",
        Author = "Test Author",
        Category = "Fiction",
        File = CreateMockFile("test.pdf", "application/pdf")
    };

    // Act
    var results = ValidateModel(model);

    // Assert
    Assert.That(results, Is.Empty);
}

[Test]
public void UserDto_SecurityFields_NotExposed()
{
    // Arrange
    var user = new ApplicationUser
    {
        PasswordHash = "sensitive_hash",
        SecurityStamp = "sensitive_stamp"
    };

    // Act
    var dto = user.ToDto();

    // Assert
    Assert.That(dto, Has.No.Property("PasswordHash"));
    Assert.That(dto, Has.No.Property("SecurityStamp"));
}
```

## 🚀 Évolutions Futures

### **Nouvelles Fonctionnalités**

- **GraphQL DTOs** : Support pour requêtes flexibles
- **Real-time DTOs** : SignalR avec DTOs typés
- **Batch Operations** : DTOs pour opérations en lot
- **Export/Import** : DTOs pour migration de données

### **Optimisations Avancées**

- **Memory Pooling** : Réutilisation d'objets DTO
- **Serialization** : Optimisation JSON avec System.Text.Json
- **Compression** : DTOs compressés pour gros volumes
- **Caching** : DTOs en cache avec invalidation intelligente

### **Sécurité Renforcée**

- **Rate Limiting** : Par DTO et endpoint
- **Input Sanitization** : Encore plus stricte
- **Audit Trail** : Tracking des modifications DTO
- **Encryption** : Champs sensibles chiffrés

## 📚 Documentation Technique

### **Attributs de Validation Disponibles**

- `[SafeNameValidation]` - Noms sécurisés
- `[FileValidation]` - Fichiers sécurisés
- `[DescriptionValidation]` - Textes longs
- `[TagsValidation]` - Système de tags
- `[StrictEmailValidation]` - Emails validés
- `[StrictPasswordValidation]` - Mots de passe forts

### **Performance Guidelines**

- ✅ Utiliser `Select()` pour projections
- ✅ Éviter le over-fetching de données
- ✅ Implémenter la pagination par défaut
- ✅ Utiliser `AsNoTracking()` en lecture seule

L'architecture DTOs actuelle offre une base solide pour une API sécurisée, performante et maintenable, avec une validation renforcée et une séparation claire des responsabilités.
