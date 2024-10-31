using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims; // Utilisé pour manipuler les informations des utilisateurs (claims) dans les tokens d'authentification, comme l'identifiant de l'utilisateur (UserId).
using LibraryAPI.Data;
using LibraryAPI.Models;

namespace LibraryAPI.Controllers
{

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
                if (string.IsNullOrEmpty(user.Email))
                {
                    // Gérez le cas où l'email de l'utilisateur est null ou vide
                    return BadRequest("User email is missing. Notification cannot be sent.");
                }

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

        if (userNotification == null || userNotification.Notification == null)
            return NotFound("Notification not found for this user.");


        userNotification.Notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok("Notification marked as read.");
    }
}

}
