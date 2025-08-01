# Validation

Ce dossier contient le système de validation renforcé de l'application LibraryAPI avec des attributs de validation personnalisés qui garantissent la sécurité, l'intégrité et la qualité des données. Cette architecture multicouche protège contre les injections, les attaques XSS et assure la conformité des données métier.

## 🏗️ Architecture de Validation

### **Validation Multicouche**

1. **Validation côté client** : JavaScript (optionnel)
2. **Validation au niveau modèle** : Attributs de validation personnalisés
3. **Validation métier** : Logique spécifique dans les services
4. **Validation base de données** : Contraintes SQL

### **Principe de Sécurité First**

- ✅ **Validation stricte** de tous les inputs utilisateur
- ✅ **Sanitization automatique** des données dangereuses
- ✅ **Protection XSS/Injection** avec échappement de caractères
- ✅ **Validation de cohérence** entre format et contenu

## 🛡️ ValidationAttributes.cs

### **FileValidationAttribute**

**Rôle** : Validation complète et sécurisée des fichiers uploadés

```csharp
[AttributeUsage(AttributeTargets.Property)]
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

        // Validation de la taille
        if (file.Length > MaxSize)
        {
            ErrorMessage = $"Le fichier dépasse la taille maximale de {MaxSize / (1024 * 1024)}MB";
            return false;
        }

        // Validation de l'extension
        if (AllowedExtensions.Length > 0)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                ErrorMessage = $"Extensions autorisées : {string.Join(", ", AllowedExtensions)}";
                return false;
            }
        }

        // Validation du type MIME
        if (AllowedMimeTypes.Length > 0)
        {
            if (string.IsNullOrEmpty(file.ContentType) ||
                !AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                ErrorMessage = $"Types de fichiers autorisés : {string.Join(", ", AllowedMimeTypes)}";
                return false;
            }
        }

        // Validation du nom de fichier (sécurité)
        if (!IsSecureFileName(file.FileName))
        {
            ErrorMessage = "Le nom de fichier contient des caractères non autorisés";
            return false;
        }

        // Validation de la signature du fichier (Magic Numbers)
        if (!ValidateFileSignature(file))
        {
            ErrorMessage = "Le contenu du fichier ne correspond pas à son extension";
            return false;
        }

        return true;
    }

    private bool IsSecureFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Length > 255)
            return false;

        // Caractères interdits
        char[] dangerousChars = { '<', '>', ':', '"', '|', '?', '*', '\\', '/' };
        if (fileName.IndexOfAny(dangerousChars) >= 0)
            return false;

        // Noms de fichiers système interdits
        string[] systemNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "LPT1", "LPT2" };
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
        if (systemNames.Contains(nameWithoutExt))
            return false;

        return true;
    }

    private bool ValidateFileSignature(IFormFile file)
    {
        if (file.Length == 0) return false;

        using var stream = file.OpenReadStream();
        var buffer = new byte[16]; // Lire plus d'octets pour certains formats
        var bytesRead = stream.Read(buffer, 0, buffer.Length);

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

        return extension switch
        {
            ".pdf" => ValidatePDF(buffer),
            ".jpg" or ".jpeg" => ValidateJPEG(buffer),
            ".png" => ValidatePNG(buffer),
            ".gif" => ValidateGIF(buffer),
            ".epub" => ValidateEPUB(buffer),
            ".zip" => ValidateZIP(buffer),
            ".txt" => ValidateText(buffer, bytesRead),
            ".docx" => ValidateDOCX(buffer),
            _ => true // Autoriser temporairement les autres types
        };
    }

    private bool ValidatePDF(byte[] buffer)
    {
        // PDF commence par "%PDF"
        return buffer.Take(4).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 });
    }

    private bool ValidateJPEG(byte[] buffer)
    {
        // JPEG commence par FF D8 FF
        return buffer.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF });
    }

    private bool ValidatePNG(byte[] buffer)
    {
        // PNG : 89 50 4E 47 0D 0A 1A 0A
        byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        return buffer.Take(8).SequenceEqual(pngSignature);
    }

    private bool ValidateGIF(byte[] buffer)
    {
        // GIF87a ou GIF89a
        return buffer.Take(6).SequenceEqual(Encoding.ASCII.GetBytes("GIF87a")) ||
               buffer.Take(6).SequenceEqual(Encoding.ASCII.GetBytes("GIF89a"));
    }

    private bool ValidateEPUB(byte[] buffer)
    {
        // EPUB est un ZIP, commence par "PK"
        return buffer.Take(2).SequenceEqual(new byte[] { 0x50, 0x4B });
    }
}
```

### **Utilisation de FileValidation**

```csharp
public class BookMagazineModel
{
    [Required]
    [FileValidation(
        MaxSize = 100 * 1024 * 1024, // 100MB
        AllowedExtensions = new[] { ".pdf", ".epub", ".mobi", ".txt", ".doc", ".docx" },
        AllowedMimeTypes = new[] {
            "application/pdf",
            "application/epub+zip",
            "application/x-mobipocket-ebook",
            "text/plain",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        }
    )]
    public IFormFile? File { get; set; }

    [FileValidation(
        MaxSize = 10 * 1024 * 1024, // 10MB pour images
        AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
        AllowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" }
    )]
    public IFormFile? CoverImage { get; set; }
}
```

