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

    // [HttpPut("update-profile")]
    // [Authorize]
    // public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
    // { 
    //     // Récupération de l'utilisateur à partir du contexte
    //     var user = await _userManager.GetUserAsync(User);
    //     if (user == null) return NotFound();

    //     // Mise à jour des champs du profil
    //     user.FullName = model.FullName;
    //     user.Description = model.Description;
    //     user.ProfilePicture = model.ProfilePicture;

    //     // Sauvegarde des modifications dans la base de données
    //     var result = await _userManager.UpdateAsync(user);
    //     if (result.Succeeded) return Ok(new { Message = "Profile updated successfully!" });

    //     // En cas d'échec de mise à jour, retourner les erreurs
    //     return BadRequest(result.Errors);
    // }
    
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
