namespace LibraryAPI.Models
{

    public class UserReadingHistory
    {
        public string UserId { get; set; }  // ID de l'utilisateur
        public ApplicationUser User { get; set; }  // Référence à l'utilisateur

        public int BookMagazineId { get; set; }  // ID du livre ou magazine
        public BookMagazine BookMagazine { get; set; }  // Référence au livre ou magazine

        public DateTime LastReadDate { get; set; }  // Date de la dernière consultation
    }    
}

