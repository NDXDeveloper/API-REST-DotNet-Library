# Data

Ce dossier contient le fichier de configuration de la base de données de l'application.

- **ApplicationDbContext.cs** : Définit le contexte de la base de données de l'application en utilisant Entity Framework Core. `ApplicationDbContext` gère les interactions avec la base de données pour les entités de l'application, comme `User`, `BookMagazine`, `Category`, et autres. Il est également utilisé dans les contrôleurs pour effectuer des opérations CRUD (Create, Read, Update, Delete).

Ce contexte permet la configuration des relations entre les entités et la génération de migrations pour créer et maintenir le schéma de la base de données.

---

Ce dossier contient la configuration de la couche d'accès aux données de l'application LibraryAPI, basée sur Entity Framework Core avec une architecture robuste et sécurisée.

## 🏗️ Architecture de la Base de Données

### **ApplicationDbContext.cs**

**Classe principale** qui hérite de `IdentityDbContext<ApplicationUser>` pour intégrer ASP.NET Core Identity avec Entity Framework Core.

## 📊 Tables et Entités Configurées

### **Tables Identity (ASP.NET Core)**

- `AspNetUsers` - Utilisateurs étendus (ApplicationUser)
- `AspNetRoles` - Rôles (Admin, User)
- `AspNetUserRoles` - Association utilisateurs-rôles
- `AspNetUserClaims` - Claims personnalisés
- `AspNetUserLogins` - Connexions externes
- `AspNetUserTokens` - Tokens de sécurité

### **Entités Métier Principales**

#### **Bibliothèque et Contenu**

```csharp
public DbSet<BookMagazine> BooksMagazines { get; set; }
public DbSet<Author> Authors { get; set; }
public DbSet<Category> Categories { get; set; }
```

#### **Gestion des Fichiers**

```csharp
public DbSet<FileUuid> FileUuids { get; set; }
public DbSet<CoverImageUuid> CoverImageUuids { get; set; }
```

- **Unicité garantie** : Gestion des UUID pour éviter les conflits de noms
- **Sécurité** : Noms de fichiers non prédictibles
- **Traçabilité** : Historique des fichiers uploadés

#### **Interactions Utilisateur**

```csharp
public DbSet<UserFavorite> UserFavorites { get; set; }
public DbSet<UserReadingHistory> UserReadingHistory { get; set; }
public DbSet<Rating> Ratings { get; set; }
public DbSet<Comment> Comments { get; set; }
```

#### **Système de Notifications**

```csharp
public DbSet<Notification> Notifications { get; set; }
public DbSet<UserNotification> UserNotifications { get; set; }
```

#### **Audit et Conformité**

```csharp
public DbSet<AuditLog> AuditLogs { get; set; }
```

## 🔧 Configuration Avancée (`OnModelCreating`)

### **Clés Primaires Composites**

```csharp
// Favoris utilisateur
modelBuilder.Entity<UserFavorite>()
    .HasKey(uf => new { uf.UserId, uf.BookMagazineId });

// Historique de lecture
modelBuilder.Entity<UserReadingHistory>()
    .HasKey(urh => new { urh.UserId, urh.BookMagazineId });
```

### **Index Optimisés pour Performance**

```csharp
// Audit logs - optimisation des requêtes temporelles
modelBuilder.Entity<AuditLog>()
    .HasIndex(a => a.CreatedAt)
    .HasDatabaseName("IX_AuditLogs_CreatedAt");

// Audit logs - requêtes par utilisateur et date
modelBuilder.Entity<AuditLog>()
    .HasIndex(a => new { a.UserId, a.CreatedAt })
    .HasDatabaseName("IX_AuditLogs_UserId_CreatedAt");
```

## 🗄️ Structure de Base de Données

### **Relations Principales**

#### **BookMagazine** (Centre du modèle)

- **Many-to-One** avec `Author` (AuthorId)
- **Many-to-One** avec `Category` (CategoryId)
- **One-to-Many** avec `Rating`, `Comment`
- **Many-to-Many** avec `User` via `UserFavorite`, `UserReadingHistory`

#### **ApplicationUser** (Utilisateur étendu)

- **One-to-Many** avec `Rating`, `Comment`, `UserNotification`
- **Many-to-Many** avec `BookMagazine` via `UserFavorite`, `UserReadingHistory`

