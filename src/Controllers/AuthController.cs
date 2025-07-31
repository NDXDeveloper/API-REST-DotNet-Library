using Microsoft.AspNetCore.Mvc;  // Pour gérer les contrôleurs et les actions d'API
using Microsoft.AspNetCore.Identity;  // Pour utiliser Identity (gestion des utilisateurs, rôles, etc.)
using Microsoft.AspNetCore.Authorization;  // Pour gérer les attributs d'autorisation
using System.IdentityModel.Tokens.Jwt;  // Pour manipuler les tokens JWT
using System.Security.Claims;  // Pour créer et gérer les claims dans les tokens JWT
using Microsoft.IdentityModel.Tokens;  // Pour gérer la validation et la signature des tokens JWT
using System.Text;  // Pour encoder les clés de sécurité
using Microsoft.AspNetCore.Http;  // Pour utiliser IFormFile
using System.IO;  // Pour utiliser Path et FileStream
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data; // Pour utiliser la méthode Include
using LibraryAPI.Models;
using Microsoft.AspNetCore.RateLimiting;

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
        
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Service d'envoi d'emails pour les notifications par email
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

        // ===== CONSTRUCTEUR AVEC INJECTION DE DÉPENDANCES =====
        
        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context,
            EmailService emailService, 
            ILogger<AuthController> logger)  // ✅ Logger pour aspects techniques seulement
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
            _logger = logger;
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

            return Ok(new { 
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
                    return Ok(new { Token = token });
                }

                // Pas de log technique pour échec de connexion normale (c'est métier, pas technique)
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
                    return Ok(new { 
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

                // Création du token JWT
                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(3),
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


 /* à envisager :
        Récupérer la liste des utilisateurs :
        Dans une méthode pour récupérer tous les utilisateurs (par exemple, GetAllUsers() ou GetUsers()), UserDto est pratique pour filtrer et structurer les informations utilisateur avant de les renvoyer au client.

        Récupérer les informations d’un utilisateur spécifique :
        Une méthode comme GetUserById() ou GetUserProfile() pourrait utiliser UserDto pour fournir des informations détaillées sur un utilisateur spécifique sans exposer d’informations sensibles.

        Filtrer les utilisateurs par rôles :
        Si vous avez une méthode comme GetUsersByRole(string roleName) pour récupérer uniquement les utilisateurs ayant un rôle spécifique, UserDto serait idéal pour structurer la réponse sans exposer l’intégralité des entités ApplicationUser.

        Rechercher des utilisateurs par critères :
        Une méthode SearchUsers(string query) pourrait utiliser UserDto pour renvoyer des informations utilisateur en réponse à des critères de recherche, limitant les données renvoyées au strict nécessaire.

        Afficher l'activité d'un utilisateur :
        Dans des méthodes pour afficher les activités des utilisateurs, comme l’historique des favoris ou les statistiques d’utilisation, UserDto permet d’inclure seulement les informations essentielles d'un utilisateur.

        Notifications ou activité récente :
        Dans des méthodes pour afficher les notifications d’un utilisateur ou l’activité récente, UserDto est utile pour structurer les informations utilisateur dans la réponse de manière sécurisée.
        */


        /*
        ===== MÉTHODES SUPPLÉMENTAIRES À ENVISAGER =====
        
        Les commentaires ci-dessous décrivent des fonctionnalités additionnelles
        qui pourraient être implémentées dans ce contrôleur :

        📋 GESTION AVANCÉE DES UTILISATEURS :
        - Désactivation/réactivation de comptes utilisateur
        - Réinitialisation de mot de passe par email
        - Changement de rôle d'un utilisateur (promotion/rétrogradation)
        - Suppression définitive d'un compte utilisateur
        - Statistiques d'utilisation par utilisateur

        🔐 SÉCURITÉ RENFORCÉE :
        - Authentification à deux facteurs (2FA)
        - Historique des connexions
        - Verrouillage de compte après X tentatives échouées
        - Détection d'activité suspecte
        - Invalidation de tokens JWT (blacklist)

        📊 ANALYTICS ET MONITORING :
        - Statistiques de connexion
        - Utilisateurs les plus actifs
        - Analyse des tendances d'inscription
        - Monitoring des erreurs d'authentification
        - Rapport d'activité administrative

        📬 NOTIFICATIONS AVANCÉES :
        - Envoi d'emails de bienvenue
        - Notifications de sécurité (nouvelle connexion)
        - Alertes d'activité suspecte
        - Newsletters et communications

        🎨 PERSONNALISATION :
        - Thèmes personnalisés par utilisateur
        - Préférences de langue
        - Configuration d'affichage
        - Paramètres de confidentialité

        EXEMPLE D'IMPLÉMENTATION - Méthode de réinitialisation de mot de passe :
        
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            _logger.LogInformation("🔄 Password reset attempt for email: {Email}", model.Email);
            
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("⚠️ Password reset attempted for non-existent email: {Email}", model.Email);
                // Ne pas révéler si l'email existe ou non (sécurité)
                return Ok(new { Message = "If the email exists, a reset link has been sent." });
            }

            // Génération d'un token de réinitialisation
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Envoi d'email avec le lien de réinitialisation
            // ... code d'envoi d'email ...
            
            _logger.LogInformation("📧 Password reset email sent for user {UserId}", user.Id);
            return Ok(new { Message = "Password reset email sent successfully." });
        }
        */

/*

✅ LOGS : Logs techniques uniquement :
- Erreurs d'exception non gérées
- Problèmes de configuration (JWT, rôles manquants)
- Erreurs de base de données et requêtes
- Problèmes filesystem (upload d'images)
- Erreurs de permissions et I/O
- Incohérences système critiques
- Problèmes de performance

===== EXEMPLES DE LOGS GÉNÉRÉS (TECHNIQUES SEULEMENT) =====

[15:30:16 WRN] 🔧 Had to create missing 'User' role during registration - check initial setup
[15:32:46 ERR] 🚨 Critical system inconsistency: PasswordSignIn succeeded but FindByEmail failed for user@example.com
[15:35:20 WRN] 📁 Had to create missing uploads directory: wwwroot/images/profiles - check deployment setup  
[15:36:12 ERR] ❌ File system permission error during profile picture upload
[15:40:15 ERR] 🚨 Database context configuration error: UserRoles or Roles is null
[15:45:30 ERR] 🚨 JWT Key is not configured in appsettings - authentication will fail

CES LOGS AIDENT À :
✅ Détecter les problèmes de configuration
✅ Identifier les erreurs système
✅ Monitorer les performances
✅ Diagnostiquer les pannes
✅ Assurer la maintenance technique


*/