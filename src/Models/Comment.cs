using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{

    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookMagazineId { get; set; }

        [Required]
        public string UserId { get; set; }  // ID de l'utilisateur qui a laissé le commentaire

        [Required]
        public string Content { get; set; }  // Contenu du commentaire

        public DateTime CommentDate { get; set; } = DateTime.Now;  // Date du commentaire

        public int? ParentCommentId { get; set; }  // ID du commentaire parent (si c'est une réponse)

        // Relations avec BookMagazine, ApplicationUser et le commentaire parent
        public BookMagazine BookMagazine { get; set; }
        public ApplicationUser User { get; set; }
        public Comment ParentComment { get; set; }
    }
    
}
