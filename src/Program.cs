
using Microsoft.AspNetCore.Mvc;
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

// Importations pour les modèles et contexte de données de l'application
using LibraryAPI.Models;
using LibraryAPI.Data;

// ✅ IMPORTS POUR LA VALIDATION RENFORCÉE
// Importation des filtres de validation personnalisés
using LibraryAPI.Filters;
// Importation des middlewares de validation
using LibraryAPI.Middleware;
// Importation pour les attributs de validation
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.Features;   // Configuration des limitations de formulaires et uploads
using Microsoft.AspNetCore.RateLimiting;    // Services de limitation de taux des requêtes
using System.Threading.RateLimiting;    // Options et algorithmes de limitation (FixedWindow, SlidingWindow, etc.)

// ===== INITIALISATION DE L'APPLICATION =====

// Initialisation du constructeur d'application Web avec les paramètres passés (ici, les arguments d'exécution)
var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURATION DE LA BASE DE DONNÉES =====

// Configuration de la chaîne de connexion à MariaDB
// Ceci ajoute le service de contexte de base de données à l'application, en précisant que nous utilisons MariaDB comme SGBD
// "ApplicationDbContext" est une classe qui représente le contexte de la base de données dans Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    // Configuration pour utiliser MySQL (MariaDB) avec les informations de connexion définies dans appsettings.json sous "MariaDBConnection"
    options.UseMySql(builder.Configuration.GetConnectionString("MariaDBConnection"),
    // Définition de la version spécifique de MySQL/MariaDB utilisée (ici, la version 10.6.4)
    new MySqlServerVersion(new Version(10, 6, 4)))
);

// ===== CONFIGURATION DE L'AUTHENTIFICATION =====

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
        // Clé utilisée pour signer le jeton, encodée en UTF-8, avec vérification de nullité pour éviter les erreurs
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")))
    };
});

// ===== CONFIGURATION CORS =====

// Configuration de CORS avec une politique pour les endpoints publics
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicApiPolicy", builder =>
    {
        // Définit les origines autorisées à accéder à l'API (ici, un site de confiance)
        builder.WithOrigins("https://trustedwebsite.com")
               // Autorise tous les en-têtes HTTP
               .AllowAnyHeader()
               // Autorise toutes les méthodes HTTP (GET, POST, PUT, DELETE, etc.)
               .AllowAnyMethod();
    });
});

// ===== ✅ CONFIGURATION DES CONTRÔLEURS AVEC VALIDATION RENFORCÉE =====

// Ajout des services de contrôleurs API à l'application avec validation renforcée
// Cela permet à l'application de reconnaître et gérer les requêtes HTTP dirigées vers les points de terminaison définis dans les contrôleurs
builder.Services.AddControllers(options =>
{
    // ✅ AJOUT DES FILTRES DE VALIDATION GLOBALEMENT
    // Filtre pour valider automatiquement les modèles avant l'exécution des actions
    options.Filters.Add<ModelValidationFilter>();
    // Filtre pour vérifier la sécurité des fichiers uploadés (signatures, noms malveillants, etc.)
    options.Filters.Add<FileValidationFilter>();

    //options.Filters.Add<RateLimitingFilter>();
})
// ✅ PERSONNALISATION DES RÉPONSES D'ERREURS DE VALIDATION
.ConfigureApiBehaviorOptions(options =>
{
    // Personnaliser la réponse des erreurs de validation pour fournir des informations détaillées
    options.InvalidModelStateResponseFactory = context =>
    {
        // Extraction des erreurs de validation du ModelState
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        // Création d'une réponse structurée avec les erreurs de validation
        var response = new
        {
            Message = "Erreurs de validation détectées",
            Errors = errors,
            Timestamp = DateTime.UtcNow,
            TraceId = context.HttpContext.TraceIdentifier
        };

        // Retour d'une réponse HTTP 400 (Bad Request) avec les détails des erreurs
        return new BadRequestObjectResult(response);
    };
});

// builder.Services.AddEndpointsApiExplorer();


