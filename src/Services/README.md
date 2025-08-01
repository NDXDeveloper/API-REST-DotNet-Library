# Services

Le dossier `Services` contient les services métier de l'application LibraryAPI qui fournissent des fonctionnalités spécialisées aux contrôleurs. Ces services implémentent la logique métier et offrent des abstractions pour les opérations complexes avec gestion d'erreurs robuste et logging intégré.

## 🏗️ Architecture des Services

### **Principe de Design**

- **Separation of Concerns** : Logique métier séparée des contrôleurs
- **Dependency Injection** : Services injectés via DI container
- **Error Handling** : Gestion d'erreurs granulaire avec logging
- **Async/Await** : Operations asynchrones pour performance
- **Configuration** : Paramètres externalisés dans appsettings.json

## 📧 EmailService.cs

**Rôle** : Service d'envoi d'emails avancé avec templates HTML et gestion d'erreurs robuste

### **Fonctionnalités Principales**

```csharp
public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // Configuration SMTP sécurisée avec fallbacks
        var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"])
        {
            Port = int.TryParse(_configuration["EmailSettings:Port"], out int port) ? port : 587,
            Credentials = new NetworkCredential(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]
            ),
            EnableSsl = true,
        };

        // Validation des paramètres critiques
        var senderEmail = _configuration["EmailSettings:SenderEmail"];
        if (string.IsNullOrEmpty(senderEmail))
        {
            throw new InvalidOperationException("Sender email configuration is missing.");
        }

        // Construction du message avec métadonnées
        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, _configuration["EmailSettings:SenderName"] ?? "Library API"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(toEmail);
        await smtpClient.SendMailAsync(mailMessage);
    }
}
```

### **Configuration Requise** (appsettings.json)

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": "587",
    "SenderName": "Library API",
    "SenderEmail": "noreply@library.com",
    "AdminEmail": "admin@library.com",
    "Username": "your-smtp-username",
    "Password": "your-app-password"
  }
}
```

### **Utilisations dans l'Application**

#### **1. Emails de Bienvenue** (AuthController)

```csharp
// Template HTML riche avec CSS inline
var welcomeContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }}
        .email-container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 15px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 20px; }}
        .content {{ padding: 40px 30px; }}
        .welcome-message {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 25px; border-radius: 10px; }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1>📚 Bienvenue dans votre Bibliothèque Numérique !</h1>
        </div>
        <div class='content'>
            <h2>Bonjour {user.FullName} ! 👋</h2>
            <div class='welcome-message'>
                <h3>🎉 Félicitations !</h3>
                <p>Votre compte a été créé avec succès.</p>
            </div>
            <!-- Contenu riche avec informations utilisateur -->
        </div>
    </div>
</body>
</html>";

await _emailService.SendEmailAsync(user.Email, "🎉 Bienvenue !", welcomeContent);
```

#### **2. Notifications Administrateur** (BookMagazineController)

```csharp
// Email automatique lors d'upload de nouveau contenu
var emailContent = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <h2>📚 Nouveau livre ajouté !</h2>
    <div style='background: #f8f9fa; padding: 20px; border-radius: 8px;'>
        <h3>📖 {bookMagazine.Title}</h3>
        <p><strong>Auteur :</strong> {author.Name}</p>
        <p><strong>Catégorie :</strong> {category.Name}</p>
        <p><strong>Date d'ajout :</strong> {DateTime.Now:dd/MM/yyyy}</p>
        <p><strong>Ajouté par :</strong> {userId}</p>
    </div>
    <p>Ce livre est maintenant disponible pour tous les utilisateurs.</p>
</div>";

