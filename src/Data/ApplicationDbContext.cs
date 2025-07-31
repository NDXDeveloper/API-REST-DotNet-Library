// Importation du namespace nécessaire pour utiliser Identity avec Entity Framework Core.
// Identity permet la gestion des utilisateurs, rôles, connexions, etc.
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
// Importation du namespace pour Entity Framework Core qui permet l'accès aux bases de données.
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Models;

namespace LibraryAPI.Data
{

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

        // Nouvelle table pour stocker les UUIDs générés
        public DbSet<FileUuid> FileUuids { get; set; }

        public DbSet<CoverImageUuid> CoverImageUuids { get; set; }  // Nouvelle table pour les UUID des images

        // Ajout des tables pour les notes et commentaires
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Comment> Comments { get; set; }

        // Notifications 
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        
        public DbSet<AuditLog> AuditLogs { get; set; }

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


            // Ajout d'index sur les colonnes utilisées fréquemment dans les recherches
            // modelBuilder.Entity<BookMagazine>()
            //     .HasIndex(b => b.Title)
            //     .HasDatabaseName("IX_BooksMagazines_Title");

            // modelBuilder.Entity<BookMagazine>()
            //     .HasIndex(b => b.Description)
            //     .HasDatabaseName("IX_BooksMagazines_Description");

            // modelBuilder.Entity<BookMagazine>()
            //     .HasIndex(b => b.Tags)
            //     .HasDatabaseName("IX_BooksMagazines_Tags");

            // // Index sur la relation avec l'Author
            // modelBuilder.Entity<BookMagazine>()
            //     .HasIndex(b => b.AuthorId)
            //     .HasDatabaseName("IX_BooksMagazines_AuthorId");    


            // Configuration pour ajouter un index unique sur la colonne 'Uuid' de la table 'CoverImageUuids'
            // modelBuilder.Entity<CoverImageUuid>()
            //     .HasIndex(c => c.Uuid)
            //     .IsUnique();  // Assurer l'unicité

            // Configuration d'un index unique sur la colonne Uuid dans la table FileUuid
            // modelBuilder.Entity<FileUuid>()
            // .HasIndex(f => f.Uuid)
            // .IsUnique();

            // Index pour optimiser les requêtes d'audit
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt)
                .HasDatabaseName("IX_AuditLogs_CreatedAt");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.UserId, a.CreatedAt })
                .HasDatabaseName("IX_AuditLogs_UserId_CreatedAt");


        }
    }

}