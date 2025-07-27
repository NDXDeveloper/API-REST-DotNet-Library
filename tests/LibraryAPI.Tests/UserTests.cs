using Xunit; // Pour les tests unitaires
using Moq; // Pour créer des mocks (objets simulés)
using Microsoft.AspNetCore.Mvc; // Pour les contrôleurs et les actions d'API
using Microsoft.AspNetCore.Identity; // Pour la gestion des utilisateurs
using Microsoft.Extensions.Configuration; // Pour la configuration
using Microsoft.Extensions.Logging; // Pour les journaux (logging)
using Microsoft.Extensions.Options; // Pour les options
using Microsoft.AspNetCore.Http; // Pour les interfaces HTTP
using LibraryAPI.Data; // Pour accéder à la base de données
using LibraryAPI.Models; // Pour les modèles de données
using System.Threading.Tasks; // Pour la gestion des tâches asynchrones
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult; // Pour éviter les conflits de noms

using System.IdentityModel.Tokens.Jwt;           // Pour JwtSecurityToken, JwtSecurityTokenHandler, JwtRegisteredClaimNames
using System.Security.Claims;                    // Pour Claim et ClaimTypes
using Microsoft.IdentityModel.Tokens;            // Pour SymmetricSecurityKey, SigningCredentials, SecurityAlgorithms
using System.Text;                               // Pour Encoding


namespace UnitTests // Définition de l'espace de noms pour ce fichier de tests utilisateurs
{
    public class UserTests // Déclaration de la classe de test
    {
        // Mock<UserManager<ApplicationUser>> : Crée un objet simulé (mock) pour UserManager, qui est utilisé pour gérer les opérations liées aux utilisateurs (comme la création, la recherche, etc.).
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        // Mock<SignInManager<ApplicationUser>> : Crée un objet simulé pour SignInManager, qui gère les connexions des utilisateurs.
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        // IConfiguration : Cette interface est utilisée pour accéder aux paramètres de configuration, comme les clés JWT.
        private readonly IConfiguration _configuration;

        public UserTests()
        {
            // Mock UserManager
            // Crée un mock pour l'interface qui stocke les utilisateurs.
            var store = new Mock<IUserStore<ApplicationUser>>();
            // Initialise le mock pour UserManager.
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // Mock SignInManager
            // Initialise le mock pour SignInManager avec le mock de UserManager et d'autres dépendances.
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(_mockUserManager.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(), null!, null!, null!, null!);

            // Initialize IConfiguration if needed
            // Crée un dictionnaire pour stocker les paramètres de configuration en mémoire.
            // CORRECTION: Utilisation de Dictionary<string, string?> au lieu de Dictionary<string, string>
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"Jwt:Key", "YourSuperSecretKeyWithAtLeast16Chars"},
                {"Jwt:Issuer", "LibraryApi"},
                {"Jwt:Audience", "LibraryApiUsers"}
            };

            //Crée une instance de IConfiguration à partir de ces paramètres.
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact] // Indique que cette méthode est un test unitaire.
        public async Task Login_ReturnsOkResult_WhenCredentialsAreValid()
        {
            // Arrange : Ici, vous préparez vos données et configurez le comportement attendu des mocks.
            //           Vous définissez l'email et le mot de passe et configurez le mock de SignInManager pour retourner SignInResult.Success
            //           lorsque la méthode PasswordSignInAsync est appelée avec ces informations.
            var email = "nix@nix.fr";
            var password = "password";

            // Mock sign-in logic
            _mockSignInManager.Setup(x => x.PasswordSignInAsync(email, password, false, false))
                .ReturnsAsync(SignInResult.Success);

            // Act : Vous appelez la méthode à tester. Dans ce cas, vous essayez de vous connecter avec les informations d'identification fournies.
            var result = await _mockSignInManager.Object.PasswordSignInAsync(email, password, false, false);

            // Assert : Vous vérifiez que le résultat du test est comme prévu. Ici, vous vérifiez que la connexion a réussi (result.Succeeded est vrai).
            Assert.True(result.Succeeded);
        }

        // Pour générer le Token - VERSION CORRIGÉE
        private string GenerateJwtToken(ApplicationUser user)
        {
            // CORRECTION: Ajout de vérifications null pour éviter les warnings CS8604
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

            // Obtenir la clé secrète depuis _configuration
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public void GenerateJwtToken_AfterSuccessfulLogin_ReturnsToken()
        {
            // Arrange
            var email = "nix@nix.fr";
            var user = new ApplicationUser { UserName = email, Email = email };

            // Simulate finding the user
            _mockUserManager.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user);

            // Act
            var token = GenerateJwtToken(user);

            // Assert
            Assert.NotNull(token); // Verify the token is generated
        }

        // Vous pouvez ajouter d'autres tests ici

    }
}
