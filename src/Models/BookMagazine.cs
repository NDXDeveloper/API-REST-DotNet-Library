using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibraryAPI.Models.Validation;

namespace LibraryAPI.Models
{
    public class BookMagazine
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le titre est obligatoire")]
        [SafeNameValidation(MinLength = 2, MaxLength = 200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public int AuthorId { get; set; }  // Foreign key vers l'auteur
        [ForeignKey("AuthorId")]
        public Author Author { get; set; } = null!;

        [DescriptionValidation(MaxLength = 2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }  // Foreign key vers la catégorie
        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        [TagsValidation(MaxTags = 10, MaxTagLength = 30)]
        public string Tags { get; set; } = string.Empty;

        [Required]
        [StringLength(500, ErrorMessage = "Le chemin de fichier ne peut dépasser 500 caractères")]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Le chemin d'image ne peut dépasser 500 caractères")]
        public string CoverImagePath { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.Now;

        // champ pour suivre le nombre de vues
        [Range(0, int.MaxValue, ErrorMessage = "Le nombre de vues doit être positif")]
        public int ViewCount { get; set; } = 0;

        // compteur de téléchargements
        [Range(0, int.MaxValue, ErrorMessage = "Le nombre de téléchargements doit être positif")]
        public int DownloadCount { get; set; } = 0;

        // le nom original du fichier
        [SafeNameValidation(MinLength = 1, MaxLength = 255)]
        public string OriginalFileName { get; set; } = string.Empty;

        // le nom original de l'image de couverture
        [SafeNameValidation(MinLength = 1, MaxLength = 255)]
        public string OriginalCoverImageName { get; set; } = string.Empty;

        // la note moyenne
        [Range(0.0, 5.0, ErrorMessage = "La note moyenne doit être entre 0 et 5")]
        public double AverageRating { get; set; }
    }
}