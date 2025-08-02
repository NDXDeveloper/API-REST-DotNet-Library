using Microsoft.AspNetCore.Mvc;             // Pour gérer les contrôleurs et les actions d'API
using Microsoft.AspNetCore.Identity;        // Pour utiliser Identity (gestion des utilisateurs, rôles, etc.)
using Microsoft.AspNetCore.Authorization;   // Pour gérer les attributs d'autorisation
using System.IdentityModel.Tokens.Jwt;      // Pour manipuler les tokens JWT
using System.Security.Claims;               // Pour créer et gérer les claims dans les tokens JWT
using Microsoft.IdentityModel.Tokens;       // Pour gérer la validation et la signature des tokens JWT
using System.Text;                          // Pour encoder les clés de sécurité
using Microsoft.AspNetCore.Http;            // Pour utiliser IFormFile (upload de fichiers)
using System.IO;                            // Pour utiliser Path et FileStream (gestion de fichiers)
using Microsoft.EntityFrameworkCore;        // Pour utiliser Entity Framework Core et les requêtes de base de données        
using LibraryAPI.Data;                      // Pour utiliser la méthode Include et le contexte ApplicationDbContext
using LibraryAPI.Models;                    // Pour accéder aux modèles de données (ApplicationUser, AuditActions, etc.)
using Microsoft.AspNetCore.RateLimiting;    // Pour la limitation du taux de requêtes (protection contre les abus)

namespace LibraryAPI.Controllers
{
    /// <summary>
    /// CONTRÔLEUR D'AUTHENTIFICATION ET GESTION DES UTILISATEURS
    /// 
    /// Ce contrôleur gère toutes les opérations liées aux utilisateurs :
    /// - Inscription (register)
    /// - Connexion (login) avec génération de tokens JWT
    /// - Déconnexion (logout)
    /// - Mise à jour de profil avec upload d'images
    /// - Gestion administrative des utilisateurs (liste, recherche, etc.)
    /// - Notifications utilisateur
    /// 
    /// LOGS SERILOG (TECHNIQUES UNIQUEMENT) :
    /// - Erreurs techniques (exceptions, problèmes de BDD, etc.)
    /// - Problèmes de performance (opérations lentes)
    /// - Erreurs de configuration (clés JWT manquantes, etc.)
    /// - Problèmes d'infrastructure (fichiers, dossiers, etc.)
    /// 
    /// NOTE : Les logs d'audit (qui fait quoi, quand) sont gérés séparément
    /// </summary>
    [EnableRateLimiting("StrictPolicy")]  // Limitation stricte du taux de requêtes pour sécurité
    [Route("api/[controller]")]           // Route de base : /api/Auth
    [ApiController]                       // Indique que c'est un contrôleur API avec validation automatique
    public class AuthController : ControllerBase
    {
        // ===== SERVICES INJECTÉS PAR DÉPENDANCE =====
        
        /// <summary>
        /// Gestionnaire des utilisateurs fourni par ASP.NET Core Identity
        /// Permet de créer, modifier, supprimer des utilisateurs et gérer leurs mots de passe
        /// </summary>
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Gestionnaire de connexion fourni par ASP.NET Core Identity
        /// Gère les opérations de connexion/déconnexion des utilisateurs
        /// </summary>
        private readonly SignInManager<ApplicationUser> _signInManager;

        /// <summary>
        /// Gestionnaire des rôles fourni par ASP.NET Core Identity
        /// Permet de créer et gérer les rôles (Admin, User, etc.)
        /// </summary>
        private readonly RoleManager<IdentityRole> _roleManager;

        /// <summary>
        /// Configuration de l'application (appsettings.json)
        /// Utilisé pour récupérer les clés JWT, chaînes de connexion, etc.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Contexte de base de données Entity Framework
        /// Permet d'accéder aux tables Users, Roles, UserRoles, etc.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Service d'envoi d'emails pour les notifications par email
        /// Utilisé pour envoyer des emails de bienvenue, notifications, etc.
        /// </summary>
        private readonly EmailService _emailService;
        
        /// <summary>
        /// ✅ SERVICE DE LOGGING SERILOG - LOGS TECHNIQUES SEULEMENT
        /// Utilisé pour :
        /// - Erreurs techniques (exceptions, problèmes système)
        /// - Problèmes de performance 
        /// - Erreurs de configuration
        /// - Debug technique
        /// 
        /// PAS utilisé pour :
        /// - Audit utilisateur (qui se connecte, quand, etc.)
        /// - Traçabilité métier
        /// - Logs de sécurité/conformité
        /// </summary>
        private readonly ILogger<AuthController> _logger;
        