// ===== ✅ CONFIGURATION DES UPLOADS DE FICHIERS =====

// Configuration des limitations pour les uploads de fichiers
builder.Services.Configure<FormOptions>(options =>
{
    // Taille maximale autorisée pour les fichiers uploadés (100MB)
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
    // Longueur maximale des valeurs dans les formulaires (illimitée pour les gros fichiers)
    options.ValueLengthLimit = int.MaxValue;
    // Nombre maximal de champs dans un formulaire (illimité)
    options.ValueCountLimit = int.MaxValue;
    // Longueur maximale des clés de formulaire (illimitée)
    options.KeyLengthLimit = int.MaxValue;
});

// ===== CONFIGURATION DE SWAGGER =====

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

// ===== SERVICES SUPPLÉMENTAIRES =====

// Ajout de EmailService pour l'injection de dépendance (service pour envoyer des emails de notification)
builder.Services.AddScoped<EmailService>();

// Configuration des politiques de limitation de taux pour protéger l'API contre les abus
// Le rate limiting permet de contrôler le nombre de requêtes par utilisateur/IP dans un intervalle de temps donné
builder.Services.AddRateLimiter(options =>
{
    // Politique globale - pour la plupart des endpoints
    // Utilise l'algorithme "Fixed Window" : un compteur fixe qui se remet à zéro à intervalles réguliers
    options.AddFixedWindowLimiter("GlobalPolicy", (FixedWindowRateLimiterOptions opt) =>
    {
        opt.PermitLimit = 200;        // 200 requêtes autorisées par fenêtre
        opt.Window = TimeSpan.FromMinutes(1);  // Fenêtre de 1 minute
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;  // Traiter les requêtes en file d'attente dans l'ordre d'arrivée
        opt.QueueLimit = 50;          // Maximum 50 requêtes en file d'attente
    });

    // Politique stricte - pour auth et actions sensibles
    // Plus restrictive pour les endpoints critiques (login, register, reset password, etc.)
    options.AddFixedWindowLimiter("StrictPolicy", (FixedWindowRateLimiterOptions opt) =>
    {
        opt.PermitLimit = 10;         // Seulement 10 requêtes par minute
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 5;           // File d'attente réduite pour les actions sensibles
    });

    // Politique upload - très restrictive
    // Protection contre les uploads massifs qui peuvent surcharger le serveur
    options.AddFixedWindowLimiter("UploadPolicy", (FixedWindowRateLimiterOptions opt) =>
    {
        opt.PermitLimit = 3;          // Seulement 3 uploads autorisés
        opt.Window = TimeSpan.FromMinutes(15);  // Sur une fenêtre de 15 minutes
        opt.QueueLimit = 2;           // File d'attente très limitée
    });

    // Politique publique - pour API publique
    // Plus permissive pour les endpoints publics (consultation, recherche, etc.)
    options.AddFixedWindowLimiter("PublicPolicy", (FixedWindowRateLimiterOptions opt) =>
    {
        opt.PermitLimit = 1000;       // Plus permissif pour les consultations publiques
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 100;         // File d'attente plus importante
    });

    // Gestion personnalisée des rejets de requêtes
    // Définit la réponse renvoyée quand la limite est atteinte
    options.OnRejected = async (context, token) =>
    {
        // Statut HTTP 429 "Too Many Requests"
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";

        // Création d'une réponse JSON informative
        var response = new
        {
            Message = "Trop de requêtes. Veuillez réessayer plus tard.",
            RetryAfter = "60 seconds",    // Indication du délai avant de pouvoir réessayer
            Timestamp = DateTime.UtcNow   // Horodatage pour le debugging
        };
        
        // Sérialisation et envoi de la réponse JSON au client
        await context.HttpContext.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(response), token);
    };
});

// ===== CONSTRUCTION DE L'APPLICATION =====

// Construction de l'application avec tous les services configurés précédemment
var app = builder.Build();

// ===== CONFIGURATION DU PIPELINE HTTP =====

// Force la redirection HTTPS pour toutes les requêtes HTTP
app.UseHttpsRedirection();

