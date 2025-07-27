using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryAPI.Filters
{
    /// <summary>
    /// Filtre pour limiter le taux de requêtes par utilisateur
    /// </summary>
    public class RateLimitingFilter : ActionFilterAttribute
    {
        private static readonly Dictionary<string, DateTime> LastRequestTimes = new();
        private static readonly Dictionary<string, int> RequestCounts = new();
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;

        public RateLimitingFilter(int maxRequests = 100, int timeWindowMinutes = 15)
        {
            _maxRequests = maxRequests;
            _timeWindow = TimeSpan.FromMinutes(timeWindowMinutes);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var clientId = GetClientIdentifier(context.HttpContext);
            var now = DateTime.UtcNow;

            lock (LastRequestTimes)
            {
                // Réinitialiser si la fenêtre de temps est écoulée
                if (LastRequestTimes.TryGetValue(clientId, out var lastTime) &&
                    now - lastTime > _timeWindow)
                {
                    RequestCounts[clientId] = 0;
                }

                LastRequestTimes[clientId] = now;
                RequestCounts[clientId] = RequestCounts.GetValueOrDefault(clientId, 0) + 1;

                if (RequestCounts[clientId] > _maxRequests)
                {
                    context.Result = new StatusCodeResult(429); // Too Many Requests
                    return;
                }
            }

            base.OnActionExecuting(context);
        }

        private static string GetClientIdentifier(HttpContext context)
        {
            // Utiliser l'ID utilisateur si disponible, sinon l'IP
            var userId = context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userId))
            {
                return userId;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}