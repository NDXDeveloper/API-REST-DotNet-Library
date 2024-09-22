// Classe utilisée pour le modèle de données lors de l'enregistrement d'un utilisateur.
// Cette classe sera utilisée pour recevoir les informations envoyées par le client (comme un formulaire de registration) lors de la création d'un nouveau compte utilisateur.

public class RegisterModel
{
    // Propriété FullName : représente le nom complet de l'utilisateur qui sera enregistré.
    // C'est un champ requis pour l'enregistrement.
    public string FullName { get; set; }

    // Propriété Email : représente l'email de l'utilisateur qui sera utilisé comme identifiant unique pour la connexion.
    // C'est un champ requis pour l'enregistrement.
    public string Email { get; set; }

    // Propriété Description : permet à l'utilisateur de fournir une description personnelle ou une biographie lors de l'enregistrement.
    // C'est un champ optionnel pour l'enregistrement.
    public string Description { get; set; }

    // Propriété Password : représente le mot de passe de l'utilisateur. Ce mot de passe sera hashé avant d'être stocké dans la base de données.
    // C'est un champ requis pour l'enregistrement.
    public string Password { get; set; }
}

// Classe utilisée pour le modèle de données lors de la connexion d'un utilisateur.
// Cette classe sera utilisée pour recevoir les informations envoyées par le client (comme un formulaire de login) lors de la tentative de connexion d'un utilisateur.

public class LoginModel
{
    // Propriété Email : représente l'email ou l'identifiant de l'utilisateur.
    // C'est un champ requis pour la connexion.
    public string Email { get; set; }

    // Propriété Password : représente le mot de passe saisi par l'utilisateur pour s'authentifier.
    // C'est un champ requis pour la connexion.
    public string Password { get; set; }
}