foreach (var admin in adminUsers)
{
    await _emailService.SendEmailAsync(admin.Email,
        "📚 Nouveau livre ajouté", emailContent);
}
```

#### **3. Alertes Critiques** (Serilog Configuration)

```csharp
// Configuration automatique pour erreurs système
.WriteTo.Email(
    options: new EmailSinkOptions
    {
        From = "noreply@library.com",
        To = new List<string> { "admin@library.com" },
        Subject = "[🚨 ERREUR CRITIQUE] LibraryAPI - {Level}",
        Body = "🚨 ERREUR CRITIQUE LibraryAPI 🚨\n\n" +
               "Time: {Timestamp}\nLevel: {Level}\nMessage: {Message}\nException: {Exception}"
    },
    restrictedToMinimumLevel: LogEventLevel.Error
)
```

### **Gestion d'Erreurs Avancée**

```csharp
public async Task<EmailResult> SendEmailWithRetryAsync(string toEmail, string subject, string body, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await SendEmailAsync(toEmail, subject, body);
            return EmailResult.Success();
        }
        catch (SmtpException ex) when (attempt < maxRetries)
        {
            // Retry avec backoff exponentiel
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            continue;
        }
        catch (Exception ex)
        {
            return EmailResult.Failure(ex.Message);
        }
    }
    return EmailResult.Failure("Max retries exceeded");
}
```

## 📊 AuditLogger.cs

**Rôle** : Service d'audit spécialisé pour traçabilité métier et conformité RGPD

### **Fonctionnalités Principales**

```csharp
public class AuditLogger
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ApplicationDbContext context,
                      IHttpContextAccessor httpContextAccessor,
                      ILogger<AuditLogger> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(string action, string message)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Message = message,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Logging technique séparé - ne jamais faire planter l'app
            _logger.LogError(ex, "Erreur lors de l'audit: {Action} - {Message}", action, message);
        }
    }
}
```

### **Actions Trackées** (AuditActions.cs)

#### **Authentification et Utilisateurs**

```csharp
// Actions utilisateur
await _auditLogger.LogAsync(AuditActions.LOGIN_SUCCESS,
    $"Connexion réussie pour: {user.Email}");
await _auditLogger.LogAsync(AuditActions.REGISTER,
    $"Nouvel utilisateur: {user.Email}");
await _auditLogger.LogAsync(AuditActions.PROFILE_UPDATED,
    $"Profil mis à jour: {user.Email}");
```

#### **Bibliothèque et Contenu**

```csharp
// Actions sur le contenu
await _auditLogger.LogAsync(AuditActions.BOOK_CREATED,
    $"Nouveau livre: '{book.Title}' par {author.Name}");
await _auditLogger.LogAsync(AuditActions.BOOK_DOWNLOADED,
    $"Téléchargement: '{book.Title}' (ID: {bookId})");
await _auditLogger.LogAsync(AuditActions.BOOK_RATED,
    $"Note {rating}/5 pour livre ID {bookId}");
```

#### **Sécurité et Système**

```csharp
// Événements sécuritaires
await _auditLogger.LogAsync(AuditActions.UNAUTHORIZED_ACCESS,
    $"Accès non autorisé sur {path} depuis {clientIP}");
await _auditLogger.LogAsync(AuditActions.RATE_LIMIT_EXCEEDED,
    $"Limite dépassée: {method} {path} par {userId}");
await _auditLogger.LogAsync(AuditActions.SYSTEM_STARTUP,
    $"Système démarré en environnement {environment}");
```

### **Consultation des Audits** (Admin/AuditController)

```csharp
[HttpGet("logs")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int size = 50)
{
    var logs = await _context.AuditLogs
        .OrderByDescending(a => a.CreatedAt)
        .Skip((page - 1) * size)
        .Take(size)
        .Select(a => new
        {
            a.Id, a.UserId, a.Action, a.Message,
            a.CreatedAt, a.IpAddress
        })
        .ToListAsync();

    return Ok(logs);
}

