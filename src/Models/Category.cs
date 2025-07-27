using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LibraryAPI.Models.Validation;

namespace LibraryAPI.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de la catégorie est obligatoire")]
        [SafeNameValidation(MinLength = 2, MaxLength = 50)]
        public string Name { get; set; } = string.Empty;

        // Relation avec BookMagazine (une catégorie peut avoir plusieurs livres/magazines)
        public ICollection<BookMagazine> BooksMagazines { get; set; } = new List<BookMagazine>();
    }
}