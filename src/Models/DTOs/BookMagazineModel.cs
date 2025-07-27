using System.ComponentModel.DataAnnotations;
using LibraryAPI.Models.Validation;

namespace LibraryAPI.Models
{
    public class BookMagazineModel
    {
        [Required(ErrorMessage = "Le titre est obligatoire")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le titre doit faire entre 2 et 200 caractères")]
        [SafeNameValidation(MinLength = 2, MaxLength = 200)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "L'auteur est obligatoire")]
        [SafeNameValidation(MinLength = 2, MaxLength = 100)]
        public string? Author { get; set; }

        [DescriptionValidation(MaxLength = 2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "La catégorie est obligatoire")]
        [SafeNameValidation(MinLength = 2, MaxLength = 50)]
        public string? Category { get; set; }

        [TagsValidation(MaxTags = 10, MaxTagLength = 30)]
        public string? Tags { get; set; }

        [Required(ErrorMessage = "Le fichier est obligatoire")]
        [FileValidation(
            MaxSize = 100 * 1024 * 1024, // 100MB
            AllowedExtensions = new[] { ".pdf", ".epub", ".mobi", ".txt", ".doc", ".docx" },
            AllowedMimeTypes = new[] {
                "application/pdf",
                "application/epub+zip",
                "application/x-mobipocket-ebook",
                "text/plain",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            }
        )]
        public IFormFile? File { get; set; }

        [FileValidation(
            MaxSize = 10 * 1024 * 1024, // 10MB
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
            AllowedMimeTypes = new[] {
                "image/jpeg",
                "image/png",
                "image/gif",
                "image/webp"
            }
        )]
        public IFormFile? CoverImage { get; set; }
    }
}