[HttpGet("stats")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetStats()
{
    var today = DateTime.UtcNow.Date;
    var stats = new
    {
        TotalLogs = await _context.AuditLogs.CountAsync(),
        TodayLogs = await _context.AuditLogs.CountAsync(a => a.CreatedAt >= today),
        LoginAttempts = await _context.AuditLogs.CountAsync(a => a.Action.Contains("LOGIN")),
        BookActions = await _context.AuditLogs.CountAsync(a => a.Action.Contains("BOOK"))
    };

    return Ok(stats);
}
```

## 🚀 Services Avancés Recommandés

### **FileManagementService.cs**

**Rôle** : Gestion avancée des fichiers avec sécurité renforcée

```csharp
public class FileManagementService
{
    private readonly ILogger<FileManagementService> _logger;
    private readonly IConfiguration _configuration;

    public async Task<FileUploadResult> SaveFileSecurelyAsync(IFormFile file, string category)
    {
        // Validation de sécurité
        if (!IsFileSecure(file))
        {
            return FileUploadResult.Failure("File security validation failed");
        }

        // Génération UUID sécurisé
        var uuid = Guid.NewGuid().ToString();
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{uuid}{extension}";

        // Détermination du dossier selon la catégorie
        var uploadsFolder = category switch
        {
            "books" => Path.Combine("wwwroot", "files"),
            "covers" => Path.Combine("wwwroot", "images", "covers"),
            "profiles" => Path.Combine("wwwroot", "images", "profiles"),
            _ => throw new ArgumentException("Invalid category")
        };

        // Création du dossier si nécessaire
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
            _logger.LogWarning("Created missing directory: {Path}", uploadsFolder);
        }

        // Sauvegarde sécurisée
        var filePath = Path.Combine(uploadsFolder, fileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return FileUploadResult.Success(fileName, filePath);
    }

    private bool IsFileSecure(IFormFile file)
    {
        // Vérification des signatures de fichiers (magic numbers)
        using var stream = file.OpenReadStream();
        var buffer = new byte[8];
        stream.Read(buffer, 0, 8);

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        return extension switch
        {
            ".pdf" => buffer.Take(4).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }),
            ".jpg" or ".jpeg" => buffer.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),
            ".png" => buffer.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            _ => false
        };
    }
}
```

### **NotificationService.cs**

**Rôle** : Service de notifications multi-canal

```csharp
public class NotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public async Task<NotificationResult> SendNotificationAsync(
        string userId, string subject, string content, NotificationChannel channels = NotificationChannel.All)
    {
        var results = new List<bool>();

        // Notification en base de données
        if (channels.HasFlag(NotificationChannel.Database))
        {
            results.Add(await SaveNotificationToDatabase(userId, subject, content));
        }

        // Notification par email
        if (channels.HasFlag(NotificationChannel.Email))
        {
            results.Add(await SendEmailNotification(userId, subject, content));
        }

        // Notification push (future)
        if (channels.HasFlag(NotificationChannel.Push))
        {
            results.Add(await SendPushNotification(userId, subject, content));
        }

        return new NotificationResult
        {
            Success = results.All(r => r),
            SuccessfulChannels = results.Count(r => r),
            TotalChannels = results.Count
        };
    }

    private async Task<bool> SaveNotificationToDatabase(string userId, string subject, string content)
    {
        try
        {
            var notification = new Notification
            {
                Subject = subject,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var userNotification = new UserNotification
            {
                UserId = userId,
                NotificationId = notification.Id,
                IsSent = true
            };

            _context.UserNotifications.Add(userNotification);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save notification to database");
            return false;
        }
    }
}

[Flags]
public enum NotificationChannel
{
    Database = 1,
    Email = 2,
    Push = 4,
    SMS = 8,
    All = Database | Email | Push | SMS
}
```

### **CacheService.cs**

**Rôle** : Service de cache avec invalidation intelligente

```csharp
public class CacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheService> _logger;

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        // Essayer le cache mémoire d'abord
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            return value;
        }

        // Puis le cache distribué
        var distributedValue = await _distributedCache.GetStringAsync(key);
        if (distributedValue != null)
        {
            var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue);

            // Remettre en cache mémoire
            _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));

            return deserializedValue;
        }

        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var cacheExpiration = expiration ?? TimeSpan.FromHours(1);

        // Cache mémoire
        _memoryCache.Set(key, value, cacheExpiration);

        // Cache distribué
        var serializedValue = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheExpiration
        };

        await _distributedCache.SetStringAsync(key, serializedValue, options);
    }

    public async Task InvalidateAsync(string pattern)
    {
        // Invalidation par pattern (ex: "books:*", "user:123:*")
        // Implementation dépend du provider de cache
    }
}
```

### **SearchService.cs**

**Rôle** : Service de recherche avancée

```csharp
public class SearchService
{
    private readonly ApplicationDbContext _context;
    private readonly CacheService _cacheService;

