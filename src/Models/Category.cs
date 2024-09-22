using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    // Relation avec BookMagazine (une catégorie peut avoir plusieurs livres/magazines)
    public ICollection<BookMagazine> BooksMagazines { get; set; }
}
