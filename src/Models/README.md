# Models

Le dossier `Models` contient les classes représentant les entités principales de l'application LibraryAPI, correspondant aux tables de la base de données. Cette architecture utilise Entity Framework Core avec des validations renforcées, un système d'audit complet et une sécurité multicouche.

## 🏗️ Architecture des Modèles

### **Principe de Design**

- **Entity First** : Modèles riches avec logique métier
- **Validation multicouche** : Attributs + règles métier + contraintes DB
- **Sécurité intégrée** : Protection contre injections et attaques
- **Audit trail** : Traçabilité complète des modifications
- **Relations explicites** : Navigation properties optimisées

## 📋 Entités Principales

### **👤 Gestion des Utilisateurs**

#### **ApplicationUser.cs**

**Hérite de** : `IdentityUser`  
**Rôle** : Utilisateur étendu avec informations personnalisées

```csharp
public class ApplicationUser : IdentityUser
{
    [SafeNameValidation(MinLength = 2, MaxLength = 100)]
    public string? FullName { get; set; }

    [DescriptionValidation(MaxLength = 1000)]
    public string? Description { get; set; }

    [Url, StringLength(500)]
    public string? ProfilePicture { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Extensions Identity :**

- ✅ Profil enrichi avec nom complet et description
- ✅ Photo de profil avec validation URL
- ✅ Horodatage de création automatique
- ✅ Validation sécurisée des champs personnalisés

---

### **📚 Bibliothèque et Contenu**

#### **BookMagazine.cs**

**Entité centrale** : Livres et magazines de la bibliothèque

```csharp
public class BookMagazine
{
    [Key]
    public int Id { get; set; }

    [Required, SafeNameValidation(MinLength = 2, MaxLength = 200)]
    public string Title { get; set; } = string.Empty;

    [Required, ForeignKey("AuthorId")]
    public Author Author { get; set; } = null!;

    [DescriptionValidation(MaxLength = 2000)]
    public string Description { get; set; } = string.Empty;

