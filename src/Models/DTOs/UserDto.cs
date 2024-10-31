using System;

namespace LibraryAPI.Models
{

    public class UserDto
    {
        public string? Id { get; set; }   // Identifiant de l'utilisateur
        public string? UserName { get; set; } = string.Empty;  // Nom d'utilisateur
        public string? Email { get; set; } = string.Empty;  // Email utilisateur pour contact
        public DateTime? CreatedAt { get; set; }  // Date de création (optionnelle)
        public string? Role { get; set; }  // Rôle (ex. : Admin, Utilisateur)

        // Ajouter des propriétés supplémentaires si nécessaire, comme l'avatar, etc.
    }
   
}
