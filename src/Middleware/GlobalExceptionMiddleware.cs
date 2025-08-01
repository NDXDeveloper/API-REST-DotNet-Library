using System.Security.Claims;   // Pour récupérer les informations de l'utilisateur connecté (ID, nom, etc.)
using Serilog;                  // Système de logging avancé pour enregistrer les erreurs et événements

/// <summary>
/// MIDDLEWARE DE GESTION GLOBALE DES EXCEPTIONS
/// 
/// Ce middleware intercepte TOUTES les exceptions non gérées dans l'application
/// et les traite de manière uniforme au lieu de laisser l'application planter.
/// 
/// RÔLE :
/// - Capture automatiquement toutes les erreurs
/// - Log les erreurs avec des détails contextuels (qui, quoi, où, quand)
/// - Retourne une réponse JSON standardisée au client
/// - Évite d'exposer les détails techniques sensibles aux utilisateurs
/// </summary>
public class GlobalExceptionMiddleware
{
    // ===== PROPRIÉTÉS PRIVÉES =====

    // Délégué qui représente le prochain middleware dans la chaîne de traitement
    // Chaque requête HTTP passe par une série de middlewares (pipeline)
    private readonly RequestDelegate _next;

    // Service de logging pour enregistrer les erreurs dans les fichiers de logs
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    //private readonly AuditLogger _auditLogger;

    // ===== CONSTRUCTEUR =====