    [Required, ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;

    [TagsValidation(MaxTags = 10, MaxTagLength = 30)]
    public string Tags { get; set; } = string.Empty;

    // Gestion des fichiers sécurisée
    [Required, StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(500)]
    public string CoverImagePath { get; set; } = string.Empty;

    // Métadonnées
    public DateTime UploadDate { get; set; } = DateTime.Now;

    // Statistiques
    [Range(0, int.MaxValue)]
    public int ViewCount { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int DownloadCount { get; set; } = 0;

    // Noms originaux pour traçabilité
    [SafeNameValidation(MinLength = 1, MaxLength = 255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [SafeNameValidation(MinLength = 1, MaxLength = 255)]
    public string OriginalCoverImageName { get; set; } = string.Empty;

    // Note moyenne calculée
    [Range(0.0, 5.0)]
    public double AverageRating { get; set; }
}
```

**Fonctionnalités avancées :**

- 🔒 **Sécurité** : Validation des noms de fichiers et chemins
- 📊 **Statistiques** : Compteurs de vues et téléchargements
- 🏷️ **Tags** : Système de étiquetage avec validation
- ⭐ **Notation** : Système de notes avec calcul automatique
- 📝 **Traçabilité** : Noms originaux des fichiers

#### **Author.cs**

**Entité** : Auteurs des livres et magazines

```csharp
public class Author
{
    [Key]
    public int Id { get; set; }

    [Required, SafeNameValidation(MinLength = 2, MaxLength = 100)]
    public string Name { get; set; } = string.Empty;

    // Relation One-to-Many
    public ICollection<BookMagazine> BooksMagazines { get; set; } = new List<BookMagazine>();
}
```

#### **Category.cs**

**Entité** : Catégories de classification

```csharp
public class Category
{
    [Key]
    public int Id { get; set; }

    [Required, SafeNameValidation(MinLength = 2, MaxLength = 50)]
    public string Name { get; set; } = string.Empty;

    // Relation One-to-Many
    public ICollection<BookMagazine> BooksMagazines { get; set; } = new List<BookMagazine>();
}
```

---

### **🔐 Gestion des Fichiers Sécurisée**

#### **FileUuid.cs**

**Rôle** : Gestion des UUID pour fichiers principaux

```csharp
public class FileUuid
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Uuid { get; set; } = string.Empty;
}
```

#### **CoverImageUuid.cs**

**Rôle** : Gestion des UUID pour images de couverture

```csharp
public class CoverImageUuid
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(36)]  // UUID standard
    public string Uuid { get; set; } = string.Empty;
}
```

**Sécurité des fichiers :**

- ✅ **Noms non-prédictibles** : UUID pour tous les fichiers
- ✅ **Prévention énumération** : Impossible de deviner les noms
- ✅ **Traçabilité** : Historique des fichiers uploadés
- ✅ **Nettoyage** : Suppression sécurisée avec UUID

---

### **👥 Interactions Utilisateur**

#### **UserFavorite.cs**

**Rôle** : Système de favoris utilisateur

```csharp
public class UserFavorite
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int BookMagazineId { get; set; }
    public BookMagazine BookMagazine { get; set; } = null!;
}
```

#### **UserReadingHistory.cs**

**Rôle** : Historique de lecture

```csharp
public class UserReadingHistory
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int BookMagazineId { get; set; }
    public BookMagazine BookMagazine { get; set; } = null!;

    public DateTime LastReadDate { get; set; }
}
```

#### **Rating.cs**

**Rôle** : Système de notation (1-5 étoiles)

```csharp
public class Rating
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookMagazineId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, Range(1, 5, ErrorMessage = "La note doit être comprise entre 1 et 5")]
    public int RatingValue { get; set; }

    public DateTime RatingDate { get; set; } = DateTime.Now;

    // Relations
    public BookMagazine BookMagazine { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
```

#### **Comment.cs**

**Rôle** : Système de commentaires avec réponses

```csharp
public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookMagazineId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 1)]
    [DescriptionValidation(MaxLength = 1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CommentDate { get; set; } = DateTime.Now;

    // Support des réponses (commentaires imbriqués)
    public int? ParentCommentId { get; set; }

    // Relations
    public BookMagazine BookMagazine { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public Comment ParentComment { get; set; } = null!;
}
```

**Fonctionnalités commentaires :**

- 💬 **Threading** : Réponses aux commentaires
- 📏 **Validation** : Longueur et contenu sécurisé
- 🕐 **Horodatage** : Date de création automatique
- 🔗 **Relations** : Liens vers livre et utilisateur

---

### **📢 Système de Notifications**

#### **Notification.cs**

**Rôle** : Notifications système

```csharp
public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    [SafeNameValidation(MinLength = 1, MaxLength = 200)]
    public string? Subject { get; set; } = "New Notification";

    [DescriptionValidation(MaxLength = 5000)]
    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; } = false;
}
```

#### **UserNotification.cs**

**Rôle** : Association notifications-utilisateurs

```csharp
public class UserNotification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string? UserId { get; set; }

    [Required]
    public int NotificationId { get; set; }

    public Notification? Notification { get; set; }
    public bool IsSent { get; set; } = false;
}
```

**Fonctionnalités notifications :**

- 📧 **Multi-canal** : Base de données + Email
- 🎯 **Ciblage** : Notifications par utilisateur ou globales
- 📊 **Statut** : Suivi lu/non-lu et envoyé/non-envoyé
- 🕐 **Horodatage** : Traçabilité temporelle

---

### **📊 Audit et Conformité**

#### **AuditLog.cs**

**Rôle** : Traçabilité complète des actions

```csharp
public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(450)] // Compatible ASP.NET Identity
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Métadonnées de sécurité
    [StringLength(45)]
    public string? IpAddress { get; set; }
}
```

#### **AuditActions.cs (Constantes)**

**Actions trackées** :

```csharp
public static class AuditActions
{
    // Authentification
    public const string LOGIN_SUCCESS = "LOGIN_SUCCESS";
    public const string LOGIN_FAILED = "LOGIN_FAILED";
    public const string REGISTER = "REGISTER";

    // Bibliothèque
    public const string BOOK_CREATED = "BOOK_CREATED";
    public const string BOOK_DOWNLOADED = "BOOK_DOWNLOADED";
    public const string BOOK_RATED = "BOOK_RATED";

    // Sécurité
    public const string UNAUTHORIZED_ACCESS = "UNAUTHORIZED_ACCESS";
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

