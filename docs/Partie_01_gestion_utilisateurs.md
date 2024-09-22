 
Pour créer une API REST en .NET qui gère les utilisateurs et intègre Swagger pour la documentation, nous allons utiliser ASP.NET Core avec Entity Framework Core pour interagir avec une base de données MariaDB. Voici les étapes détaillées pour le projet `LibraryApi` du dépot `API-REST-DotNet-Library` :

### Prérequis

1. **.NET SDK** installé sur votre système (version 8.0 ou supérieure).
2. **MariaDB** installé et en cours d'exécution.
3. **Visual Studio Code** ou tout autre éditeur de code de votre choix.

### Étape 1 : Créer le projet

Ouvrez un terminal et exécutez les commandes suivantes pour créer le projet :

```bash
dotnet new webapi -n LibraryApi
cd LibraryApi
```

### Étape 2 : Configurer le fichier .http
Dans le fichier .http de votre projet déclarez la variable qui stocke l'adresse de l'API :

```csharp
// Déclaration d'une variable qui stocke l'adresse de l'API
// Ici, la variable @LibraryApi_HostAddress est définie avec la valeur "http://localhost:5000".
// Cela signifie que l'API est hébergée localement sur le port 5000.
// Cette variable peut être réutilisée dans les requêtes suivantes pour éviter la répétition de l'URL.

@LibraryApi_HostAddress = http://localhost:5000

```


### Étape 3 : Ajouter les dépendances

Ajoutez les packages nécessaires pour Entity Framework Core et Swagger. Exécutez ces commandes dans le terminal :

```bash

dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Swashbuckle.AspNetCore

```

### Étape 4 : Configurer la base de données

Créez un fichier de configuration pour la connexion à la base de données. Dans `appsettings.json`, ajoutez la chaîne de connexion suivante :

```json
{
  "_comment_ConnectionStrings": "Cette section contient la chaîne de connexion à MariaDB.",
  "ConnectionStrings": {
    "MariaDBConnection": "server=localhost;port=3306;database=librarydb;user=myuser;password=mypassword"
  },

  "_comment_Jwt": "Cette section contient les informations de configuration pour les tokens JWT.",
  "Jwt": {
    "Key": "YourSuperSecretKeyWithAtLeast16Chars",
    "Issuer": "LibraryApi",
    "Audience": "LibraryApiUsers",
    "_comment_Key": "Assurez-vous que la clé JWT est suffisamment complexe et secrète.",
    "_comment_Issuer_Audience": "L'Issuer et l'Audience sont utilisés pour valider le token."
  },

  "_comment_Logging": "Cette section configure le logging pour l'application.",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },

  "_comment_AllowedHosts": "Liste des hôtes autorisés à accéder à l'application.",
  "AllowedHosts": "*",

  "_comment_CORS": "Configurer les règles CORS pour permettre ou restreindre l'accès à l'API.",
  "Cors": {
    "AllowedOrigins": [ "http://localhost:3000", "http://example.com" ],
    "AllowCredentials": true
  },



  "_comment_AppSettings": "Paramètres spécifiques à l'application.",
  "AppSettings": {
    "FeatureXEnabled": true,
    "MaxItemsToShow": 100
  }
}
```

Assurez-vous de remplacer `myuser` par le nom de votre utilisateur MariaDB.
Assurez-vous de remplacer `mypassword` par le mot de passe de votre utilisateur MariaDB.

### Étape 5. Configuration de MariaDB dans Program.cs
On configure MariaDB avec EF Core dans le fichier Program.cs pour connecter l'API à la base de données :

