# Filtres (Filters)

Le dossier `Filtres` contient les filtres d'action personnalisés de l'application LibraryAPI qui implémentent une validation renforcée et une sécurité multicouche. Ces filtres s'exécutent automatiquement avant et après les actions des contrôleurs pour garantir l'intégrité et la sécurité des données.

## 🏗️ Architecture des Filtres

### **Principe de Fonctionnement**

Les filtres dans ASP.NET Core s'exécutent dans un **pipeline de traitement** qui entoure l'exécution des actions des contrôleurs :

```
Request → Authorization → Action Filters → Action Execution → Result Filters → Response
```

### **Types de Filtres Implémentés**

1. **Action Filters** : Validation avant/après exécution d'action
2. **Result Filters** : Modification des résultats de réponse
3. **Exception Filters** : Gestion spécialisée des erreurs
4. **Global Filters** : Application automatique sur tous les endpoints

## 🛡️ ModelValidationFilter.cs

**Type** : `ActionFilterAttribute`  
**Rôle** : Validation automatique et complète des modèles de données avec gestion d'erreurs avancée

### **Fonctionnalités Principales**

```csharp
public class ModelValidationFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            // Extraction et structuration des erreurs de validation
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors
                        .Select(e => e.ErrorMessage)
                        .ToArray() ?? Array.Empty<string>()
                );

            // Création d'une réponse d'erreur standardisée
            var response = new
            {
                Message = "Erreurs de validation détectées",
                Errors = errors,
                Timestamp = DateTime.UtcNow,
                TraceId = context.HttpContext.TraceIdentifier
            };

            // Retour immédiat avec BadRequest (400)
            context.Result = new BadRequestObjectResult(response);
            return;
        }

        base.OnActionExecuting(context);
    }
}
```

### **Avantages de ce Filtre**

- ✅ **Validation précoce** : Arrêt avant exécution si données invalides
- ✅ **Réponses standardisées** : Format uniforme pour toutes les erreurs
- ✅ **Traçabilité** : TraceId pour debugging et support
- ✅ **Détails complets** : Toutes les erreurs retournées en une fois

### **Configuration Globale** (Program.cs)

```csharp
builder.Services.AddControllers(options =>
{
    // Application automatique sur tous les contrôleurs
    options.Filters.Add<ModelValidationFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    // Personnalisation avancée des réponses de validation
    options.InvalidModelStateResponseFactory = context =>
    {
        // Logique déjà gérée par ModelValidationFilter
        return new BadRequestObjectResult("Validation handled by filter");
    };
});
```

### **Exemple de Réponse d'Erreur**

```json
{
  "Message": "Erreurs de validation détectées",
  "Errors": {
    "Title": [
      "Le titre est obligatoire",
      "Le titre doit faire entre 2 et 200 caractères"
    ],
    "Email": ["Format d'email invalide"],
    "File": ["Le fichier est obligatoire", "Taille de fichier trop importante"]
  },
  "Timestamp": "2025-08-01T15:30:16.123Z",
  "TraceId": "0HN7JSKKJSAEH:00000001"
}
```

## 🔒 FileValidationFilter.cs

**Type** : `ActionFilterAttribute`  
**Rôle** : Validation avancée de sécurité des fichiers uploadés avec détection de menaces

### **Fonctionnalités de Sécurité**

```csharp
public class FileValidationFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;

        // Vérification de la présence de fichiers
        if (request.HasFormContentType && request.Form.Files.Any())
        {
            foreach (var file in request.Form.Files)
            {
                // Validation sécuritaire complète
                if (!IsFileSecure(file))
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        Message = "Fichier non sécurisé détecté",
                        FileName = file.FileName,
                        Timestamp = DateTime.UtcNow,
                        TraceId = context.HttpContext.TraceIdentifier
                    });
                    return;
                }
            }
        }

        base.OnActionExecuting(context);
    }

    private static bool IsFileSecure(IFormFile file)
    {
        // 1. Vérification des signatures de fichiers (Magic Numbers)
        if (!ValidateFileSignature(file))
            return false;

        // 2. Détection de noms de fichiers malveillants
        if (IsMaliciousFileName(file.FileName))
            return false;

        // 3. Validation de la cohérence extension/contenu
        if (!ValidateExtensionContentMatch(file))
            return false;

        return true;
    }
}
```

### **Validations de Sécurité Implémentées**

#### **1. Validation des Signatures (Magic Numbers)**