        /// <summary>
        /// ✅ SERVICE D'AUDIT - LOGS MÉTIER ET TRAÇABILITÉ
        /// Utilisé pour :
        /// - Traçabilité des connexions/déconnexions
        /// - Audit des inscriptions d'utilisateurs
        /// - Historique des modifications de profil
        /// - Conformité réglementaire (RGPD, audit de sécurité)
        /// - Analyse des patterns d'utilisation
        /// </summary>
        private readonly AuditLogger _auditLogger;

        // ===== CONSTRUCTEUR AVEC INJECTION DE DÉPENDANCES =====

        /// <summary>
        /// Constructeur du contrôleur avec injection de dépendances
        /// Tous les services nécessaires sont injectés automatiquement par ASP.NET Core
        /// </summary>
        /// <param name="userManager">Gestionnaire des utilisateurs Identity</param>
        /// <param name="signInManager">Gestionnaire de connexion Identity</param>
        /// <param name="roleManager">Gestionnaire des rôles Identity</param>
        /// <param name="configuration">Configuration de l'application</param>
        /// <param name="context">Contexte de base de données</param>
        /// <param name="emailService">Service d'envoi d'emails</param>
        /// <param name="logger">✅ Service de logging pour aspects techniques</param>
        /// <param name="auditLogger">✅ Service d'audit pour traçabilité métier</param>
        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context,
            EmailService emailService,
            ILogger<AuthController> logger,
            AuditLogger auditLogger
            )  
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
            _logger = logger;               // ✅ Service de logging technique
            _auditLogger = auditLogger;     // ✅ Service d'audit métier
        }

        // ===== MÉTHODES D'AUTHENTIFICATION =====

        /// <summary>
        /// INSCRIPTION D'UN NOUVEL UTILISATEUR
        /// Logs techniques : erreurs de création, problèmes de rôles
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
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
                    {
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                        // ✅ LOG TECHNIQUE : Création de rôle manquant (problème de configuration)
                        _logger.LogWarning("🔧 Had to create missing 'User' role during registration - check initial setup");
                    }

                    // On assigne le rôle "User" au nouvel utilisateur
                    await _userManager.AddToRoleAsync(user, "User");

                    await _auditLogger.LogAsync(AuditActions.REGISTER,
                            $"Nouvel utilisateur enregistré: {user.Email}");

                    // ✅ NOUVEAU : Envoi de l'email de bienvenue
                    try
                    {
                        var welcomeSubject = "🎉 Bienvenue dans votre Bibliothèque Numérique !";
                        var welcomeContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            margin: 0; 
            padding: 20px; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #333;
        }}
        .email-container {{ 
            max-width: 600px; 
            margin: 0 auto; 
            background-color: white; 
            border-radius: 15px; 
            overflow: hidden;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
        }}
        .header {{ 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white; 
            text-align: center; 
            padding: 40px 20px;
        }}
        .header h1 {{ 
            margin: 0; 
            font-size: 28px; 
            font-weight: 300;
        }}
        .content {{ 
            padding: 40px 30px;
        }}
        .welcome-message {{ 
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
            color: white;
            padding: 25px; 
            border-radius: 10px; 
            margin: 20px 0; 
            text-align: center;
        }}
        .features {{ 
            background-color: #f8f9fa; 
            padding: 25px; 
            border-radius: 10px; 
            margin: 25px 0;
            border-left: 5px solid #667eea;
        }}
        .feature-item {{ 
            margin: 15px 0; 
            display: flex; 
            align-items: center;
        }}
        .feature-icon {{ 
            width: 30px; 
            height: 30px; 
            background: #667eea; 
            border-radius: 50%; 
            display: inline-flex; 
            align-items: center; 
            justify-content: center; 
            margin-right: 15px;
            color: white;
            font-weight: bold;
        }}
        .cta-button {{ 
            display: inline-block; 
            padding: 15px 30px; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white; 
            text-decoration: none; 
            border-radius: 25px; 
            font-weight: bold;
            text-align: center;
            margin: 20px 0;
            transition: transform 0.3s ease;
        }}
        .cta-button:hover {{ 
            transform: translateY(-2px);
        }}
        .footer {{ 
            text-align: center; 
            color: #666; 
            font-size: 14px; 
            padding: 30px;
            background-color: #f8f9fa;
            border-top: 1px solid #eee;
        }}
        .user-info {{ 
            background-color: #e3f2fd; 
            padding: 20px; 
            border-radius: 8px; 
            margin: 20px 0;
            border: 1px solid #bbdefb;
        }}
        .highlight {{ 
            color: #667eea; 
            font-weight: bold; 
        }}
        .stats {{ 
            display: flex; 
            justify-content: space-around; 
            margin: 25px 0;
            text-align: center;
        }}
        .stat-item {{ 
            flex: 1; 
            padding: 15px;
        }}
        .stat-number {{ 
            font-size: 24px; 
            font-weight: bold; 
            color: #667eea;
        }}
        .stat-label {{ 
            font-size: 12px; 
            color: #666; 
            text-transform: uppercase;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1>📚 Bibliothèque Numérique</h1>
            <p style='margin: 10px 0 0 0; opacity: 0.9;'>Votre nouvelle aventure littéraire commence ici !</p>
        </div>
        
        <div class='content'>
            <h2>Bonjour <span class='highlight'>{user.FullName}</span> ! 👋</h2>
            
            <div class='welcome-message'>
                <h3 style='margin: 0 0 10px 0;'>🎉 Félicitations !</h3>
                <p style='margin: 0; font-size: 16px;'>Votre compte a été créé avec succès. Vous faites maintenant partie de notre communauté de lecteurs passionnés !</p>
            </div>

            <div class='user-info'>
                <h4 style='margin-top: 0; color: #1976d2;'>📋 Informations de votre compte :</h4>
                <p><strong>Email :</strong> {user.Email}</p>
                <p><strong>Nom complet :</strong> {user.FullName}</p>
                <p><strong>Date d'inscription :</strong> {DateTime.Now:dd/MM/yyyy à HH:mm}</p>
                {(!string.IsNullOrEmpty(user.Description) ? $"<p><strong>Description :</strong> {user.Description}</p>" : "")}
            </div>

            <div class='features'>
                <h4 style='margin-top: 0; color: #333;'>🚀 Découvrez ce que vous pouvez faire :</h4>
                
                <div class='feature-item'>
                    <div class='feature-icon'>📖</div>
                    <div>
                        <strong>Parcourir la bibliothèque</strong><br>
                        <small>Explorez notre collection de livres et magazines numériques</small>
                    </div>
                </div>
                
                <div class='feature-item'>
                    <div class='feature-icon'>⬇️</div>
                    <div>
                        <strong>Télécharger vos lectures</strong><br>
                        <small>Accédez à vos livres préférés hors ligne</small>
                    </div>
                </div>
                
                <div class='feature-item'>
                    <div class='feature-icon'>❤️</div>
                    <div>
                        <strong>Créer votre liste de favoris</strong><br>
                        <small>Sauvegardez vos lectures coup de cœur</small>
                    </div>
                </div>
                
                <div class='feature-item'>
                    <div class='feature-icon'>⭐</div>
                    <div>
                        <strong>Noter et commenter</strong><br>
                        <small>Partagez vos avis avec la communauté</small>
                    </div>
                </div>
                
                <div class='feature-item'>
                    <div class='feature-icon'>📊</div>
                    <div>
                        <strong>Suivre votre historique</strong><br>
                        <small>Retrouvez facilement vos lectures passées</small>
                    </div>
                </div>
            </div>

            <div class='stats'>
                <div class='stat-item'>
                    <div class='stat-number'>1000+</div>
                    <div class='stat-label'>Livres disponibles</div>
                </div>
                <div class='stat-item'>
                    <div class='stat-number'>500+</div>
                    <div class='stat-label'>Magazines</div>
                </div>
                <div class='stat-item'>
                    <div class='stat-number'>24/7</div>
                    <div class='stat-label'>Accès libre</div>
                </div>
            </div>

            <div style='text-align: center; margin: 30px 0;'>
                <a href='#' class='cta-button'>🚀 Commencer à explorer</a>
            </div>

            <div style='background-color: #fff3e0; padding: 20px; border-radius: 8px; border-left: 4px solid #ff9800; margin: 25px 0;'>
                <h4 style='margin-top: 0; color: #ef6c00;'>💡 Conseil pour bien commencer :</h4>
                <p style='margin-bottom: 0;'>Complétez votre profil et ajoutez une photo pour personnaliser votre expérience. Vous pouvez également parcourir nos catégories populaires pour découvrir de nouveaux genres !</p>
            </div>

            <div style='background-color: #f3e5f5; padding: 20px; border-radius: 8px; border-left: 4px solid #9c27b0; margin: 25px 0;'>
                <h4 style='margin-top: 0; color: #7b1fa2;'>🔐 Sécurité de votre compte :</h4>
                <p style='margin-bottom: 0;'>Gardez vos identifiants en sécurité et n'hésitez pas à nous contacter si vous remarquez une activité suspecte sur votre compte.</p>
            </div>
        </div>
        
        <div class='footer'>
            <p><strong>Merci de nous avoir rejoints ! 🙏</strong></p>
            <p>L'équipe de la Bibliothèque Numérique</p>
            <hr style='margin: 20px 0; border: none; border-top: 1px solid #eee;'>
            <p style='font-size: 12px; color: #999;'>
                📧 Ceci est un email automatique de bienvenue<br>
                📅 Envoyé le {DateTime.Now:dd/MM/yyyy à HH:mm:ss}<br>
                🌐 LibraryAPI - Votre bibliothèque numérique personnelle
            </p>
        </div>
    </div>
</body>
</html>";

                        // Envoi de l'email de bienvenue
                        await _emailService.SendEmailAsync(user.Email, welcomeSubject, welcomeContent);

                        // ✅ LOG TECHNIQUE : Succès d'envoi de l'email de bienvenue
                        _logger.LogInformation("✅ Welcome email sent successfully to new user {UserEmail} ({UserId})",
                                              user.Email, user.Id);
                    }
                    catch (Exception emailEx)
                    {
                        // ✅ LOG TECHNIQUE : Erreur d'envoi d'email de bienvenue (non bloquante)
                        _logger.LogWarning(emailEx, "⚠️ Failed to send welcome email to new user {UserEmail} ({UserId})",
                                          user.Email, user.Id);
                        // L'inscription continue même si l'email échoue
                    }

                    return Ok(new
                    {
                        Message = "User registered successfully!",
                        UserId = user.Id,
                        Email = user.Email,
                        FullName = user.FullName
                    });
                }

                // ✅ LOG TECHNIQUE : Erreurs de validation Identity (problème technique)
                _logger.LogWarning("⚠️ User registration failed due to Identity validation errors: {Errors}",
                                  string.Join(", ", result.Errors.Select(e => e.Description)));

                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Exception non gérée (problème système)
                _logger.LogError(ex, "❌ Technical error during user registration");
                return StatusCode(500, "An internal error occurred during registration");
            }
        }

        /// <summary>
        /// CONNEXION D'UN UTILISATEUR
        /// Logs techniques : erreurs de génération JWT, problèmes de configuration
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                // Tentative de connexion avec le mot de passe fourni
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

                if (result.Succeeded)
                {   
                    // Si la connexion réussit, récupérer l'utilisateur et ses rôles
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    // Vérifier si l'utilisateur est nul
                    if (user == null)
                    {
                        // ✅ LOG TECHNIQUE : État incohérent (problème système grave)
                        _logger.LogError("🚨 Critical system inconsistency: PasswordSignIn succeeded but FindByEmail failed for {Email}", 
                                        model.Email);
                        return Unauthorized();
                    }

                    var roles = await _userManager.GetRolesAsync(user); 

                    // Générer un token JWT pour l'utilisateur
                    var token = GenerateJwtToken(user, roles);

                    await _auditLogger.LogAsync(AuditActions.LOGIN_SUCCESS,
                            $"Connexion réussie pour l'utilisateur: {user.Email}");

                    return Ok(new { Token = token });
                }

                // Pas de log technique pour échec de connexion normale (c'est métier, pas technique)
                await _auditLogger.LogAsync(AuditActions.LOGIN_FAILED,
                    $"Tentative de connexion échouée pour: {model.Email}");

                return Unauthorized();
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Exception non gérée (problème système)
                _logger.LogError(ex, "❌ Technical error during login process");
                return StatusCode(500, "An internal error occurred during login");
            }
        }

        /// <summary>
        /// DÉCONNEXION D'UN UTILISATEUR
        /// Logs techniques : erreurs de SignOut
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Déconnexion de l'utilisateur
                await _signInManager.SignOutAsync();

                await _auditLogger.LogAsync(AuditActions.LOGOUT,
                        $"Déconnexion de l'utilisateur");

                return Ok(new { Message = "Logged out successfully!" });
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur technique lors de la déconnexion
                _logger.LogError(ex, "❌ Technical error during logout process");
                return StatusCode(500, "An internal error occurred during logout");
            }
        }

        // ===== GESTION DU PROFIL UTILISATEUR =====
        
        /// <summary>
        /// MISE À JOUR DU PROFIL UTILISATEUR
        /// Logs techniques : erreurs d'upload, problèmes filesystem, erreurs BDD
        /// </summary>
        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) 
                {
                    // ✅ LOG TECHNIQUE : Token valide mais utilisateur introuvable (problème système)
                    _logger.LogError("🚨 Valid JWT token but user not found in database - potential data inconsistency");
                    return NotFound();
                }

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
                    try
                    {
                        // Création du dossier de destination s'il n'existe pas
                        var uploadsFolder = Path.Combine("wwwroot", "images", "profiles");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                            // ✅ LOG TECHNIQUE : Création de dossier manquant (problème de setup)
                            _logger.LogWarning("📁 Had to create missing uploads directory: {Path} - check deployment setup", 
                                              uploadsFolder);
                        }

                        // Génération d'un nom de fichier unique avec l'ID utilisateur
                        var fileName = $"{user.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{model.ProfilePicture.FileName}";
                        var imagePath = Path.Combine(uploadsFolder, fileName);
                        
                        // Sauvegarde du fichier sur le serveur
                        using (var stream = new FileStream(imagePath, FileMode.Create))
                        {
                            await model.ProfilePicture.CopyToAsync(stream);
                        }

                        // Suppression de l'ancienne image (si elle existe)
                        // if (!string.IsNullOrEmpty(user.ProfilePicture))
                        // {
                        //     var oldImagePath = Path.Combine("wwwroot", user.ProfilePicture.TrimStart('/'));
                        //     if (File.Exists(oldImagePath))
                        //     {
                        //         File.Delete(oldImagePath);
                        //     }
                        // }

                        // Stockage du chemin relatif dans la base de données
                        user.ProfilePicture = $"/images/profiles/{fileName}";
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème de permissions filesystem
                        _logger.LogError(ex, "❌ File system permission error during profile picture upload");
                        return BadRequest(new { Message = "Server configuration error - cannot save files" });
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème de structure de dossiers
                        _logger.LogError(ex, "❌ Directory structure error during profile picture upload");
                        return BadRequest(new { Message = "Server configuration error - missing directories" });
                    }
                    catch (IOException ex)
                    {
                        // ✅ LOG TECHNIQUE : Problème I/O (disque plein, etc.)
                        _logger.LogError(ex, "❌ I/O error during profile picture upload");
                        return BadRequest(new { Message = "Server error - cannot save file" });
                    }
                }

                // Sauvegarde des modifications dans la base de données
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _auditLogger.LogAsync(AuditActions.PROFILE_UPDATED,
                            $"Profil mis à jour pour l'utilisateur: {user.Email}");

                    return Ok(new
                    {
                        Message = "Profile updated successfully!",
                        ProfilePictureUrl = user.ProfilePicture
                    });
                }

                // ✅ LOG TECHNIQUE : Erreurs de validation Identity
                _logger.LogWarning("⚠️ User profile update failed due to Identity validation errors: {Errors}",
                                  string.Join(", ", result.Errors.Select(e => e.Description)));

                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Exception non gérée
                _logger.LogError(ex, "❌ Technical error during profile update");
                return StatusCode(500, "An internal error occurred during profile update");
            }
        }

        // ===== MÉTHODES ADMINISTRATIVES =====
        
        /// <summary>
        /// RÉCUPÉRATION DE TOUS LES UTILISATEURS
        /// Logs techniques : erreurs de base de données, problèmes de performance
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUsers()
        {
            try
            {
                // Vérification que les tables de rôles sont correctement configurées
                if (_context.UserRoles == null || _context.Roles == null)
                {
                    // ✅ LOG TECHNIQUE : Problème de configuration de base de données
                    _logger.LogError("🚨 Database context configuration error: UserRoles or Roles is null");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        "Database configuration error");
                }

                // Requête pour récupérer tous les utilisateurs avec leurs rôles
                var users = _context.Users
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Email = u.Email!,
                        Role = _context.UserRoles!
                                .Where(ur => ur.UserId == u.Id)
                                .Join(_context.Roles,
                                    ur => ur.RoleId,
                                    role => role.Id,
                                    (ur, role) => role.Name)
                                .FirstOrDefault()
                    })
                    .ToList();

                return Ok(users);
            }
            catch (InvalidOperationException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de requête LINQ/EF
                _logger.LogError(ex, "❌ Database query error while retrieving users list");
                return StatusCode(500, "Database query error");
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générique
                _logger.LogError(ex, "❌ Technical error while retrieving users list");
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("users/{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUserById(string id)
        {
            try
            {
                // Recherche de l'utilisateur par ID avec son rôle
                var user = _context.Users
                    .Where(u => u.Id == id)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Email = u.Email!,
                        Role = _context.UserRoles
                                .Where(ur => ur.UserId == u.Id)
                                .Join(_context.Roles,
                                    ur => ur.RoleId,
                                    role => role.Id,
                                    (ur, role) => role.Name)
                                .FirstOrDefault()
                    })
                    .FirstOrDefault();

                if (user == null)
                {
                    return NotFound($"User with id {id} not found.");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur lors de la récupération
                _logger.LogError(ex, "❌ Technical error while retrieving user {UserId}", id);
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("users/role/{roleName}")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUsersByRole(string roleName)
        {
            try
            {
                // Récupérer le rôle correspondant
                var role = _context.Roles.FirstOrDefault(r => r.Name == roleName);

                // Vérifier si le rôle existe
                if (role == null)
                {
                    return NotFound($"Role '{roleName}' not found.");
                }

                var users = _context.Users
                    .Where(u => _context.UserRoles
                        .Any(ur => ur.UserId == u.Id && ur.RoleId == role.Id))
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Email = u.Email!,
                        Role = roleName
                    })
                    .ToList();

                return Ok(users);
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur lors de la recherche par rôle
                _logger.LogError(ex, "❌ Technical error while retrieving users by role {RoleName}", roleName);
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("users/search")]
        [Authorize(Roles = "Admin")]
        public IActionResult SearchUsers([FromQuery] string query)
        {
            // Validation du terme de recherche
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty");
            }

            try
            {
                // Recherche dans les noms d'utilisateur et emails
                var users = _context.Users
                    .Where(u => u.UserName!.Contains(query) || u.Email!.Contains(query))
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Email = u.Email!,
                        Role = _context.UserRoles
                                .Where(ur => ur.UserId == u.Id)
                                .Join(_context.Roles,
                                    ur => ur.RoleId,
                                    role => role.Id,
                                    (ur, role) => role.Name)
                                .FirstOrDefault()
                    })
                    .ToList();

                return Ok(users);
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur lors de la recherche
                _logger.LogError(ex, "❌ Technical error during user search with query '{Query}'", query);
                return StatusCode(500, "An internal error occurred during search");
            }
        }

        [HttpGet("users/{id}/notifications")]
        [Authorize]
        public IActionResult GetUserNotifications(string id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            // Vérification des droits d'accès
            if (currentUserId != id && !isAdmin)
            {
                return Forbid("You can only access your own notifications");
            }

            try
            {
                // Récupération des notifications de l'utilisateur
                var notifications = _context.UserNotifications
                    .Where(un => un.UserId == id)
                    .Include(un => un.Notification)  // Chargement des détails de notification
                    .Select(un => new 
                    {
                        NotificationId = un.NotificationId,
                        Content = un.Notification != null ? un.Notification.Content : "No content available",
                        IsRead = un.Notification != null && un.Notification.IsRead,
                        CreatedAt = un.Notification != null ? un.Notification.CreatedAt : DateTime.MinValue
                    })
                    .OrderByDescending(n => n.CreatedAt)  // Plus récentes en premier
                    .ToList();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur lors de la récupération des notifications
                _logger.LogError(ex, "❌ Technical error while retrieving notifications for user {UserId}", id);
                return StatusCode(500, "An internal error occurred while retrieving notifications");
            }
        }

        // ===== MÉTHODE PRIVÉE DE GÉNÉRATION JWT =====
        
        /// <summary>
        /// GÉNÉRATION D'UN TOKEN JWT
        /// Logs techniques : erreurs de configuration, problèmes de signature
        /// </summary>
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            try
            {
                // Création des claims (informations incluses dans le token)
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Ajout des rôles en tant que claims
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

                // Récupération de la clé secrète depuis la configuration
                var jwtKey = _configuration["Jwt:Key"];
                if (string.IsNullOrEmpty(jwtKey))
                {
                    // ✅ LOG TECHNIQUE : Problème de configuration critique
                    _logger.LogError("🚨 JWT Key is not configured in appsettings - authentication will fail");
                    throw new InvalidOperationException("JWT Key is not configured.");
                }

                // Vérification des autres paramètres JWT
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];
                
                if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                {
                    // ✅ LOG TECHNIQUE : Configuration JWT incomplète
                    _logger.LogError("🚨 JWT Issuer or Audience not configured - tokens may be invalid");
                }

                // Création de la clé de signature symétrique
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryHours", 3); // 3h par défaut



                // Création du token JWT
                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    //expires: DateTime.UtcNow.AddHours(3),
                    expires: DateTime.UtcNow.AddHours(expiryHours),
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (ArgumentException ex)
            {
                // ✅ LOG TECHNIQUE : Problème avec les paramètres du token
                _logger.LogError(ex, "❌ Invalid arguments during JWT token generation");
                throw;
            }
            catch (SecurityTokenException ex)
            {
                // ✅ LOG TECHNIQUE : Erreur de sécurité lors de la création du token
                _logger.LogError(ex, "❌ Security error during JWT token generation");
                throw;
            }
            catch (Exception ex)
            {
                // ✅ LOG TECHNIQUE : Erreur générale
                _logger.LogError(ex, "❌ Unexpected error during JWT token generation");
                throw;
            }
        }
    }
}

