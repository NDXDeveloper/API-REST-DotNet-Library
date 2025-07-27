using System;
using System.ComponentModel.DataAnnotations;
using LibraryAPI.Models.Validation;

namespace LibraryAPI.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookMagazineId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;  // ID de l'utilisateur qui a laissé le commentaire

        [Required(ErrorMessage = "Le contenu du commentaire est obligatoire")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Le commentaire doit faire entre 1 et 1000 caractères")]
        [DescriptionValidation(MaxLength = 1000)]
        public string Content { get; set; } = string.Empty;  // Contenu du commentaire

        public DateTime CommentDate { get; set; } = DateTime.Now;  // Date du commentaire

        public int? ParentCommentId { get; set; }  // ID du commentaire parent (si c'est une réponse)

        // Relations avec BookMagazine, ApplicationUser et le commentaire parent
        public BookMagazine BookMagazine { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public Comment ParentComment { get; set; } = null!;
    }
}