#### **Audit et Notifications**

- **AuditLog** : Traçabilité de toutes les actions
- **Notification** → **UserNotification** : Système de diffusion

### **Contraintes et Validations**

#### **Intégrité Référentielle**

- ✅ **Foreign Keys** avec `CASCADE DELETE` approprié
- ✅ **Clés composites** pour éviter les doublons métier
- ✅ **Index uniques** sur les UUID de fichiers

#### **Validation au Niveau Base**

- ✅ **NOT NULL** sur les champs critiques
- ✅ **String Length** limitations configurées
- ✅ **Range** validations pour ratings (1-5)

## 🔍 Optimisations de Performance

### **Index Stratégiques**

- **Audit Logs** : Requêtes temporelles et par utilisateur
- **BookMagazines** : Recherche par titre, auteur, catégorie
- **UserFavorites/History** : Accès rapide par utilisateur

### **Requêtes Optimisées**

- **Include/ThenInclude** : Chargement eager des relations
- **Select** projections : Limitation des données transférées
- **AsNoTracking** : Performance en lecture seule

## 💾 Configuration Base de Données

### **Provider MariaDB/MySQL**

```csharp
options.UseMySql(connectionString,
    new MySqlServerVersion(new Version(10, 6, 4)))
```

### **Gestion des Migrations**

```bash
# Créer une migration
dotnet ef migrations add NomMigration

# Appliquer les migrations
dotnet ef database update

# Générer script SQL
dotnet ef migrations script
```

### **Seed Data** (Initialisation automatique)

- **Rôles** : Admin, User (création automatique)
- **Utilisateur Admin** : admin@library.com / AdminPass123!
- **Catégories** : Création à la demande lors d'uploads

## 🛡️ Sécurité et Conformité

### **Protection des Données**

- ✅ **Chiffrement** : Mots de passe hashés (Identity)
- ✅ **UUID** : Fichiers non énumérables
- ✅ **Audit Trail** : Traçabilité complète RGPD
- ✅ **Soft Delete** : Possibilité de récupération

### **Conformité RGPD**

- **AuditLog** : Historique des actions utilisateur
- **Droit à l'oubli** : Mécanismes de suppression
- **Portabilité** : Export des données utilisateur
- **Traçabilité** : Qui fait quoi, quand

## 📈 Monitoring et Maintenance

### **Surveillance Base de Données**

- **Health Checks** : Connectivité et performance
- **Métriques** : Nombre d'entités, croissance
- **Logs** : Requêtes lentes et erreurs

### **Maintenance Préventive**

- **Index** : Analyse et optimisation régulière
- **Statistiques** : Mise à jour des stats MySQL
- **Backup** : Stratégie de sauvegarde automatisée
- **Purge** : Nettoyage des logs anciens

## 🚀 Évolutions Recommandées

### **Nouvelles Fonctionnalités**

- **Versioning** : Historique des modifications de livres
- **Tags** : Système de tags avancé avec table dédiée
- **Collections** : Regroupements personnalisés
- **Social** : Partage et recommandations

### **Optimisations Avancées**

- **Read Replicas** : Séparation lecture/écriture
- **Partitioning** : Partition des logs par date
- **Cache** : Redis pour requêtes fréquentes
- **Search Engine** : Elasticsearch pour recherche full-text

### **Monitoring Avancé**

- **Query Performance** : Surveillance des requêtes lentes
- **Deadlock Detection** : Prévention des blocages
- **Growth Tracking** : Suivi de la croissance des données
- **Automated Alerts** : Alertes sur métriques critiques

## 🔧 Configuration de Développement

### **Connection String Locale**

```json
{
  "ConnectionStrings": {
    "MariaDBConnection": "server=localhost;port=3306;database=librarydb;user=dev;password=devpass;"
  }
}
```

### **Variables d'Environnement Production**

```bash
MYSQLHOST=production-host
MYSQLPORT=3306
MYSQLDATABASE=library_prod
MYSQLUSER=app_user
MYSQLPASSWORD=secure_password
```

La configuration actuelle offre une base solide pour une application en production avec une séparation claire des responsabilités, une sécurité renforcée et une observabilité complète pour la maintenance et l'évolution.
