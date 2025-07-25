using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{

    public class CoverImageUuid
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(36)]  // Longueur d'un UUID standard
        public string Uuid { get; set; } = string.Empty;
    }
    
}
