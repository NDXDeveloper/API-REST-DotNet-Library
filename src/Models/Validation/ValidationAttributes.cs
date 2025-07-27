using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LibraryAPI.Models.Validation
{
    /// <summary>
    /// Attribut de validation personnalisé pour les fichiers uploadés
    /// </summary>
    public class FileValidationAttribute : ValidationAttribute
    {
        public long MaxSize { get; set; } = 50 * 1024 * 1024; // 50MB par défaut
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
        public string[] AllowedMimeTypes { get; set; } = Array.Empty<string>();

        public override bool IsValid(object? value)
        {
            if (value == null) return true; // Fichier optionnel

            if (value is not IFormFile file)
            {
                ErrorMessage = "Le fichier fourni n'est pas valide";
                return false;
            }

            // Vérification de la taille
            if (file.Length > MaxSize)
            {
                ErrorMessage = $"Le fichier dépasse la taille maximale autorisée de {MaxSize / (1024 * 1024)}MB";
                return false;
            }

            // Vérification de l'extension
            if (AllowedExtensions.Length > 0)
            {
                var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                {
                    ErrorMessage = $"Extensions autorisées : {string.Join(", ", AllowedExtensions)}";
                    return false;
                }
            }

            // Vérification du type MIME
            if (AllowedMimeTypes.Length > 0)
            {
                if (string.IsNullOrEmpty(file.ContentType) || !AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                {
                    ErrorMessage = $"Types de fichiers autorisés : {string.Join(", ", AllowedMimeTypes)}";
                    return false;
                }
            }

            // Vérification du nom de fichier (sécurité)
            if (string.IsNullOrEmpty(file.FileName) || file.FileName.Length > 255)
            {
                ErrorMessage = "Le nom de fichier est invalide ou trop long (max 255 caractères)";
                return false;
            }

            // Vérification des caractères dangereux dans le nom
            char[] dangerousChars = { '<', '>', ':', '"', '|', '?', '*', '\\', '/' };
            if (file.FileName.IndexOfAny(dangerousChars) >= 0)
            {
                ErrorMessage = "Le nom de fichier contient des caractères non autorisés";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Attribut pour valider les noms (auteurs, catégories, etc.)
    /// </summary>
    public class SafeNameValidationAttribute : ValidationAttribute
    {
        public int MinLength { get; set; } = 2;
        public int MaxLength { get; set; } = 100;

        public override bool IsValid(object? value)
        {
            if (value == null) return true;

            if (value is not string name)
            {
                ErrorMessage = "La valeur doit être une chaîne de caractères";
                return false;
            }

            name = name.Trim();

            // Vérification de la longueur
            if (name.Length < MinLength || name.Length > MaxLength)
            {
                ErrorMessage = $"La longueur doit être entre {MinLength} et {MaxLength} caractères";
                return false;
            }

            // Vérification des caractères autorisés (lettres, chiffres, espaces, tirets, apostrophes, points, virgules)
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-ZÀ-ÿ0-9\s\-\.\'\,\!\?\(\)]+$"))
            {
                ErrorMessage = "Seuls les lettres, chiffres, espaces et caractères de ponctuation de base sont autorisés";
                return false;
            }

            // Pas de caractères consécutifs d'espacement
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"\s{2,}"))
            {
                ErrorMessage = "Les espaces multiples consécutifs ne sont pas autorisés";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Attribut pour valider les tags
    /// </summary>
    public class TagsValidationAttribute : ValidationAttribute
    {
        public int MaxTags { get; set; } = 10;
        public int MaxTagLength { get; set; } = 30;

        public override bool IsValid(object? value)
        {
            if (value == null) return true;

            if (value is not string tagsString)
            {
                ErrorMessage = "Les tags doivent être une chaîne de caractères";
                return false;
            }

            if (string.IsNullOrWhiteSpace(tagsString)) return true;

            // Séparer les tags par virgule
            var tags = tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim())
                                .Where(t => !string.IsNullOrEmpty(t))
                                .ToArray();

            // Vérifier le nombre maximum de tags
            if (tags.Length > MaxTags)
            {
                ErrorMessage = $"Maximum {MaxTags} tags autorisés";
                return false;
            }

            // Vérifier chaque tag
            foreach (var tag in tags)
            {
                if (tag.Length > MaxTagLength)
                {
                    ErrorMessage = $"Chaque tag ne peut dépasser {MaxTagLength} caractères";
                    return false;
                }

                // Vérifier les caractères autorisés pour les tags
                if (!System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[a-zA-ZÀ-ÿ0-9\s\-]+$"))
                {
                    ErrorMessage = "Les tags ne peuvent contenir que des lettres, chiffres, espaces et tirets";
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Attribut pour valider les descriptions
    /// </summary>
    public class DescriptionValidationAttribute : ValidationAttribute
    {
        public int MaxLength { get; set; } = 2000;

        public override bool IsValid(object? value)
        {
            if (value == null) return true;

            if (value is not string description)
            {
                ErrorMessage = "La description doit être une chaîne de caractères";
                return false;
            }

            description = description.Trim();

            if (description.Length > MaxLength)
            {
                ErrorMessage = $"La description ne peut dépasser {MaxLength} caractères";
                return false;
            }

            // Vérifier qu'il n'y a pas de contenu potentiellement dangereux
            string[] dangerousPatterns = { "<script", "javascript:", "vbscript:", "onload=", "onerror=" };
            foreach (var pattern in dangerousPatterns)
            {
                if (description.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "La description contient du contenu non autorisé";
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Attribut pour valider les emails avec des règles strictes
    /// </summary>
    public class StrictEmailValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null) return true;

            if (value is not string email)
            {
                ErrorMessage = "L'email doit être une chaîne de caractères";
                return false;
            }

            email = email.Trim().ToLowerInvariant();

            // Vérification de la longueur
            if (email.Length > 254)
            {
                ErrorMessage = "L'email ne peut dépasser 254 caractères";
                return false;
            }

            // Vérification du format avec regex stricte
            var emailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, emailRegex))
            {
                ErrorMessage = "Format d'email invalide";
                return false;
            }

            // Vérifier qu'il n'y a pas de points consécutifs
            if (email.Contains(".."))
            {
                ErrorMessage = "L'email ne peut contenir de points consécutifs";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Attribut pour valider les mots de passe avec des règles strictes
    /// </summary>
    public class StrictPasswordValidationAttribute : ValidationAttribute
    {
        public int MinLength { get; set; } = 8;
        public int MaxLength { get; set; } = 128;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialChar { get; set; } = true;

        public override bool IsValid(object? value)
        {
            if (value == null)
            {
                ErrorMessage = "Le mot de passe est obligatoire";
                return false;
            }

            if (value is not string password)
            {
                ErrorMessage = "Le mot de passe doit être une chaîne de caractères";
                return false;
            }

            // Vérification de la longueur
            if (password.Length < MinLength || password.Length > MaxLength)
            {
                ErrorMessage = $"Le mot de passe doit faire entre {MinLength} et {MaxLength} caractères";
                return false;
            }

            // Vérifications des exigences
            if (RequireUppercase && !password.Any(char.IsUpper))
            {
                ErrorMessage = "Le mot de passe doit contenir au moins une majuscule";
                return false;
            }

            if (RequireLowercase && !password.Any(char.IsLower))
            {
                ErrorMessage = "Le mot de passe doit contenir au moins une minuscule";
                return false;
            }

            if (RequireDigit && !password.Any(char.IsDigit))
            {
                ErrorMessage = "Le mot de passe doit contenir au moins un chiffre";
                return false;
            }

            if (RequireSpecialChar)
            {
                string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
                if (!password.Any(c => specialChars.Contains(c)))
                {
                    ErrorMessage = "Le mot de passe doit contenir au moins un caractère spécial";
                    return false;
                }
            }

            // Vérifier qu'il n'y a pas de caractères de contrôle
            if (password.Any(char.IsControl))
            {
                ErrorMessage = "Le mot de passe contient des caractères non autorisés";
                return false;
            }

            return true;
        }
    }
}
