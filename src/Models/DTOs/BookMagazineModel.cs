using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

public class BookMagazineModel
{
    [Required]
    public string? Title { get; set; }

    [Required]
    public string? Author { get; set; }  // L'auteur est désormais un champ texte

    public string? Description { get; set; }

    [Required]
    public string? Category { get; set; }  // La catégorie est désormais un champ texte

    public string? Tags { get; set; }

    [Required]
    public IFormFile? File { get; set; }

    public IFormFile? CoverImage { get; set; }  // Optionnel, image de couverture
}
