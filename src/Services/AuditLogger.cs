// Services/AuditLogger.cs
using LibraryAPI.Data;
using LibraryAPI.Models;
using System.Security.Claims;


    public class AuditLogger
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogger> _logger;

        public AuditLogger(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor,
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
                // Important : ne jamais faire planter l'app à cause des logs
                _logger.LogError(ex, "Erreur lors de l'audit: {Action} - {Message}", action, message);
            }
        }
    }