```csharp
// Importation des bibliothèques nécessaires pour utiliser Entity Framework Core avec un fournisseur de base de données
using Microsoft.EntityFrameworkCore;
// Importation des bibliothèques nécessaires pour configurer l'authentification via JWT (JSON Web Token) dans une application ASP.NET Core
using Microsoft.AspNetCore.Authentication.JwtBearer;
// Importation des bibliothèques pour gérer la validation des jetons JWT, notamment pour les configurations de sécurité
using Microsoft.IdentityModel.Tokens;
// Importation de System.Text pour encoder les clés de sécurité sous forme de chaînes de caractères (UTF8)
using System.Text;
// Importation de la gestion des identités (utilisateurs, rôles) dans ASP.NET Core via Identity
using Microsoft.AspNetCore.Identity;
// Importation des bibliothèques nécessaires pour configurer Swagger, un outil de documentation d'API
using Microsoft.OpenApi.Models;

// Initialisation du constructeur d'application Web avec les paramètres passés (ici, les arguments d'exécution)
var builder = WebApplication.CreateBuilder(args);

// Configuration de la chaîne de connexion à MariaDB
// Ceci ajoute le service de contexte de base de données à l'application, en précisant que nous utilisons MariaDB comme SGBD
// "ApplicationDbContext" est une classe qui représente le contexte de la base de données dans Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    // Configuration pour utiliser MySQL (MariaDB) avec les informations de connexion définies dans appsettings.json sous "MariaDBConnection"
    options.UseMySql(builder.Configuration.GetConnectionString("MariaDBConnection"),
    // Définition de la version spécifique de MySQL/MariaDB utilisée (ici, la version 10.6.4)
    new MySqlServerVersion(new Version(10, 6, 4)))
);

// Ajout du système d'authentification avec Identity
// "Identity" est un système intégré à ASP.NET Core pour la gestion des utilisateurs et des rôles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    // Stocke les informations des utilisateurs et des rôles dans la base de données via Entity Framework
    .AddEntityFrameworkStores<ApplicationDbContext>()
    // Ajoute des fournisseurs de jetons (utilisés par exemple pour la gestion des tokens de réinitialisation de mot de passe, de vérification des emails, etc.)
    .AddDefaultTokenProviders();

// Configuration de l'authentification via JWT (JSON Web Token)
// Ici, on configure l'application pour qu'elle utilise JWT comme méthode d'authentification
builder.Services.AddAuthentication(options =>
{
    // Définit le schéma d'authentification par défaut pour cette application en tant que JWT
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    // Définit le schéma d'authentification à utiliser en cas de défi d'authentification (par exemple, quand un utilisateur non authentifié tente d'accéder à une ressource protégée)
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
// Configuration spécifique du traitement des jetons JWT pour leur validation
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Exige que l'émetteur du jeton soit validé (pour s'assurer que le jeton provient d'une source de confiance)
        ValidateIssuer = true,
        // Exige que l'audience du jeton soit validée (pour s'assurer que le jeton est destiné à cette application)
        ValidateAudience = true,
        // Exige que la durée de vie du jeton soit validée (pour éviter d'accepter des jetons expirés)
        ValidateLifetime = true,
        // Exige que la clé de signature du jeton soit validée (pour s'assurer que le jeton n'a pas été altéré)
        ValidateIssuerSigningKey = true,
        // Spécifie l'émetteur valide du jeton (généralement l'URL de l'API ou du serveur d'authentification)
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        // Spécifie l'audience valide du jeton (qui doit être le consommateur du jeton, par exemple une application cliente)
        ValidAudience = builder.Configuration["Jwt:Audience"],
        // Clé utilisée pour signer le jeton, encodée en UTF-8
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// Ajout des services de contrôleurs API à l'application
// Cela permet à l'application de reconnaître et gérer les requêtes HTTP dirigées vers les points de terminaison définis dans les contrôleurs
builder.Services.AddControllers();

// Ajout de Swagger pour générer la documentation de l'API
// Swagger génère automatiquement une interface graphique interactive et un fichier JSON décrivant les routes et points de terminaison de l'API
builder.Services.AddSwaggerGen(c =>
{
    // Configuration de la version de Swagger, avec un titre et une version pour l'API
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LibraryApi", Version = "v1" });

    // Configuration de Swagger pour inclure l'authentification JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        // Description de la manière d'utiliser le token JWT (ici, en tant que valeur de l'en-tête Authorization)
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",  // Nom du champ dans l'en-tête HTTP qui contiendra le token JWT
        In = ParameterLocation.Header,  // Le token JWT sera fourni dans l'en-tête de la requête HTTP
        Type = SecuritySchemeType.ApiKey,  // Définit que c'est un schéma de sécurité basé sur un token (API key)
        Scheme = "Bearer"  // Spécifie que nous utilisons le schéma "Bearer" pour la transmission du token
    });

    // Définition de la sécurité requise pour les points de terminaison dans l'API (requiert un token JWT pour certains endpoints)
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"  // Se réfère au schéma de sécurité défini précédemment
                },
                Scheme = "oauth2",  // Précise que le schéma est basé sur OAuth2 pour l'authentification
                Name = "Bearer",
                In = ParameterLocation.Header,  // Précise que le token sera transmis dans l'en-tête
            },
            new List<string>()  // Aucune autorisation spécifique requise (vide)
        }
    });
});

// Construction de l'application avec tous les services configurés précédemment
var app = builder.Build();

app.UseStaticFiles();  // Permet de servir les fichiers statiques depuis wwwroot

// Configuration pour n'activer Swagger que dans l'environnement de développement (en évitant d'exposer la documentation en production)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();  // Active Swagger pour générer la documentation API
    app.UseSwaggerUI(c =>
    {
        // Définition de l'URL où accéder à la documentation de l'API (fichier JSON Swagger)
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryApi v1");
    });
}

// Active l'authentification dans le pipeline des requêtes HTTP
app.UseAuthentication();

// Active l'autorisation (vérification des droits d'accès aux ressources) dans le pipeline des requêtes HTTP
app.UseAuthorization();

// Mappage des contrôleurs API pour gérer les requêtes HTTP et les rediriger vers les contrôleurs appropriés
app.MapControllers();

// Lancement de l'application (écoute des requêtes entrantes)
app.Run();

```