## 📝 SafeNameValidationAttribute

**Rôle** : Validation sécurisée des noms, titres et identifiants

```csharp
public class SafeNameValidationAttribute : ValidationAttribute
{
    public int MinLength { get; set; } = 2;
    public int MaxLength { get; set; } = 100;
    public bool AllowSpecialChars { get; set; } = true;

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

        // Vérification des caractères autorisés
        var allowedPattern = AllowSpecialChars
            ? @"^[a-zA-ZÀ-ÿ0-9\s\-\.\'\,\!\?\(\)]+$"  // Avec ponctuation
            : @"^[a-zA-ZÀ-ÿ0-9\s\-]+$";               // Sans ponctuation

        if (!System.Text.RegularExpressions.Regex.IsMatch(name, allowedPattern))
        {
            ErrorMessage = AllowSpecialChars
                ? "Seuls les lettres, chiffres, espaces et ponctuation de base sont autorisés"
                : "Seuls les lettres, chiffres, espaces et tirets sont autorisés";
            return false;
        }

        // Détection de patterns suspects
        if (ContainsSuspiciousPatterns(name))
        {
            ErrorMessage = "Le contenu contient des éléments non autorisés";
            return false;
        }

        // Pas d'espaces multiples consécutifs
        if (System.Text.RegularExpressions.Regex.IsMatch(name, @"\s{2,}"))
        {
            ErrorMessage = "Les espaces multiples consécutifs ne sont pas autorisés";
            return false;
        }

        return true;
    }

    private bool ContainsSuspiciousPatterns(string input)
    {
        // Patterns d'injection SQL/NoSQL
        string[] sqlPatterns = {
            "union", "select", "insert", "update", "delete", "drop", "exec", "script",
            "javascript:", "vbscript:", "onload", "onerror", "onclick"
        };

        var lowerInput = input.ToLowerInvariant();
        return sqlPatterns.Any(pattern => lowerInput.Contains(pattern));
    }
}
```

### **Utilisation de SafeNameValidation**

```csharp
public class Author
{
    [Required]
    [SafeNameValidation(MinLength = 2, MaxLength = 100, AllowSpecialChars = true)]
    public string Name { get; set; } = string.Empty;
}

public class Category
{
    [Required]
    [SafeNameValidation(MinLength = 2, MaxLength = 50, AllowSpecialChars = false)]
    public string Name { get; set; } = string.Empty;
}
```

## 🏷️ TagsValidationAttribute

**Rôle** : Validation du système de tags avec contrôle du nombre et de la qualité

```csharp
public class TagsValidationAttribute : ValidationAttribute
{
    public int MaxTags { get; set; } = 10;
    public int MaxTagLength { get; set; } = 30;
    public int MinTagLength { get; set; } = 2;

    public override bool IsValid(object? value)
    {
        if (value == null) return true;

        if (value is not string tagsString)
        {
            ErrorMessage = "Les tags doivent être une chaîne de caractères";
            return false;
        }

        if (string.IsNullOrWhiteSpace(tagsString)) return true;

        // Parsing des tags
        var tags = ParseTags(tagsString);

        // Vérification du nombre de tags
        if (tags.Length > MaxTags)
        {
            ErrorMessage = $"Maximum {MaxTags} tags autorisés";
            return false;
        }

        // Validation de chaque tag
        foreach (var tag in tags)
        {
            if (!ValidateIndividualTag(tag))
                return false;
        }

        // Vérification des doublons
        if (tags.Length != tags.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            ErrorMessage = "Les tags en doublon ne sont pas autorisés";
            return false;
        }

        return true;
    }

    private string[] ParseTags(string tagsString)
    {
        return tagsString
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();
    }

    private bool ValidateIndividualTag(string tag)
    {
        // Longueur du tag
        if (tag.Length < MinTagLength || tag.Length > MaxTagLength)
        {
            ErrorMessage = $"Chaque tag doit faire entre {MinTagLength} et {MaxTagLength} caractères";
            return false;
        }

        // Caractères autorisés (plus restrictif que SafeName)
        if (!System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[a-zA-ZÀ-ÿ0-9\-]+$"))
        {
            ErrorMessage = "Les tags ne peuvent contenir que lettres, chiffres et tirets";
            return false;
        }

        // Tags interdits
        string[] forbiddenTags = { "admin", "system", "root", "null", "undefined" };
        if (forbiddenTags.Contains(tag.ToLowerInvariant()))
        {
            ErrorMessage = $"Le tag '{tag}' n'est pas autorisé";
            return false;
        }

        return true;
    }
}
```

## 📝 DescriptionValidationAttribute

**Rôle** : Validation des textes longs avec protection XSS

todo : completer ce fichier
