 
## Partie 7 - Notifications : Tutoriel d'Implémentation

### Objectif

L'objectif de cette partie est de notifier les utilisateurs par email lorsqu'il y a une nouvelle publication, une mise à jour de contenu, ou un commentaire reçu sur une ressource.

### Étapes de l’implémentation

### 1. Création des Modèles de Données pour les Notifications

#### Modèle `Notification.cs`

Ce modèle représente une notification dans le système, stockant le contenu de la notification, la date de création, et son statut (lue ou non).

```csharp
using System;
using System.ComponentModel.DataAnnotations;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } // Message de la notification

    public DateTime CreatedAt { get; set; } = DateTime.Now; // Date de création

    public bool IsRead { get; set; } = false; // Indicateur si la notification est lue
}
```

#### Modèle `UserNotification.cs`

Ce modèle gère l'association entre une notification et un utilisateur, en gardant un état de notification envoyé ou non.

```csharp
using System.ComponentModel.DataAnnotations;

public class UserNotification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } // ID de l'utilisateur

    [Required]
    public int NotificationId { get; set; } // ID de la notification

    public Notification Notification { get; set; } // Relation avec Notification

    public bool IsSent { get; set; } = false; // Indicateur si la notification est envoyée
}
```

### 2. Mise à jour du Contexte de Base de Données

Ajoutez les nouveaux modèles dans `ApplicationDbContext` pour qu’ils soient pris en charge dans la base de données.

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserNotification> UserNotifications { get; set; }
}
```

### 3. Ajout de la Migration et Mise à Jour de la Base de Données

Créez une migration pour ajouter ces modèles à la base de données.

```bash
dotnet ef migrations add AddNotifications
dotnet ef database update
```

### 4. Configuration du Service d’Email

Pour envoyer des notifications par email, nous allons configurer un service d’envoi d’emails via SMTP.

#### Ajouter la Configuration SMTP dans `appsettings.json`

```json
"EmailSettings": {
    "SmtpServer": "smtp.example.com",
    "Port": 587,
    "SenderName": "Notifications MyApp",
    "SenderEmail": "no-reply@myapp.com",
    "Username": "smtp_username",
    "Password": "smtp_password"
}
```

#### Création de `EmailService.cs`

Ce service utilise les paramètres SMTP pour envoyer des emails.

```csharp
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"])
        {
            Port = int.Parse(_configuration["EmailSettings:Port"]),
            Credentials = new NetworkCredential(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]
            ),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_configuration["EmailSettings:SenderEmail"], _configuration["EmailSettings:SenderName"]),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
    }
}
```

### 5. Création des Routes d’API pour les Notifications

#### `NotificationController.cs`

Ce contrôleur permet de créer des notifications, de les envoyer par email et de les marquer comme lues.

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;

    public NotificationController(ApplicationDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // *** Créer une notification ***
    [HttpPost("create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateNotification([FromBody] string content)
    {
        var notification = new Notification { Content = content };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Associer la notification à tous les utilisateurs
        var users = _context.Users.Select(u => u.Id).ToList();
        foreach (var userId in users)
        {
            _context.UserNotifications.Add(new UserNotification
            {
                UserId = userId,
                NotificationId = notification.Id
            });
        }

        await _context.SaveChangesAsync();

        return Ok("Notification created successfully.");
    }

    // *** Envoyer des notifications par email ***
    [HttpPost("send-emails")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendEmails()
    {
        var pendingNotifications = _context.UserNotifications
            .Where(un => !un.IsSent)
            .ToList();

        foreach (var userNotification in pendingNotifications)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userNotification.UserId);
            if (user != null)
            {
                var notification = _context.Notifications.FirstOrDefault(n => n.Id == userNotification.NotificationId);
                if (notification != null)
                {
                    await _emailService.SendEmailAsync(user.Email, "New Notification", notification.Content);
                    userNotification.IsSent = true;
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok("Emails sent successfully.");
    }

    // *** Marquer une notification comme lue ***
    [HttpPost("mark-as-read/{notificationId}")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userNotification = await _context.UserNotifications
            .FirstOrDefaultAsync(un => un.NotificationId == notificationId && un.UserId == userId);

        if (userNotification == null)
            return NotFound("Notification not found for this user.");

        userNotification.Notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok("Notification marked as read.");
    }
}
```

### 6. Tests des Fonctions de Notifications

Voici comment tester ces fonctionnalités :

1. **Créer une notification** en appelant `POST /api/Notification/create` et en envoyant le message de la notification.
2. **Envoyer les notifications par email** en appelant `POST /api/Notification/send-emails`. Cela enverra un email à chaque utilisateur associé à la notification.
3. **Marquer la notification comme lue** en appelant `POST /api/Notification/mark-as-read/{notificationId}`.


