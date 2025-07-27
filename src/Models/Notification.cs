using System;
using System.ComponentModel.DataAnnotations;
using LibraryAPI.Models.Validation;

namespace LibraryAPI.Models
{
    public class Notification  // Ce modèle représente une notification dans le système, stockant le sujet de la notification, le contenu de la notification, la date de création, et son statut (lue ou non).
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "L'objet est obligatoire")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "L'objet doit faire entre 1 et 200 caractères")]
        [SafeNameValidation(MinLength = 1, MaxLength = 200)]
        public string? Subject { get; set; } = "New Notification";

        [DescriptionValidation(MaxLength = 5000)]
        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}