using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using LibraryAPI.Models.Validation;

namespace LibraryAPI.Models
{
    // Déclaration de la classe ApplicationUser, qui étend (hérite de) IdentityUser.
    // IdentityUser est la classe de base fournie par ASP.NET Core Identity pour gérer les utilisateurs.
    // Cette classe inclut des propriétés de base comme UserName, Email, PasswordHash, etc.
    // ApplicationUser permet d'ajouter des propriétés supplémentaires spécifiques à notre application.
    public class ApplicationUser : IdentityUser
    {
        [SafeNameValidation(MinLength = 2, MaxLength = 100)]
        public string? FullName { get; set; }

        [DescriptionValidation(MaxLength = 1000)]
        public string? Description { get; set; }

        [Url(ErrorMessage = "L'URL de l'image de profil n'est pas valide")]
        [StringLength(500, ErrorMessage = "L'URL de l'image ne peut dépasser 500 caractères")]
        public string? ProfilePicture { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}