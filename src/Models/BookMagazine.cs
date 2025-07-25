using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models
{

    public class BookMagazine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public int AuthorId { get; set; }  // Foreign key vers l'auteur
        [ForeignKey("AuthorId")]
        public Author Author { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }  // Foreign key vers la catégorie
        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        public string Tags { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public string CoverImagePath { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.Now;

        // Nouveau champ pour suivre le nombre de vues
        public int ViewCount { get; set; } = 0;  // Initialisé à 0

        // Ou si vous préférez un compteur de téléchargements
        public int DownloadCount { get; set; } = 0;  // Initialisé à 0

        // Ajout d'une nouvelle propriété pour stocker le nom original du fichier
        public string OriginalFileName { get; set; } = string.Empty;

        public string OriginalCoverImageName { get; set; }  = string.Empty; // Stocker le nom original de l'image de couverture

        public double AverageRating { get; set; }  // Nouveau champ pour stocker la note moyenne


    }
    
}
