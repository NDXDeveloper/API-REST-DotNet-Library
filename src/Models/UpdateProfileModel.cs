using Microsoft.AspNetCore.Http;  // Ajouter cette directive

// Classe utilisée pour le modèle de données lors de la mise à jour du profil utilisateur.
// Cette classe est utilisée pour recevoir les informations que l'utilisateur souhaite modifier dans son profil.
// Les champs sont optionnels (nullable), donc l'utilisateur peut choisir de ne mettre à jour que certains champs.

public class UpdateProfileModel
{
    // Propriété FullName : représente le nom complet que l'utilisateur souhaite définir ou modifier.
    // Le point d'interrogation (?) indique que cette propriété est nullable, ce qui signifie que l'utilisateur peut ne pas fournir cette information.
    public string? FullName { get; set; }

    // Propriété Description : permet à l'utilisateur de mettre à jour ou ajouter une description personnelle ou une biographie.
    // Elle est également nullable, donc l'utilisateur peut ne pas la remplir.
    public string? Description { get; set; }

    // Propriété ProfilePicture : représente l'URL de l'image de profil que l'utilisateur souhaite ajouter ou mettre à jour.
    // Comme les autres, elle est nullable, ce qui signifie que l'utilisateur peut ne pas changer l'image de profil.
    public IFormFile? ProfilePicture { get; set; }
}
