// Controllers/Admin/AuditController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;

namespace LibraryAPI.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int size = 50)
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    a.Action,
                    a.Message,
                    a.CreatedAt,
                    a.IpAddress
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("stats")]
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
    }
}


/* pour tester :

### Connexion (génère un log)
POST https://localhost:5001/api/Auth/login
Content-Type: application/json

{
  "email": "admin@library.com",
  "password": "AdminPass123!"
}

### Consulter les logs (Admin)
GET https://localhost:5001/api/admin/audit/logs?page=1&size=10
Authorization: Bearer YOUR_ADMIN_TOKEN

### Statistiques
GET https://localhost:5001/api/admin/audit/stats
Authorization: Bearer YOUR_ADMIN_TOKEN
*/