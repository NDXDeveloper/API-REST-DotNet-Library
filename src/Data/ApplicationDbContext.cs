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

}