    // Système
    public const string SYSTEM_STARTUP = "SYSTEM_STARTUP";
    public const string SYSTEM_ERROR = "SYSTEM_ERROR";
}
```

---

### **📝 DTOs et Modèles de Transfert**

#### **AuthModels.cs**

**Modèles d'authentification** :

```csharp
public class RegisterModel
{
    [Required, SafeNameValidation(MinLength = 2, MaxLength = 100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StrictEmailValidation]
    public string Email { get; set; } = string.Empty;

    [DescriptionValidation(MaxLength = 500)]
    public string Description { get; set; } = string.Empty;

    [Required, StrictPasswordValidation(
        MinLength = 8, RequireUppercase = true,
        RequireLowercase = true, RequireDigit = true,
        RequireSpecialChar = true)]
    public string Password { get; set; } = string.Empty;
}

public class LoginModel
{
    [Required, StrictEmailValidation]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
```

#### **UpdateProfileModel.cs**

**Mise à jour de profil** :

```csharp
public class UpdateProfileModel
{
    [SafeNameValidation(MinLength = 2, MaxLength = 100)]
    public string? FullName { get; set; }

    [DescriptionValidation(MaxLength = 1000)]
    public string? Description { get; set; }

    [FileValidation(MaxSize = 5MB,
        AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"])]
    public IFormFile? ProfilePicture { get; set; }
}
```

## 🛡️ Système de Validation Avancé

### **Validation Multi-Niveaux**

1. **Attributs** : Validation côté modèle
2. **Filters** : Validation côté contrôleur
3. **Business Logic** : Validation métier
4. **Database** : Contraintes en base

### **Attributs de Validation Personnalisés**

- `[SafeNameValidation]` - Noms et titres sécurisés
- `[FileValidation]` - Validation complète de fichiers
- `[DescriptionValidation]` - Textes longs avec anti-XSS
- `[TagsValidation]` - Système de tags contrôlé
- `[StrictEmailValidation]` - Emails avec format strict
- `[StrictPasswordValidation]` - Mots de passe forts

## 🔗 Relations et Navigation

### **Diagramme des Relations**

```
ApplicationUser (1) ←→ (N) UserFavorite ←→ (1) BookMagazine
ApplicationUser (1) ←→ (N) UserReadingHistory ←→ (1) BookMagazine
ApplicationUser (1) ←→ (N) Rating ←→ (1) BookMagazine
ApplicationUser (1) ←→ (N) Comment ←→ (1) BookMagazine
Author (1) ←→ (N) BookMagazine
Category (1) ←→ (N) BookMagazine
Notification (1) ←→ (N) UserNotification ←→ (1) ApplicationUser
```

### **Navigation Properties Optimisées**

- ✅ **Lazy Loading** désactivé pour performance
- ✅ **Include/ThenInclude** explicite
- ✅ **Select** projections pour éviter over-fetching
- ✅ **AsNoTracking** pour lectures seules

## 🚀 Fonctionnalités Avancées

### **Soft Delete** (Recommandé)

```csharp
public abstract class SoftDeletableEntity
{
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### **Auditable Entities**

```csharp
public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
```

### **Versioning Support**

```csharp
public abstract class VersionedEntity
{
    public int Version { get; set; } = 1;
    public string? ChangeReason { get; set; }
}
```

## 📊 Performance et Optimisation

### **Index Recommandés**

- `BookMagazine.Title` - Recherche textuelle
- `BookMagazine.AuthorId` - Jointures fréquentes
- `UserFavorite.UserId` - Favoris par utilisateur
- `AuditLog.CreatedAt` - Requêtes temporelles
- `Rating.BookMagazineId` - Calculs de notes

### **Contraintes Uniques**

- `FileUuid.Uuid` - Unicité des fichiers
- `CoverImageUuid.Uuid` - Unicité des images
- `Rating(UserId, BookMagazineId)` - Une note par utilisateur/livre

## 🔧 Configuration Entity Framework

### **Fluent API Configuration**

```csharp
// Dans ApplicationDbContext.OnModelCreating()
modelBuilder.Entity<UserFavorite>()
    .HasKey(uf => new { uf.UserId, uf.BookMagazineId });

modelBuilder.Entity<AuditLog>()
    .HasIndex(a => a.CreatedAt)
    .HasDatabaseName("IX_AuditLogs_CreatedAt");
```

### **Seed Data**

- **Rôles** : Admin, User
- **Utilisateur Admin** : admin@library.com
- **Catégories de base** : Fiction, Non-Fiction, Technical

Cette architecture de modèles offre une base solide pour une application en production avec sécurité renforcée, traçabilité complète et performance optimisée.
