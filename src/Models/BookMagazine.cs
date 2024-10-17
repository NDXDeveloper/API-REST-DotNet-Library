using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BookMagazine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public int AuthorId { get; set; }  // Foreign key vers l'auteur
    [ForeignKey("AuthorId")]
    public Author Author { get; set; }

    public string Description { get; set; }

    [Required]
    public int CategoryId { get; set; }  // Foreign key vers la catégorie
    [ForeignKey("CategoryId")]
    public Category Category { get; set; }

    public string Tags { get; set; }

    [Required]
    public string FilePath { get; set; }

    public string CoverImagePath { get; set; }

    public DateTime UploadDate { get; set; } = DateTime.Now;

    // Nouveau champ pour suivre le nombre de vues
    public int ViewCount { get; set; } = 0;  // Initialisé à 0

    // Ou si vous préférez un compteur de téléchargements
    public int DownloadCount { get; set; } = 0;  // Initialisé à 0
}
