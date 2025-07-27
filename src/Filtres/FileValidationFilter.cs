using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryAPI.Filters
{
    /// <summary>
    /// Filtre spécialisé pour la validation des fichiers uploadés
    /// </summary>
    public class FileValidationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;

            // Vérifier si c'est un upload de fichier
            if (request.HasFormContentType && request.Form.Files.Any())
            {
                foreach (var file in request.Form.Files)
                {
                    // Vérifications de sécurité supplémentaires
                    if (!IsFileSecure(file))
                    {
                        context.Result = new BadRequestObjectResult(new
                        {
                            Message = "Fichier non sécurisé détecté",
                            FileName = file.FileName,
                            Timestamp = DateTime.UtcNow
                        });
                        return;
                    }
                }
            }

            base.OnActionExecuting(context);
        }

        private static bool IsFileSecure(IFormFile file)
        {
            // Vérifier la signature du fichier (magic numbers)
            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var buffer = new byte[8];
                stream.Read(buffer, 0, 8);

                // Vérifier que le fichier correspond à son extension
                var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (!IsValidFileSignature(buffer, extension))
                {
                    return false;
                }
            }

            // Vérifier les noms de fichiers malveillants
            string[] dangerousNames = { "web.config", ".htaccess", "autorun.inf", "desktop.ini" };
            if (dangerousNames.Any(name => file.FileName.ToLowerInvariant().Contains(name)))
            {
                return false;
            }

            return true;
        }

        private static bool IsValidFileSignature(byte[] buffer, string? extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;

            return extension switch
            {
                ".pdf" => buffer.Take(4).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }), // %PDF
                ".jpg" or ".jpeg" => buffer.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),
                ".png" => buffer.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".gif" => buffer.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }) ||
                         buffer.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }),
                ".zip" or ".epub" => buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
                _ => true // Autoriser les autres types pour l'instant
            };
        }
    }
}