    public async Task<SearchResult<BookMagazine>> SearchBooksAsync(SearchCriteria criteria)
    {
        var cacheKey = $"search:{criteria.GetHashCode()}";
        var cachedResult = await _cacheService.GetAsync<SearchResult<BookMagazine>>(cacheKey);

        if (cachedResult != null)
        {
            return cachedResult;
        }

        var query = _context.BooksMagazines.AsQueryable();

        // Recherche textuelle
        if (!string.IsNullOrEmpty(criteria.Keyword))
        {
            query = query.Where(b =>
                EF.Functions.Like(b.Title, $"%{criteria.Keyword}%") ||
                EF.Functions.Like(b.Description, $"%{criteria.Keyword}%") ||
                EF.Functions.Like(b.Author.Name, $"%{criteria.Keyword}%") ||
                EF.Functions.Like(b.Tags, $"%{criteria.Keyword}%"));
        }

        // Filtres avancés
        if (criteria.CategoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == criteria.CategoryId.Value);
        }

        if (criteria.MinRating.HasValue)
        {
            query = query.Where(b => b.AverageRating >= criteria.MinRating.Value);
        }

        // Tri
        query = criteria.SortBy switch
        {
            "title" => criteria.SortDirection == "desc"
                ? query.OrderByDescending(b => b.Title)
                : query.OrderBy(b => b.Title),
            "rating" => criteria.SortDirection == "desc"
                ? query.OrderByDescending(b => b.AverageRating)
                : query.OrderBy(b => b.AverageRating),
            "date" => criteria.SortDirection == "desc"
                ? query.OrderByDescending(b => b.UploadDate)
                : query.OrderBy(b => b.UploadDate),
            _ => query.OrderByDescending(b => b.UploadDate)
        };

        // Pagination
        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Include(b => b.Author)
            .Include(b => b.Category)
            .ToListAsync();

        var result = new SearchResult<BookMagazine>
        {
            Items = items,
            TotalItems = totalItems,
            CurrentPage = criteria.Page,
            PageSize = criteria.PageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)criteria.PageSize)
        };

        // Cache du résultat
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        return result;
    }
}
```

## 🔧 Configuration et Registration

### **Startup/Program.cs Registration**

```csharp
// Services de base
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuditLogger>();

// Services avancés
builder.Services.AddScoped<FileManagementService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<SearchService>();

// Cache
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddScoped<CacheService>();

// HTTP Context pour audit
builder.Services.AddHttpContextAccessor();
```

### **Configuration avancée** (appsettings.json)

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": "587",
    "EnableSsl": true,
    "SenderEmail": "noreply@library.com",
    "SenderName": "Library API",
    "AdminEmail": "admin@library.com",
    "Username": "smtp-username",
    "Password": "app-password",
    "RetryAttempts": 3,
    "TimeoutSeconds": 30
  },
  "FileSettings": {
    "MaxFileSize": 104857600,
    "AllowedExtensions": [".pdf", ".epub", ".mobi", ".txt"],
    "AllowedImageExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"],
    "ScanForMalware": true,
    "QuarantinePath": "/quarantine"
  },
  "CacheSettings": {
    "DefaultExpirationMinutes": 60,
    "SearchResultsExpirationMinutes": 15,
    "UserDataExpirationMinutes": 30
  },
  "SearchSettings": {
    "MaxResultsPerPage": 100,
    "DefaultPageSize": 20,
    "EnableFullTextSearch": true
  }
}
```

## 📊 Monitoring et Métriques

### **Service Health Checks**

```csharp
// Dans Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<EmailServiceHealthCheck>("email")
    .AddCheck<CacheServiceHealthCheck>("cache")
    .AddCheck<FileSystemHealthCheck>("filesystem");
```

### **Métriques de Performance**

- **Email Service** : Taux de succès, temps de réponse SMTP
- **Audit Service** : Volume de logs, performance d'écriture
- **Cache Service** : Hit ratio, évictions, utilisation mémoire
- **Search Service** : Temps de réponse, requêtes populaires

## 🚀 Évolutions Recommandées

### **Microservices Ready**

- **Event Bus** : Communications inter-services
- **API Gateway** : Routage et authentification centralisée
- **Service Discovery** : Enregistrement automatique des services

### **Observabilité Avancée**

- **Distributed Tracing** : Suivi des requêtes cross-services
- **Metrics** : Prometheus + Grafana
- **APM** : Application Performance Monitoring

### **Sécurité Renforcée**

- **Secrets Management** : Azure Key Vault, HashiCorp Vault
- **Service-to-Service Auth** : Certificats, mutual TLS
- **Data Encryption** : Chiffrement au repos et en transit

L'architecture de services actuelle offre une base solide pour une application scalable avec séparation claire des responsabilités, gestion d'erreurs robuste et observabilité complète.
