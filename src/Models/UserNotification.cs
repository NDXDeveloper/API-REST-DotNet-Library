using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{

    public class UserNotification // Ce modèle gère l'association entre une notification et un utilisateur, en gardant un état de notification envoyé ou non.
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; } // ID de l'utilisateur

        [Required]
        public int NotificationId { get; set; } // ID de la notification

        public Notification? Notification { get; set; } // Relation avec Notification

        public bool IsSent { get; set; } = false; // Indicateur si la notification est envoyée
    }
    
}