    /// <summary>
    /// Constructeur du middleware - appelé automatiquement par ASP.NET Core
    /// lors de l'injection de dépendances
    /// </summary>
    /// <param name="next">Le prochain middleware dans le pipeline</param>
    /// <param name="logger">Service de logging injecté automatiquement</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;   // Stocke la référence vers le middleware suivant
        _logger = logger;   // Stocke le service de logging
        
    }

    // ===== MÉTHODE PRINCIPALE D'EXÉCUTION =====

    /// <summary>
    /// Méthode appelée automatiquement pour chaque requête HTTP
    /// C'est le point d'entrée principal du middleware
    /// </summary>
    /// <param name="context">Contexte HTTP contenant toutes les infos de la requête</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Essaie d'exécuter le middleware suivant dans la chaîne
            // Si tout se passe bien, la requête continue normalement
            await _next(context);
        }
        catch (Exception ex)
        {
            // Si une exception se produit ANYWHERE dans l'application,
            // on la capture ici et on la traite proprement
            await HandleExceptionAsync(context, ex);
        }
    }

    // ===== GESTIONNAIRE D'EXCEPTIONS =====

    /// <summary>
    /// Traite une exception capturée et génère une réponse appropriée
    /// </summary>
    /// <param name="context">Contexte HTTP de la requête qui a causé l'erreur</param>
    /// <param name="exception">L'exception qui s'est produite</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // ===== RÉCUPÉRATION DES INFORMATIONS CONTEXTUELLES =====

        // Récupère l'ID de l'utilisateur connecté (s'il y en a un)
        // ClaimTypes.NameIdentifier = identifiant unique de l'utilisateur dans le système
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        // Récupère le nom d'utilisateur (email ou nom d'affichage)
        var userName = context.User?.Identity?.Name ?? "Anonymous";
        // Récupère le chemin de l'URL demandée (ex: "/api/books/123")
        var path = context.Request.Path.Value ?? "";
        // Récupère la méthode HTTP utilisée (GET, POST, PUT, DELETE, etc.)
        var method = context.Request.Method;
        // Récupère les paramètres de requête (ex: "?page=1&limit=10")
        var queryString = context.Request.QueryString.Value ?? "";
        // Récupère l'User-Agent (navigateur/application qui fait la requête)
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
        // Récupère l'adresse IP du client qui fait la requête
        var clientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // ===== LOGGING PRINCIPAL DE L'ERREUR =====

        // Enregistre l'erreur avec tous les détails contextuels
        // LogError = niveau d'erreur (sera affiché en rouge dans les logs)
        _logger.LogError(exception,
            "🚨 Unhandled exception occurred. " +
            "Path: {Path}, Method: {Method}, Query: {QueryString}, " +
            "UserId: {UserId}, UserName: {UserName}, " +
            "ClientIP: {ClientIP}, UserAgent: {UserAgent}",
            path, method, queryString, userId, userName, clientIP, userAgent);

        // ===== LOGGING SPÉCIALISÉ PAR TYPE D'EXCEPTION =====

        // Traitement différencié selon le type d'erreur pour des logs plus précis
        switch (exception)
        {
            // Erreur d'autorisation (utilisateur pas autorisé à faire cette action)
            case UnauthorizedAccessException:
                _logger.LogWarning("🔒 Unauthorized access attempt on {Path} by {UserId} from {ClientIP}",
                                  path, userId, clientIP);
                break;
            // Erreur d'argument (paramètre invalide passé à une méthode)
            case ArgumentException argEx:
                _logger.LogWarning("📝 Invalid argument provided: {ParameterName} = {Message}",
                                  argEx.ParamName, argEx.Message);
                break;
            // Erreur d'opération invalide (action impossible dans l'état actuel)
            case InvalidOperationException:
                _logger.LogError("⚙️ Invalid operation: {Message} on {Path}", exception.Message, path);
                break;
            // Erreur de timeout (opération trop longue)
            case TimeoutException:
                _logger.LogError("⏱️ Operation timeout on {Path} after {Method} request", path, method);
                break;
        }

        // ===== LOGS D'AUDIT POUR ÉVÉNEMENTS DE SÉCURITÉ ===== 
        var auditLogger = context.RequestServices.GetService<AuditLogger>();
        if (auditLogger != null)
        {
            try
            {
                switch (exception)
                {
                    case UnauthorizedAccessException:
                        await auditLogger.LogAsync(LibraryAPI.Models.AuditActions.UNAUTHORIZED_ACCESS,
                            $"Accès non autorisé tenté sur {path} depuis l'IP {clientIP}");
                        break;
                    case TimeoutException:
                        await auditLogger.LogAsync(LibraryAPI.Models.AuditActions.SYSTEM_ERROR,
                            $"Timeout système sur {path}");
                        break;
                    default:
                        await auditLogger.LogAsync(LibraryAPI.Models.AuditActions.SYSTEM_ERROR,
                            $"Erreur système: {exception.GetType().Name} sur {path}");
                        break;
                }
            }
            catch
            {
                // Éviter les erreurs en cascade lors de l'audit
            }
        }

        // ===== DÉTERMINATION DU CODE DE STATUT HTTP =====

        // Choisit le code de statut HTTP approprié selon le type d'erreur
        // Pattern matching C# moderne (switch expressions)
        var statusCode = exception switch
        {
            UnauthorizedAccessException => 401,     // Non autorisé
            ArgumentException => 400,               // Mauvaise requête
            FileNotFoundException => 404,           // Ressource non trouvée
            TimeoutException => 408,                // Timeout de requête
            _ => 500                                // Erreur serveur interne (défaut)
        };

        // ===== CRÉATION DE LA RÉPONSE D'ERREUR =====

        // Configure la réponse HTTP
        context.Response.StatusCode = statusCode;           // Définit le code de statut
        context.Response.ContentType = "application/json";  // Réponse en JSON

        // Crée un objet de réponse standardisé
        var response = new
        {
            // Message d'erreur à afficher à l'utilisateur
            Message = statusCode == 500
                ? "An internal server error occurred. Please try again later."  // Message générique pour erreur 500
                : exception.Message,                                            // Message spécifique pour autres erreurs 
            TraceId = context.TraceIdentifier,  // Identifiant unique de la requête pour le suivi/debugging
            Timestamp = DateTime.UtcNow,        // Horodatage de l'erreur
            Path = path,                        // Informations de contexte pour le debugging
            Method = method,                    // Informations de contexte pour le debugging
            StatusCode = statusCode             // Informations de contexte pour le debugging
        };

        // ===== ENVOI DE LA RÉPONSE AU CLIENT =====

        // Sérialise l'objet response en JSON et l'envoie au client
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}

/*
COMMENT UTILISER CE MIDDLEWARE :

1. Dans Program.cs, ajouter cette ligne AVANT les autres middlewares :
   app.UseMiddleware<GlobalExceptionMiddleware>();

2. Ce middleware va automatiquement :
   ✅ Capturer toutes les exceptions non gérées
   ✅ Logger les erreurs avec plein de détails
   ✅ Retourner des réponses JSON propres
   ✅ Éviter que l'application plante

3. Les logs seront sauvegardés dans les fichiers configurés avec Serilog
   et on pourra voir exactement qui a fait quoi quand une erreur arrive.

EXEMPLE DE LOG GÉNÉRÉ :
[15:30:45 ERR] 🚨 Unhandled exception occurred. Path: /api/books, Method: POST, 
Query: , UserId: user123, UserName: john@example.com, 
ClientIP: 192.168.1.100, UserAgent: Mozilla/5.0...
System.ArgumentException: Le titre du livre ne peut pas être vide
   at LibraryAPI.Controllers.BooksController.Create(Book book)
*/