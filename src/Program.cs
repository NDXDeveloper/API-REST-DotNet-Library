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

using LibraryAPI.Models;
using LibraryAPI.Data;

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
        //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")))
    };
});


// Configuration de CORS avec une politique pour les endpoints publics
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicApiPolicy", builder =>
    {
        builder.WithOrigins("https://trustedwebsite.com")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
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

// Ajout de EmailService pour l'injection de dépendance
builder.Services.AddScoped<EmailService>();

// Construction de l'application avec tous les services configurés précédemment
var app = builder.Build();

app.UseHttpsRedirection(); // ← Déjà présent

// Optionnel : Forcer HTTPS en développement
if (app.Environment.IsDevelopment())
{
    app.UseHsts(); // HTTP Strict Transport Security
}

app.UseStaticFiles();  // Permet de servir les fichiers statiques depuis wwwroot

// Configuration pour n'activer Swagger que dans l'environnement de développement (en évitant d'exposer la documentation en production)
//if (app.Environment.IsDevelopment())
if (builder.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();  // Active Swagger pour générer la documentation API
    app.UseSwaggerUI(c =>
    {
        // Définition de l'URL où accéder à la documentation de l'API (fichier JSON Swagger)
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryApi v1");
    });
}

// Activation de la politique CORS pour les endpoints publics
app.UseCors("PublicApiPolicy");

// Active l'authentification dans le pipeline des requêtes HTTP
app.UseAuthentication();

// Active l'autorisation (vérification des droits d'accès aux ressources) dans le pipeline des requêtes HTTP
app.UseAuthorization();

// ... tout votre code existant jusqu'à ...

// Mappage des contrôleurs API pour gérer les requêtes HTTP et les rediriger vers les contrôleurs appropriés
app.MapControllers();

app.MapGet("/", () => "Library API is running! Go to /swagger for documentation.");

// Initialisation des rôles et de l'utilisateur admin au démarrage pour BDD vide
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Créer le rôle Admin s'il n'existe pas
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    // Créer le rôle User s'il n'existe pas
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    // Vérifier s'il existe déjà un utilisateur avec le rôle Admin
    var existingAdmins = await userManager.GetUsersInRoleAsync("Admin");
    if (!existingAdmins.Any())
    {
        var user = new ApplicationUser
        {
            UserName = "admin@library.com",
            Email = "admin@library.com",
            FullName = "Admin",
            Description = "Administrator Account",
            ProfilePicture = null, // Champ nullable
            EmailConfirmed = true // Confirmer l'email directement
        };

        var result = await userManager.CreateAsync(user, "AdminPass123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");
            Console.WriteLine("Admin user created: admin@library.com / AdminPass123!");
        }
        else
        {
            Console.WriteLine("Failed to create admin user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.Description}");
            }
        }
    }
}



// Lancement de l'application (écoute des requêtes entrantes)
app.Run();
