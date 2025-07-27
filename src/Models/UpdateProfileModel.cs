using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using LibraryAPI.Models.Validation;

namespace LibraryAPI.Models
{

    // Classe utilisée pour le modèle de données lors de la mise à jour du profil utilisateur.
    // Cette classe est utilisée pour recevoir les informations que l'utilisateur souhaite modifier dans son profil.
    // Les champs sont optionnels (nullable), donc l'utilisateur peut choisir de ne mettre à jour que certains champs.

    public class UpdateProfileModel
    {
        [SafeNameValidation(MinLength = 2, MaxLength = 100)]
        public string? FullName { get; set; }

        [DescriptionValidation(MaxLength = 1000)]
        public string? Description { get; set; }

        [FileValidation(
            MaxSize = 5 * 1024 * 1024, // 5MB
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
            AllowedMimeTypes = new[] {
                "image/jpeg",
                "image/png",
                "image/gif",
                "image/webp"
            }
        )]
        public IFormFile? ProfilePicture { get; set; }
    }
}