### Étape 6 : Créer la base de données avec Entity Framework
Créons les classes pour le modèle utilisateur (ApplicationUser) et la base de données (ApplicationDbContext).

#### 6.1. Modèle ApplicationUser.cs
Créez un fichier Models/ApplicationUser.cs :

```csharp
// Importation du namespace pour ASP.NET Core Identity.
// Identity permet de gérer les utilisateurs, leurs rôles, la connexion, les claims, etc.
using Microsoft.AspNetCore.Identity;

// Déclaration de la classe ApplicationUser, qui étend (hérite de) IdentityUser.
// IdentityUser est la classe de base fournie par ASP.NET Core Identity pour gérer les utilisateurs.
// Cette classe inclut des propriétés de base comme UserName, Email, PasswordHash, etc.
// ApplicationUser permet d'ajouter des propriétés supplémentaires spécifiques à l'application.
public class ApplicationUser : IdentityUser
{
    // Propriété FullName : permet de stocker le nom complet de l'utilisateur.
    // Le point d'interrogation (?) indique que cette propriété est nullable, c'est-à-dire
    // qu'elle peut ne pas avoir de valeur (null) si l'utilisateur ne fournit pas de nom complet.
    public string? FullName { get; set; }
    // Propriété Description : permet de stocker une description personnelle ou une biographie
    // de l'utilisateur. Elle est également nullable.
    public string? Description { get; set; }
    // Propriété ProfilePicture : permet de stocker l'URL d'une image de profil associée à l'utilisateur.
    // Cette propriété est aussi nullable, donc l'utilisateur peut ne pas avoir d'image de profil.
     public string? ProfilePicture { get; set; }
    // Ajout de cette propriété pour gérer l'upload de l'image de profil
     //public IFormFile? ProfilePicture { get; set; }


}
```

#### 6.2. Créer la classe ApplicationDbContext.cs
Créez un fichier Data/ApplicationDbContext.cs :

```csharp
// Importation du namespace nécessaire pour utiliser Identity avec Entity Framework Core.
// Identity permet la gestion des utilisateurs, rôles, connexions, etc.
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
// Importation du namespace pour Entity Framework Core qui permet l'accès aux bases de données.
using Microsoft.EntityFrameworkCore;

// Déclaration de la classe ApplicationDbContext qui hérite de IdentityDbContext.
// IdentityDbContext est une classe spéciale fournie par ASP.NET Core Identity qui
// étend DbContext (le contexte de base de données d'Entity Framework) et inclut
// toutes les entités nécessaires pour la gestion des utilisateurs, rôles, et autres
// fonctionnalités d'ASP.NET Core Identity. Nous utilisons ApplicationUser comme
// modèle utilisateur personnalisé.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // Constructeur de la classe ApplicationDbContext qui appelle le constructeur de la classe parente (base).
    // Il prend en paramètre un DbContextOptions, qui contient les informations nécessaires pour
    // configurer le contexte, comme la chaîne de connexion à la base de données.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        // Le corps du constructeur est vide ici, car toute la logique de configuration
        // est gérée par la classe de base (IdentityDbContext) et les options passées.
    }
}
```