/* MÉTHODES À ENVISAGER POUR COMPLÉTER LE CONTRÔLEUR :
        
        Récupérer la liste des utilisateurs :
        Dans une méthode pour récupérer tous les utilisateurs (par exemple, GetAllUsers() ou GetUsers()), 
        UserDto est pratique pour filtrer et structurer les informations utilisateur avant de les renvoyer au client.

        Récupérer les informations d'un utilisateur spécifique :
        Une méthode comme GetUserById() ou GetUserProfile() pourrait utiliser UserDto pour fournir des 
        informations détaillées sur un utilisateur spécifique sans exposer d'informations sensibles.

        Filtrer les utilisateurs par rôles :
        Si vous avez une méthode comme GetUsersByRole(string roleName) pour récupérer uniquement les 
        utilisateurs ayant un rôle spécifique, UserDto serait idéal pour structurer la réponse sans 
        exposer l'intégralité des entités ApplicationUser.

        Rechercher des utilisateurs par critères :
        Une méthode SearchUsers(string query) pourrait utiliser UserDto pour renvoyer des informations 
        utilisateur en réponse à des critères de recherche, limitant les données renvoyées au strict nécessaire.

        Afficher l'activité d'un utilisateur :
        Dans des méthodes pour afficher les activités des utilisateurs, comme l'historique des favoris 
        ou les statistiques d'utilisation, UserDto permet d'inclure seulement les informations essentielles 
        d'un utilisateur.

        Notifications ou activité récente :
        Dans des méthodes pour afficher les notifications d'un utilisateur ou l'activité récente, UserDto 
        est utile pour structurer les informations utilisateur dans la réponse de manière sécurisée.
        */


        /*
        ===== FONCTIONNALITÉS SUPPLÉMENTAIRES À DÉVELOPPER =====
        
        Les commentaires ci-dessous décrivent des fonctionnalités additionnelles
        qui pourraient être implémentées dans ce contrôleur d'authentification :

        📋 GESTION AVANCÉE DES COMPTES UTILISATEUR :
        - Désactivation/réactivation temporaire de comptes utilisateur
        - Système de réinitialisation de mot de passe par email sécurisé
        - Changement de rôle d'un utilisateur (promotion/rétrogradation)
        - Suppression définitive d'un compte utilisateur avec confirmation
        - Statistiques détaillées d'utilisation par utilisateur

        🔐 RENFORCEMENT DE LA SÉCURITÉ :
        - Authentification à deux facteurs (2FA) avec QR codes
        - Historique détaillé des connexions et géolocalisation
        - Verrouillage automatique de compte après X tentatives échouées
        - Système de détection d'activité suspecte et alertes
        - Liste noire de tokens JWT pour invalidation forcée

        📊 ANALYTICS ET SURVEILLANCE :
        - Tableau de bord des statistiques de connexion
        - Identification des utilisateurs les plus actifs
        - Analyse des tendances d'inscription et saisonnalité
        - Surveillance en temps réel des erreurs d'authentification
        - Génération de rapports d'activité administrative

        📬 SYSTÈME DE NOTIFICATIONS AVANCÉ :
        - Emails de bienvenue personnalisés avec templates
        - Notifications de sécurité (nouvelle connexion, changement de mot de passe)
        - Alertes automatiques d'activité suspecte
        - Système de newsletters et communications ciblées

        🎨 PERSONNALISATION DE L'EXPÉRIENCE :
        - Thèmes personnalisés par utilisateur (clair/sombre)
        - Gestion des préférences de langue multilingue
        - Configuration d'affichage et mise en page
        - Paramètres de confidentialité granulaires

        EXEMPLE D'IMPLÉMENTATION - Réinitialisation de mot de passe sécurisée :
        
        [HttpPost("reset-password-request")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] ResetPasswordRequestModel model)
        {
            _logger.LogInformation("🔄 Demande de réinitialisation de mot de passe pour: {Email}", model.Email);
            
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("⚠️ Tentative de réinitialisation pour email inexistant: {Email}", model.Email);
                // Ne pas révéler si l'email existe ou non (principe de sécurité)
                return Ok(new { Message = "Si l'email existe, un lien de réinitialisation a été envoyé." });
            }

            // Génération d'un token de réinitialisation sécurisé
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Création du lien de réinitialisation avec expiration
            var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?token={resetToken}&email={user.Email}";
            
            // Envoi d'email sécurisé avec template HTML
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink);
            
            _logger.LogInformation("📧 Email de réinitialisation envoyé à l'utilisateur {UserId}", user.Id);
            await _auditLogger.LogAsync(AuditActions.PASSWORD_RESET_REQUESTED, 
                $"Demande de réinitialisation de mot de passe pour: {user.Email}");
                
            return Ok(new { Message = "Email de réinitialisation envoyé avec succès." });
        }

        [HttpPost("reset-password-confirm")]
        public async Task<IActionResult> ConfirmPasswordReset([FromBody] ResetPasswordConfirmModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return BadRequest("Utilisateur introuvable.");
                }

                // Réinitialisation du mot de passe avec le token
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                
                if (result.Succeeded)
                {
                    await _auditLogger.LogAsync(AuditActions.PASSWORD_CHANGED,
                        $"Mot de passe réinitialisé avec succès pour: {user.Email}");
                    
                    // Envoi d'email de confirmation
                    await _emailService.SendPasswordChangedConfirmationAsync(user.Email, user.FullName);
                    
                    return Ok(new { Message = "Mot de passe réinitialisé avec succès." });
                }

                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur technique lors de la confirmation de réinitialisation");
                return StatusCode(500, "Erreur interne lors de la réinitialisation du mot de passe");
            }
        }
        */