// ===== ✅ MIDDLEWARES DE VALIDATION RENFORCÉE =====

// Middleware de gestion des exceptions de validation
// Capture et gère automatiquement les exceptions de validation lancées dans l'application
app.UseMiddleware<ValidationExceptionMiddleware>();

// Middleware de logging des validations (uniquement en développement pour éviter les logs excessifs en production)
if (app.Environment.IsDevelopment())
{
    // Log toutes les tentatives de validation échouées pour le débogage
    app.UseMiddleware<ValidationLoggingMiddleware>();
}




// ===== CONFIGURATION HTTPS ET SÉCURITÉ =====

// Optionnel : Forcer HTTPS en développement avec HTTP Strict Transport Security
if (app.Environment.IsDevelopment())
{
    app.UseHsts(); // HTTP Strict Transport Security
}

// ===== FICHIERS STATIQUES =====

// Permet de servir les fichiers statiques depuis le dossier wwwroot (images, CSS, JS, fichiers uploadés)
app.UseStaticFiles();

// ===== CONFIGURATION SWAGGER =====

// Configuration pour activer Swagger selon la configuration (évite d'exposer la documentation en production par défaut)
if (builder.Configuration.GetValue<bool>("EnableSwagger"))
{
    // Active Swagger pour générer la documentation API
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Définition de l'URL où accéder à la documentation de l'API (fichier JSON Swagger)
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryApi v1");
    });
}

// ===== ACTIVATION DES POLITIQUES =====

// Activation de la politique CORS pour les endpoints publics (permet l'accès depuis des domaines externes autorisés)
app.UseCors("PublicApiPolicy");

// Active l'authentification dans le pipeline des requêtes HTTP (vérifie les tokens JWT)
app.UseAuthentication();

// Active l'autorisation (vérification des droits d'accès aux ressources) dans le pipeline des requêtes HTTP
app.UseAuthorization();

// ===== ACTIVATION DU RATE LIMITING DANS LE PIPELINE =====

// Active le middleware de limitation de taux dans le pipeline de traitement des requêtes
// Doit être placé après UseAuthentication() et UseAuthorization() pour identifier l'utilisateur
// mais avant MapControllers() pour intercepter les requêtes vers les contrôleurs
app.UseRateLimiter();

// ===== MAPPING DES ROUTES =====

// Mappage des contrôleurs API pour gérer les requêtes HTTP et les rediriger vers les contrôleurs appropriés
app.MapControllers();

// Route par défaut pour vérifier que l'API fonctionne
app.MapGet("/", () => "Library API is running! Go to /swagger for documentation.");

// ===== INITIALISATION DES DONNÉES =====

// Initialisation des rôles et de l'utilisateur admin au démarrage pour BDD vide
using (var scope = app.Services.CreateScope())
{
    // Récupération des services de gestion des rôles et utilisateurs via l'injection de dépendances
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
        // Création d'un utilisateur administrateur par défaut si aucun n'existe
        var user = new ApplicationUser
        {
            UserName = "admin@library.com",
            Email = "admin@library.com",
            FullName = "Admin",
            Description = "Administrator Account",
            ProfilePicture = null, // Champ nullable
            EmailConfirmed = true // Confirmer l'email directement pour éviter les étapes de vérification
        };

        // Tentative de création de l'utilisateur avec un mot de passe par défaut
        var result = await userManager.CreateAsync(user, "AdminPass123!");
        if (result.Succeeded)
        {
            // Assigner le rôle Admin à l'utilisateur créé
            await userManager.AddToRoleAsync(user, "Admin");
            Console.WriteLine("Admin user created: admin@library.com / AdminPass123!");
        }
        else
        {
            // Affichage des erreurs en cas d'échec de création
            Console.WriteLine("Failed to create admin user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.Description}");
            }
        }
    }
}

// ===== LANCEMENT DE L'APPLICATION =====

// Lancement de l'application (écoute des requêtes entrantes)
// Cette méthode bloque le thread principal et attend les requêtes HTTP
app.Run();