### Étape 7 : Gestion des utilisateurs : Inscription, Connexion et Rôles

#### 7.1. Créer le contrôleur AuthController.cs
Créez un fichier Controllers/AuthController.cs pour gérer l’inscription, la connexion et la gestion des rôles.

```csharp
using Microsoft.AspNetCore.Mvc;  // Pour gérer les contrôleurs et les actions d'API
using Microsoft.AspNetCore.Identity;  // Pour utiliser Identity (gestion des utilisateurs, rôles, etc.)
using Microsoft.AspNetCore.Authorization;  // Pour gérer les attributs d'autorisation
using System.IdentityModel.Tokens.Jwt;  // Pour manipuler les tokens JWT
using System.Security.Claims;  // Pour créer et gérer les claims dans les tokens JWT
using Microsoft.IdentityModel.Tokens;  // Pour gérer la validation et la signature des tokens JWT
using System.Text;  // Pour encoder les clés de sécurité
using Microsoft.AspNetCore.Http;  // Pour utiliser IFormFile
using System.IO;  // Pour utiliser Path et FileStream

// Attributs de route et API pour lier ce contrôleur à une route "api/Auth"
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    // Déclaration des services utilisés dans le contrôleur
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    // Constructeur pour injecter les dépendances (services)
    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

// Action pour mettre à jour le profil de l'utilisateur connecté
[HttpPut("update-profile")]
[Authorize]
public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileModel model)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return NotFound();

    // Mise à jour du nom et de la description
    if (!string.IsNullOrEmpty(model.FullName))
    {
        user.FullName = model.FullName;
    }

    if (!string.IsNullOrEmpty(model.Description))
    {
        user.Description = model.Description;
    }

    // Gestion du fichier d'image de profil
    if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
    {
        // Définir le chemin où stocker l'image (wwwroot/images/profiles/)
        var imagePath = Path.Combine("wwwroot/images/profiles", $"{user.Id}_{model.ProfilePicture.FileName}");

        // Sauvegarder l'image sur le serveur
        using (var stream = new FileStream(imagePath, FileMode.Create))
        {
            await model.ProfilePicture.CopyToAsync(stream);
        }

        // Stocker le chemin relatif dans la base de données
        user.ProfilePicture = $"/images/profiles/{user.Id}_{model.ProfilePicture.FileName}";
    }

    // Sauvegarder les modifications dans la base de données
    var result = await _userManager.UpdateAsync(user);
    if (result.Succeeded)
    {
        return Ok(new { Message = "Profile updated successfully!", ProfilePictureUrl = user.ProfilePicture });
    }

    return BadRequest(result.Errors);
}


    // Action pour enregistrer un nouvel utilisateur
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        // Création d'un nouvel utilisateur basé sur les données fournies
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Description = model.Description
        };

        // Création de l'utilisateur avec le mot de passe fourni
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Si le rôle "User" n'existe pas, on le crée
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));

            // On assigne le rôle "User" au nouvel utilisateur
            await _userManager.AddToRoleAsync(user, "User");

            // Retourner un message de succès
            return Ok(new { Message = "User registered successfully!" });
        }

        // En cas d'échec de création, retourner les erreurs
        return BadRequest(result.Errors);
    }

    // Action pour connecter un utilisateur et générer un token JWT
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        // Tentative de connexion avec le mot de passe fourni
        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

        if (result.Succeeded)
        {
            // Si la connexion réussit, récupérer l'utilisateur et ses rôles
            var user = await _userManager.FindByEmailAsync(model.Email);
            var roles = await _userManager.GetRolesAsync(user);

            // Générer un token JWT pour l'utilisateur
            var token = GenerateJwtToken(user, roles);
            return Ok(new { Token = token });
        }

        // Si la connexion échoue, retourner une erreur Unauthorized (401)
        return Unauthorized();
    }

    // Action pour déconnecter un utilisateur (supprimer sa session)
    [HttpPost("logout")]
    [Authorize]  // Nécessite que l'utilisateur soit authentifié
    public async Task<IActionResult> Logout()
    {
        // Déconnexion de l'utilisateur
        await _signInManager.SignOutAsync();
        return Ok(new { Message = "Logged out successfully!" });
    }

    // Méthode privée pour générer un token JWT pour un utilisateur
    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        // Création des claims pour le token (Id, Email, Username, et rôles)
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName)
        };

        // Ajout des rôles en tant que claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Récupération de la clé secrète pour signer le token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(3),
            signingCredentials: creds);

        // Création du token avec des informations comme l'émetteur, l'audience, et la durée d'expiration
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### 7.2. Modèles pour l'inscription et la connexion
Créez un fichier Models/AuthModels.cs :

```csharp
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
```

#### 7.3. Gérer les rôles (administrateur)
Dans Program.cs, vous pouvez ajouter un utilisateur administrateur au démarrage si celui-ci n'existe pas :
```csharp
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    var adminUser = await userManager.FindByEmailAsync("admin@library.com");
    if (adminUser == null)
    {
        var user = new ApplicationUser
        {
            UserName = "admin@library.com",
            Email = "admin@library.com",
            FullName = "Admin",
            Description = "Administrator Account"
        };

        await userManager.CreateAsync(user, "AdminPass123!");
        await userManager.AddToRoleAsync(user, "Admin");
    }
}
```

### Étape 8 : Mise à jour du profil utilisateur
Dans le même contrôleur AuthController.cs, ajoute une méthode pour mettre à jour les informations de l'utilisateur.

```csharp
 // Action pour mettre à jour le profil de l'utilisateur connecté
