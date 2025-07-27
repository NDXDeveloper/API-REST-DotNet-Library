using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Middleware
{
    /// <summary>
    /// Middleware pour gérer les exceptions de validation
    /// </summary>
    public class ValidationExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ValidationExceptionMiddleware> _logger;

        public ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Erreur de validation: {Message}", ex.Message);
                await HandleValidationExceptionAsync(context, ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument invalide: {Message}", ex.Message);
                await HandleArgumentExceptionAsync(context, ex);
            }
        }

        private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Message = "Erreur de validation",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private static async Task HandleArgumentExceptionAsync(HttpContext context, ArgumentException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Message = "Paramètre invalide",
                Error = ex.Message,
                Parameter = ex.ParamName,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }

}