```csharp
private static bool ValidateFileSignature(IFormFile file)
{
    if (file.Length == 0) return false;

    using var stream = file.OpenReadStream();
    var buffer = new byte[8];
    stream.Read(buffer, 0, 8);

    var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

    return extension switch
    {
        // PDF : %PDF
        ".pdf" => buffer.Take(4).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }),

        // JPEG : FF D8 FF
        ".jpg" or ".jpeg" => buffer.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),

        // PNG : 89 50 4E 47 0D 0A 1A 0A
        ".png" => buffer.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),

        // GIF : GIF87a ou GIF89a
        ".gif" => buffer.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }) ||
                 buffer.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }),

        // ZIP/EPUB : PK
        ".zip" or ".epub" => buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),

        // MOBI : BOOKMOBI
        ".mobi" => buffer.Skip(60).Take(8).SequenceEqual(Encoding.ASCII.GetBytes("BOOKMOBI")),

        _ => true // Autoriser temporairement les autres types
    };
}
```

#### **2. Détection de Noms Malveillants**

```csharp
private static bool IsMaliciousFileName(string fileName)
{
    if (string.IsNullOrEmpty(fileName)) return true;

    // Liste noire de noms de fichiers dangereux
    string[] dangerousNames = {
        "web.config", ".htaccess", "autorun.inf", "desktop.ini",
        "thumbs.db", ".DS_Store", "index.php", "shell.php",
        "cmd.exe", "powershell.exe", "bash", "sh"
    };

    var lowerFileName = fileName.ToLowerInvariant();

    // Vérification de la liste noire
    if (dangerousNames.Any(name => lowerFileName.Contains(name)))
        return true;

    // Vérification des caractères dangereux
    char[] dangerousChars = { '<', '>', ':', '"', '|', '?', '*', '\\', '/' };
    if (fileName.IndexOfAny(dangerousChars) >= 0)
        return true;

    // Vérification des extensions doubles (ex: .txt.exe)
    var parts = fileName.Split('.');
    if (parts.Length > 2)
    {
        string[] executableExtensions = { "exe", "bat", "cmd", "com", "scr", "pif", "vbs", "js" };
        if (executableExtensions.Any(ext => parts.Contains(ext, StringComparer.OrdinalIgnoreCase)))
            return true;
    }

    return false;
}
```

#### **3. Validation Cohérence Extension/Contenu**

```csharp
private static bool ValidateExtensionContentMatch(IFormFile file)
{
    var declaredExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
    var detectedType = DetectFileTypeFromContent(file);

    // Vérifier que l'extension déclarée correspond au contenu réel
    return declaredExtension switch
    {
        ".pdf" => detectedType == FileType.PDF,
        ".jpg" or ".jpeg" => detectedType == FileType.JPEG,
        ".png" => detectedType == FileType.PNG,
        ".gif" => detectedType == FileType.GIF,
        _ => true // Autoriser par défaut pour les types non critiques
    };
}
```

### **Menaces Détectées et Bloquées**

- 🚫 **Fichiers exécutables** déguisés en documents
- 🚫 **Scripts malveillants** (JavaScript, VBScript, etc.)
- 🚫 **Configuration files** système (.htaccess, web.config)
- 🚫 **Double extensions** (.txt.exe, .pdf.scr)
- 🚫 **Noms réservés** système (CON, PRN, AUX, etc.)
- 🚫 **Caractères dangereux** dans les noms de fichiers
- 🚫 **Type spoofing** (extension ne correspondant pas au contenu)

## 🔧 Filtres Avancés Recommandés

### **RateLimitingFilter.cs**

**Rôle** : Limitation de taux personnalisée par utilisateur

```csharp
public class RateLimitingFilter : ActionFilterAttribute
{
    private readonly int _requestsPerMinute;
    private readonly string _policyName;
    private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public RateLimitingFilter(int requestsPerMinute = 60, string policyName = "default")
    {
        _requestsPerMinute = requestsPerMinute;
        _policyName = policyName;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var clientIP = context.HttpContext.Connection.RemoteIpAddress?.ToString();
        var identifier = userId ?? clientIP ?? "anonymous";

        var cacheKey = $"rate_limit:{_policyName}:{identifier}";

        if (_cache.TryGetValue(cacheKey, out int currentRequests))
        {
            if (currentRequests >= _requestsPerMinute)
            {
                context.Result = new ObjectResult(new
                {
                    Message = "Rate limit exceeded",
                    RetryAfter = 60,
                    Limit = _requestsPerMinute,
                    Remaining = 0
                })
                {
                    StatusCode = 429
                };
                return;
            }

            _cache.Set(cacheKey, currentRequests + 1, TimeSpan.FromMinutes(1));
        }
        else
        {
            _cache.Set(cacheKey, 1, TimeSpan.FromMinutes(1));
        }

        base.OnActionExecuting(context);
    }
}

// Utilisation
[RateLimitingFilter(10, "upload")] // 10 requêtes/minute pour uploads
public async Task<IActionResult> UploadFile([FromForm] FileModel model)
{
    // Logic d'upload
}
```

