using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Middleware
{
    /// <summary>
    /// Middleware pour loguer les tentatives de validation échouées
    /// </summary>
    public class ValidationLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ValidationLoggingMiddleware> _logger;

        public ValidationLoggingMiddleware(RequestDelegate next, ILogger<ValidationLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == "POST" || context.Request.Method == "PUT")
            {
                var originalBodyStream = context.Response.Body;
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await _next(context);

                // Loguer les erreurs de validation
                if (context.Response.StatusCode == 400)
                {
                    var userId = context.User?.Identity?.Name ?? "Anonymous";
                    var endpoint = context.Request.Path;

                    _logger.LogWarning(
                        "Échec de validation pour l'utilisateur {UserId} sur {Endpoint}. IP: {IP}",
                        userId,
                        endpoint,
                        context.Connection.RemoteIpAddress
                    );
                }

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