/*
===== SYSTÈME DE LOGGING DUAL IMPLÉMENTÉ =====

✅ LOGS TECHNIQUES (Serilog) - Diagnostic et maintenance :
- Erreurs d'exception non gérées et stack traces
- Problèmes de configuration (clés JWT manquantes, rôles non créés)
- Erreurs de base de données et problèmes de connexion
- Problèmes filesystem (permissions, dossiers manquants, uploads)
- Erreurs de permissions et d'accès aux ressources
- Incohérences système critiques et états invalides
- Problèmes de performance et timeouts

✅ LOGS D'AUDIT (Base de données) - Traçabilité et conformité :
- Connexions et déconnexions d'utilisateurs
- Inscriptions de nouveaux comptes
- Modifications de profils utilisateur
- Actions administratives (changements de rôles, etc.)
- Conformité réglementaire (RGPD, audit de sécurité)

===== EXEMPLES DE LOGS TECHNIQUES GÉNÉRÉS =====

[15:30:16 WRN] 🔧 Rôle 'User' manquant créé lors de l'inscription - vérifier la configuration initiale
[15:32:46 ERR] 🚨 Incohérence système critique: PasswordSignIn réussi mais FindByEmail échoué pour user@exemple.com
[15:35:20 WRN] 📁 Création du dossier d'upload manquant: wwwroot/images/profiles - vérifier le déploiement
[15:36:12 ERR] ❌ Erreur de permissions système lors de l'upload d'image de profil
[15:40:15 ERR] 🚨 Erreur de configuration du contexte BDD: UserRoles ou Roles est null
[15:45:30 ERR] 🚨 Clé JWT non configurée dans appsettings - l'authentification échouera
[15:50:22 INF] ✅ Email de bienvenue envoyé avec succès au nouvel utilisateur john@exemple.com (ID: abc123)
[15:55:10 WRN] ⚠️ Échec d'envoi de l'email de bienvenue au nouvel utilisateur marie@exemple.com (ID: def456)

CES LOGS PERMETTENT DE :
✅ Détecter rapidement les problèmes de configuration
✅ Identifier les erreurs système avant qu'elles n'affectent les utilisateurs
✅ Surveiller les performances et la disponibilité
✅ Diagnostiquer les pannes et résoudre les incidents
✅ Assurer une maintenance technique proactive
✅ Garantir la qualité de service

===== BÉNÉFICES DE CETTE ARCHITECTURE =====

🔧 MAINTENANCE TECHNIQUE :
- Détection précoce des problèmes système
- Diagnostic rapide des pannes et erreurs
- Surveillance de la santé de l'application
- Optimisation continue des performances

📊 CONFORMITÉ ET AUDIT :
- Traçabilité complète des actions utilisateur
- Respect des exigences réglementaires (RGPD)
- Audit de sécurité et investigations
- Analyse des patterns d'utilisation

💡 AMÉLIORATION CONTINUE :
- Identification des points de friction utilisateur
- Optimisation de l'expérience d'inscription
- Amélioration de la sécurité basée sur les données
- Évolution guidée par les métriques d'usage

*/