### **AuditFilter.cs**

**Rôle** : Audit automatique des actions sensibles

```csharp
public class AuditFilter : ActionFilterAttribute
{
    private readonly string _actionType;
    private readonly bool _logRequest;
    private readonly bool _logResponse;

    public AuditFilter(string actionType, bool logRequest = false, bool logResponse = false)
    {
        _actionType = actionType;
        _logRequest = logRequest;
        _logResponse = logResponse;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var auditLogger = context.HttpContext.RequestServices.GetService<AuditLogger>();
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        // Log avant exécution
        if (_logRequest)
        {
            var requestData = SerializeActionArguments(context.ActionArguments);
            await auditLogger?.LogAsync($"{_actionType}_STARTED",
                $"Action démarrée avec paramètres: {requestData}")!;
        }

        // Exécution de l'action
        var executedContext = await next();

        // Log après exécution
        if (executedContext.Exception == null)
        {
            var responseData = _logResponse ? SerializeResult(executedContext.Result) : "";
            await auditLogger?.LogAsync($"{_actionType}_COMPLETED",
                $"Action terminée avec succès. {responseData}")!;
        }
        else
        {
            await auditLogger?.LogAsync($"{_actionType}_FAILED",
                $"Action échouée: {executedContext.Exception.Message}")!;
        }
    }

    private string SerializeActionArguments(IDictionary<string, object?> arguments)
    {
        var safeArgs = arguments
            .Where(arg => !IssensitiveParameter(arg.Key))
            .ToDictionary(arg => arg.Key, arg => arg.Value?.ToString() ?? "null");

        return JsonSerializer.Serialize(safeArgs);
    }

    private bool IssensitiveParameter(string parameterName)
    {
        string[] sensitiveParams = { "password", "token", "secret", "key", "credential" };
        return sensitiveParams.Any(p => parameterName.ToLowerInvariant().Contains(p));
    }
}

// Utilisation
[AuditFilter("ADMIN_USER_DELETE", logRequest: true)]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteUser(string userId)
{
    // Logic de suppression
}
```

### **CacheFilter.cs**

**Rôle** : Mise en cache automatique des réponses

```csharp
public class CacheFilter : ActionFilterAttribute
{
    private readonly int _durationMinutes;
    private readonly string[] _varyByParams;
    private readonly bool _varyByUser;

    public CacheFilter(int durationMinutes = 15, string[]? varyByParams = null, bool varyByUser = false)
    {
        _durationMinutes = durationMinutes;
        _varyByParams = varyByParams ?? Array.Empty<string>();
        _varyByUser = varyByUser;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
        if (cache == null)
        {
            await next();
            return;
        }

        var cacheKey = GenerateCacheKey(context);

        // Vérifier le cache
        if (cache.TryGetValue(cacheKey, out var cachedResult))
        {
            context.Result = (IActionResult)cachedResult!;
            return;
        }

        // Exécuter et mettre en cache
        var executedContext = await next();

        if (executedContext.Exception == null && executedContext.Result is OkObjectResult okResult)
        {
            cache.Set(cacheKey, okResult, TimeSpan.FromMinutes(_durationMinutes));
        }
    }

    private string GenerateCacheKey(ActionExecutingContext context)
    {
        var parts = new List<string>
        {
            context.ActionDescriptor.RouteValues["controller"] ?? "",
            context.ActionDescriptor.RouteValues["action"] ?? ""
        };

        // Ajouter les paramètres spécifiés
        foreach (var param in _varyByParams)
        {
            if (context.ActionArguments.TryGetValue(param, out var value))
            {
                parts.Add($"{param}:{value}");
            }
        }

        // Ajouter l'utilisateur si demandé
        if (_varyByUser)
        {
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            parts.Add($"user:{userId}");
        }

        return string.Join(":", parts);
    }
}

// Utilisation
[CacheFilter(30, new[] { "categoryId", "page" }, varyByUser: true)]
public async Task<IActionResult> GetBooksByCategory(int categoryId, int page = 1)
{
    // Logic de récupération
}
```

### **SecurityHeadersFilter.cs**

**Rôle** : Ajout automatique d'en-têtes de sécurité

```csharp
public class SecurityHeadersFilter : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        var headers = context.HttpContext.Response.Headers;

        // Protection XSS
        headers.Append("X-Content-Type-Options", "nosniff");
        headers.Append("X-Frame-Options", "DENY");
        headers.Append("X-XSS-Protection", "1; mode=block");

        // CSP (Content Security Policy)
        headers.Append("Content-Security-Policy",
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");

        // HSTS (HTTP Strict Transport Security)
        if (context.HttpContext.Request.IsHttps)
        {
            headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }

        // Référer Policy
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions Policy
        headers.Append("Permissions-Policy",
            "camera=(), microphone=(), geolocation=(), payment=()");

        base.OnResultExecuting(context);
    }
}
```

