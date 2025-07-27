using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LibraryAPI.Models.Validation;

namespace LibraryAPI.Models
{
    public class Author
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de l'auteur est obligatoire")]
        [SafeNameValidation(MinLength = 2, MaxLength = 100)]
        public string Name { get; set; } = string.Empty;

        // Relation avec BookMagazine (un auteur peut avoir plusieurs livres/magazines)
        public ICollection<BookMagazine> BooksMagazines { get; set; } = new List<BookMagazine>();
    }
}