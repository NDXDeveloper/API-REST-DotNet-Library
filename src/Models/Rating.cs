using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{

    public class Rating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookMagazineId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;  // ID de l'utilisateur qui a donné la note

        [Required]
        [Range(1, 5)]
        public int RatingValue { get; set; }  // La note attribuée (de 1 à 5)

        public DateTime RatingDate { get; set; } = DateTime.Now;  // Date de la note

        // Relations avec BookMagazine et ApplicationUser
        public BookMagazine BookMagazine { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
    
}
