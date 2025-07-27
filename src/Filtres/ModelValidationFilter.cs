using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Filters
{
    /// <summary>
    /// Filtre de validation automatique qui vérifie les modèles avant l'exécution des actions
    /// </summary>
    public class ModelValidationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                var response = new
                {
                    Message = "Erreurs de validation détectées",
                    Errors = errors,
                    Timestamp = DateTime.UtcNow
                };

                context.Result = new BadRequestObjectResult(response);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}