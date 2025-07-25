using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{

    public class Notification // Ce modèle représente une notification dans le système, stockant le contenu de la notification, la date de création, et son statut (lue ou non).
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Subject { get; set; } = "New Notification"; // Objet de la notification
        public string? Content { get; set; } // Message de la notification

        public DateTime CreatedAt { get; set; } = DateTime.Now; // Date de création

        public bool IsRead { get; set; } = false; // Indicateur si la notification est lue
    }
    
}
