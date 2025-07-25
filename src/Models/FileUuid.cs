using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{

    public class FileUuid
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Uuid { get; set; } = string.Empty;

        // Si vous voulez associer cet UUID à un fichier spécifique dans BookMagazine,
        // vous pouvez aussi ajouter une relation avec BookMagazine ici (optionnel)
    }    
}
