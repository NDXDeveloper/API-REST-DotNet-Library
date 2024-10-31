// Importation du namespace pour ASP.NET Core Identity.
// Identity permet de gérer les utilisateurs, leurs rôles, la connexion, les claims, etc.
using Microsoft.AspNetCore.Identity;

namespace LibraryAPI.Models
{
    // Déclaration de la classe ApplicationUser, qui étend (hérite de) IdentityUser.
    // IdentityUser est la classe de base fournie par ASP.NET Core Identity pour gérer les utilisateurs.
    // Cette classe inclut des propriétés de base comme UserName, Email, PasswordHash, etc.
    // ApplicationUser permet d'ajouter des propriétés supplémentaires spécifiques à ton application.
    public class ApplicationUser : IdentityUser
    {
        // Propriété FullName : permet de stocker le nom complet de l'utilisateur.
        // Le point d'interrogation (?) indique que cette propriété est nullable, c'est-à-dire
        // qu'elle peut ne pas avoir de valeur (null) si l'utilisateur ne fournit pas de nom complet.
        public string? FullName { get; set; }
        // Propriété Description : permet de stocker une description personnelle ou une biographie
        // de l'utilisateur. Elle est également nullable.
        public string? Description { get; set; }
        // Propriété ProfilePicture : permet de stocker l'URL d'une image de profil associée à l'utilisateur.
        // Cette propriété est aussi nullable, donc l'utilisateur peut ne pas avoir d'image de profil.
        public string? ProfilePicture { get; set; }
        // Ajout de cette propriété pour gérer l'upload de l'image de profil
        //public IFormFile? ProfilePicture { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 


    }
}

