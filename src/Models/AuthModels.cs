using System.ComponentModel.DataAnnotations;
using LibraryAPI.Models.Validation;

namespace LibraryAPI.Models
{

    // Classe utilisée pour le modèle de données lors de l'enregistrement d'un utilisateur.
    // Cette classe sera utilisée pour recevoir les informations envoyées par le client (comme un formulaire de registration) lors de la création d'un nouveau compte utilisateur
    
    public class RegisterModel
    {
        [Required(ErrorMessage = "Le nom complet est obligatoire")]
        [SafeNameValidation(MinLength = 2, MaxLength = 100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [StrictEmailValidation]
        public string Email { get; set; } = string.Empty;

        [DescriptionValidation(MaxLength = 500)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [StrictPasswordValidation(
            MinLength = 8,
            MaxLength = 128,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialChar = true
        )]
        public string Password { get; set; } = string.Empty;
    }

    // Classe utilisée pour le modèle de données lors de la connexion d'un utilisateur.
    // Cette classe sera utilisée pour recevoir les informations envoyées par le client (comme un formulaire de login) lors de la tentative de connexion d'un utilisateur.

    public class LoginModel
    {
        [Required(ErrorMessage = "L'email est obligatoire")]
        [StrictEmailValidation]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [StringLength(128, MinimumLength = 1, ErrorMessage = "Le mot de passe est requis")]
        public string Password { get; set; } = string.Empty;
    }
}