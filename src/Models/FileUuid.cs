using System.ComponentModel.DataAnnotations;

public class FileUuid
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Uuid { get; set; }

    // Si vous voulez associer cet UUID à un fichier spécifique dans BookMagazine,
    // vous pouvez aussi ajouter une relation avec BookMagazine ici (optionnel)
}