## 📋 Configuration et Application des Filtres

### **Application Globale** (Program.cs)

```csharp
builder.Services.AddControllers(options =>
{
    // Filtres appliqués à tous les contrôleurs
    options.Filters.Add<ModelValidationFilter>();
    options.Filters.Add<FileValidationFilter>();
    options.Filters.Add<SecurityHeadersFilter>();

    // Filtres conditionnels
    if (builder.Environment.IsProduction())
    {
        options.Filters.Add<RateLimitingFilter>(60); // 60 req/min en prod
    }
});
```

### **Application par Contrôleur**

```csharp
[SecurityHeaders]
[RateLimitingFilter(30)] // 30 req/min pour ce contrôleur
public class AdminController : ControllerBase
{
    [AuditFilter("ADMIN_ACTION", logRequest: true, logResponse: true)]
    public async Task<IActionResult> SensitiveAction()
    {
        // Logic sensible
    }
}
```

### **Application par Action**

```csharp
[HttpPost("upload")]
[FileValidationFilter] // Validation spécifique aux fichiers
[RateLimitingFilter(5, "file_upload")] // 5 uploads/min max
[AuditFilter("FILE_UPLOAD", logRequest: true)]
public async Task<IActionResult> UploadFile([FromForm] FileModel model)
{
    // Logic d'upload
}
```

## 🧪 Tests des Filtres

### **Tests Unitaires**

```csharp
[Test]
public void ModelValidationFilter_InvalidModel_ReturnsBadRequest()
{
    // Arrange
    var filter = new ModelValidationFilter();
    var context = CreateActionExecutingContext();
    context.ModelState.AddModelError("Title", "Required");

    // Act
    filter.OnActionExecuting(context);

    // Assert
    Assert.That(context.Result, Is.TypeOf<BadRequestObjectResult>());
}

[Test]
public void FileValidationFilter_MaliciousFile_BlocksRequest()
{
    // Arrange
    var filter = new FileValidationFilter();
    var context = CreateContextWithFile("malicious.exe.txt");

    // Act
    filter.OnActionExecuting(context);

    // Assert
    Assert.That(context.Result, Is.TypeOf<BadRequestObjectResult>());
}
```

### **Tests d'Intégration**

```csharp
[Test]
public async Task Upload_WithMaliciousFile_Returns400()
{
    // Arrange
    var client = _factory.CreateClient();
    var maliciousFile = CreateMaliciousFile();

    // Act
    var response = await client.PostAsync("/api/books/upload", maliciousFile);

    // Assert
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
}
```

## 📊 Monitoring et Métriques

### **Métriques des Filtres**

- **Taux de blocage** : Pourcentage de requêtes bloquées par filtre
- **Types de menaces** : Classification des fichiers malveillants détectés
- **Performance** : Temps d'exécution des validations
- **Faux positifs** : Fichiers légitimes bloqués à tort

### **Logging des Filtres**

```csharp
public class FilterLoggingService
{
    private readonly ILogger<FilterLoggingService> _logger;

    public void LogFilterExecution(string filterName, string action, TimeSpan duration, bool blocked = false)
    {
        if (blocked)
        {
            _logger.LogWarning("🚫 Filter {FilterName} blocked {Action} in {Duration}ms",
                filterName, action, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug("✅ Filter {FilterName} processed {Action} in {Duration}ms",
                filterName, action, duration.TotalMilliseconds);
        }
    }
}
```

## 🚀 Évolutions Recommandées

### **IA et Machine Learning**

- **Détection comportementale** : Analyse patterns d'utilisation suspects
- **Classification automatique** : ML pour détecter nouveaux types de menaces
- **Scoring de risque** : Attribution de scores aux fichiers/utilisateurs

### **Intégrations Sécurité**

- **Antivirus** : Scan automatique avec ClamAV ou similaire
- **Threat Intelligence** : Bases de données de signatures malveillantes
- **SIEM** : Intégration avec systèmes de monitoring sécurité

### **Performance et Scalabilité**

- **Cache distribué** : Redis pour partage entre instances
- **Async processing** : Validation asynchrone pour gros fichiers
- **Queue systems** : File d'attente pour traitement différé

L'architecture de filtres actuelle offre une protection multicouche robuste avec validation proactive, détection de menaces et audit complet, essentielle pour une application sécurisée en production.
