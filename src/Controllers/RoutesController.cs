
// ===== CONTRÔLEUR POUR LISTER TOUTES LES ROUTES =====
//
//       https://localhost:5001/api/routes/list
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints; 
using System.Reflection;

namespace LibraryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoutesController : ControllerBase
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

        public RoutesController(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        [HttpGet("list")]
        public IActionResult GetAllRoutes()
        {
            var routes = _actionDescriptorCollectionProvider.ActionDescriptors.Items
                .Where(x => x is ControllerActionDescriptor)
                .Cast<ControllerActionDescriptor>()
                .Select(action => new
                {
                    Controller = action.ControllerName,
                    Action = action.ActionName,
                    Method = string.Join(", ", action.ActionConstraints?.OfType<HttpMethodActionConstraint>()
                        .FirstOrDefault()?.HttpMethods ?? new[] { "GET" }),
                    Route = $"/{action.AttributeRouteInfo?.Template ?? $"api/{action.ControllerName}/{action.ActionName}"}",
                    Authorization = GetAuthorizationInfo(action.MethodInfo),
                    Roles = GetRolesInfo(action.MethodInfo)
                })
                .OrderBy(x => x.Controller)
                .ThenBy(x => x.Route)
                .ToList();

            return Ok(routes);
        }

        private string GetAuthorizationInfo(MethodInfo methodInfo)
        {
            // Vérifier l'attribut [Authorize] sur la méthode
            var methodAuth = methodInfo.GetCustomAttribute<AuthorizeAttribute>();
            if (methodAuth != null)
            {
                return "Required";
            }

            // Vérifier l'attribut [Authorize] sur le contrôleur
            var controllerAuth = methodInfo.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>();
            if (controllerAuth != null)
            {
                return "Required (Controller)";
            }

            // Vérifier l'attribut [AllowAnonymous]
            var allowAnonymous = methodInfo.GetCustomAttribute<AllowAnonymousAttribute>();
            if (allowAnonymous != null)
            {
                return "Anonymous";
            }

            return "Public";
        }

        private string GetRolesInfo(MethodInfo methodInfo)
        {
            // Vérifier les rôles sur la méthode
            var methodAuth = methodInfo.GetCustomAttribute<AuthorizeAttribute>();
            if (methodAuth?.Roles != null)
            {
                return methodAuth.Roles;
            }

            // Vérifier les rôles sur le contrôleur
            var controllerAuth = methodInfo.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>();
            if (controllerAuth?.Roles != null)
            {
                return controllerAuth.Roles;
            }

            // ✅ CORRECTION : Vérifier si une autorisation existe avant de dire "Any authenticated user"
            if (methodAuth != null || controllerAuth != null)
            {
                return "Any authenticated user";
            }

            // ✅ CORRECTION : Pour les endpoints vraiment publics
            return "No auth required";
        }
    }
}