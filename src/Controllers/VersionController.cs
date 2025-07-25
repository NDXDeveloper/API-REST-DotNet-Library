using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace LibraryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Récupérer les métadonnées Git injectées lors du build
            var gitTag = GetAssemblyMetadata(assembly, "GitTag") ?? "unknown";
            var gitCommit = GetAssemblyMetadata(assembly, "GitCommit") ?? "unknown";
            var gitBranch = GetAssemblyMetadata(assembly, "GitBranch") ?? "unknown";
            var gitDirty = GetAssemblyMetadata(assembly, "GitDirty") ?? "";
            var buildTime = GetAssemblyMetadata(assembly, "BuildTime") ?? "unknown";
            
            // Informations de version principales
            var version = assembly.GetName().Version?.ToString() ?? "unknown";
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "unknown";
            
            return Ok(new
            {
                // Version principale
                Version = version,
                InformationalVersion = informationalVersion,
                
                // Informations Git (maintenant correctement récupérées)
                GitTag = gitTag,
                GitCommit = gitCommit,
                GitBranch = gitBranch,
                GitDirty = gitDirty,
                
                // Informations build
                BuildDate = buildTime,
                BuildMachine = Environment.MachineName,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                
                // Informations runtime
                DotNetVersion = Environment.Version.ToString(),
                Platform = Environment.OSVersion.ToString(),
                
                // Informations supplémentaires utiles
                ApiName = "LibraryAPI",
                Company = "NDXDeveloper",
                Copyright = "© 2025 Nicolas DEOUX"
            });
        }

        private static string? GetAssemblyMetadata(Assembly assembly, string key)
        {
            try 
            {
                return assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(x => x.Key == key)?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}