[HttpPut("update-profile")]
[Authorize]
public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileModel model)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return NotFound();

    // Mise à jour du nom et de la description
    if (!string.IsNullOrEmpty(model.FullName))
    {
        user.FullName = model.FullName;
    }

    if (!string.IsNullOrEmpty(model.Description))
    {
        user.Description = model.Description;
    }

    // Gestion du fichier d'image de profil
    if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
    {
        // Définir le chemin où stocker l'image (wwwroot/images/profiles/)
        var imagePath = Path.Combine("wwwroot/images/profiles", $"{user.Id}_{model.ProfilePicture.FileName}");

        // Sauvegarder l'image sur le serveur
        using (var stream = new FileStream(imagePath, FileMode.Create))
        {
            await model.ProfilePicture.CopyToAsync(stream);
        }

        // Stocker le chemin relatif dans la base de données
        user.ProfilePicture = $"/images/profiles/{user.Id}_{model.ProfilePicture.FileName}";
    }

    // Sauvegarder les modifications dans la base de données
    var result = await _userManager.UpdateAsync(user);
    if (result.Succeeded)
    {
        return Ok(new { Message = "Profile updated successfully!", ProfilePictureUrl = user.ProfilePicture });
    }

    return BadRequest(result.Errors);
}

```

Modèle pour la mise à jour du profil à ajouter dans Models:


```csharp
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

```

### Étape 9. Exécuter les migrations et la base de données
Finalement, exécute dans le terminal les commandes suivantes pour créer et mettre à jour la base de données :

```csharp
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Étape 10 : Documentation Swagger
Swagger est déjà intégré dans le fichier Program.cs. Tu peux accéder à la documentation API en accédant à l'URL /swagger une fois le projet exécuté.

Maintenant que tout est configuré, vous pouvez exécuter l'application :

```bash
dotnet run
```

Accédez à `http://localhost:5000/swagger` pour voir la documentation Swagger de votre API.

### Conclusion
Vous avez maintenant une API REST de base pour gérer les utilisateurs, avec des fonctionnalités d'inscription, de connexion, de déconnexion, de mise à jour des profils et de gestion des rôles. Vous pouvez étendre cette API en ajoutant des fonctionnalités supplémentaires pour gérer les livres et les magazines.

N'oubliez pas de tester l'API et de vous assurer que les autorisations fonctionnent correctement, notamment l'accès à certaines routes en fonction des rôles.


