 **partie 9 - Gestion des fichiers avec Amazon S3**

### Prérequis

1. **Compte AWS** : Créez un compte AWS si vous n'en avez pas.
2. **Bucket S3** : Connectez-vous à votre compte AWS, accédez à S3, puis créez un bucket pour stocker les fichiers.
3. **Clés d'accès** : Dans AWS IAM, créez un utilisateur ayant accès à S3 et téléchargez les clés d’accès et secret.

### Étapes

---

## 1. **Installer les packages AWS SDK**

Pour interagir avec S3, installez le package NuGet `AWSSDK.S3` dans votre projet.

```bash
dotnet add package AWSSDK.S3
```

---

## 2. **Configurer l’accès AWS dans l’application**

Ajoutez les informations de configuration S3 dans `appsettings.json` pour stocker les informations de connexion.

```json
"AWS": {
    "BucketName": "your-s3-bucket-name",
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key",
    "Region": "us-east-1"
}
```

---

## 3. **Configurer l’API pour AWS S3 dans `Program.cs`**

Modifiez `Program.cs` pour ajouter le service S3. Cela permettra l’injection de dépendance d’AmazonS3Client dans vos contrôleurs.

```csharp
using Amazon.S3;
using Amazon;

builder.Services.AddAWSService<IAmazonS3>();  // Ajout du service Amazon S3
```

---

## 4. **Créer le service S3 pour la gestion des fichiers**

Créez un nouveau service, `S3Service.cs`, qui contiendra la logique pour télécharger, récupérer et supprimer des fichiers dans S3.

### `S3Service.cs`

```csharp
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

public class S3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;
    private readonly string _bucketName;

    public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _configuration = configuration;
        _bucketName = _configuration["AWS:BucketName"];
    }

    // Méthode pour télécharger un fichier vers S3
    public async Task<string> UploadFileAsync(IFormFile file, string fileKey)
    {
        using var stream = file.OpenReadStream();

        var uploadRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            InputStream = stream,
            ContentType = file.ContentType,
            AutoCloseStream = true
        };

        await _s3Client.PutObjectAsync(uploadRequest);

        return $"https://{_bucketName}.s3.amazonaws.com/{fileKey}";
    }

    // Méthode pour supprimer un fichier depuis S3
    public async Task DeleteFileAsync(string fileKey)
    {
        var deleteObjectRequest = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey
        };

        await _s3Client.DeleteObjectAsync(deleteObjectRequest);
    }
}
```

---

## 5. **Modifier le contrôleur `BookMagazineController` pour utiliser S3**

Dans `BookMagazineController`, modifiez les actions de téléchargement et suppression pour utiliser le service S3.

### `BookMagazineController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

[ApiController]
[Route("api/[controller]")]
public class BookMagazineController : ControllerBase
{
    private readonly S3Service _s3Service;

    public BookMagazineController(S3Service s3Service)
    {
        _s3Service = s3Service;
    }

    // Endpoint pour ajouter un nouveau livre ou magazine avec fichier dans S3
    [HttpPost("upload")]
    public async Task<IActionResult> UploadBookMagazine([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is missing.");

        var fileKey = $"{Guid.NewGuid()}_{file.FileName}"; // Nom unique pour le fichier dans S3
        var fileUrl = await _s3Service.UploadFileAsync(file, fileKey);

        // Enregistrer l’URL dans la base de données si nécessaire
        // Ex : bookMagazine.FileUrl = fileUrl;

        return Ok(new { FileUrl = fileUrl });
    }

    // Endpoint pour supprimer un livre ou magazine
    [HttpDelete("delete/{fileKey}")]
    public async Task<IActionResult> DeleteBookMagazine(string fileKey)
    {
        await _s3Service.DeleteFileAsync(fileKey);
        return Ok("File deleted successfully.");
    }
}
```

---

## 6. **Configurer la compression des fichiers avant le téléchargement**

Pour compresser les fichiers avant de les télécharger dans S3, installez un package de compression, comme `System.IO.Compression`.

```bash
dotnet add package System.IO.Compression
```

Ensuite, modifiez la méthode `UploadFileAsync` dans `S3Service` pour compresser les fichiers avant de les envoyer à S3.

```csharp
using System.IO.Compression;

// Compression avant téléchargement
public async Task<string> UploadFileAsync(IFormFile file, string fileKey)
{
    using var compressedStream = new MemoryStream();
    using (var zipArchive = new ZipArchive(compressedStream, ZipArchiveMode.Create, true))
    {
        var zipEntry = zipArchive.CreateEntry(file.FileName, CompressionLevel.Optimal);
        using var originalFileStream = file.OpenReadStream();
        using var entryStream = zipEntry.Open();
        await originalFileStream.CopyToAsync(entryStream);
    }

    compressedStream.Seek(0, SeekOrigin.Begin);  // Revenir au début du flux pour le téléchargement

    var uploadRequest = new PutObjectRequest
    {
        BucketName = _bucketName,
        Key = fileKey,
        InputStream = compressedStream,
        ContentType = "application/zip"
    };

    await _s3Client.PutObjectAsync(uploadRequest);
    return $"https://{_bucketName}.s3.amazonaws.com/{fileKey}";
}
```

---

## 7. **Ajouter une migration pour stocker les informations de fichier dans la base de données**

Ajoutez un champ `FileUrl` à votre modèle `BookMagazine` pour stocker l’URL du fichier S3.

### Modèle `BookMagazine.cs`

```csharp
public class BookMagazine
{
    public int Id { get; set; }
    public string Title { get; set; }
    // Autres propriétés...

    public string FileUrl { get; set; } // URL du fichier dans S3
}
```

### Créer la migration

```bash
dotnet ef migrations add AddFileUrlToBookMagazine
dotnet ef database update
```

---

## 8. **Configurer le CORS pour autoriser l’accès public (si nécessaire)**

Si les fichiers sont accessibles publiquement, vous devrez configurer le CORS pour autoriser les requêtes vers l’API depuis d’autres origines.

### `Program.cs`

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicApiPolicy", builder =>
    {
        builder.WithOrigins("https://trustedwebsite.com")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

app.UseCors("PublicApiPolicy");
```

---

## 9. **Tester et vérifier**

1. **Téléchargement de fichier** : Accédez à `POST /api/BookMagazine/upload` et téléchargez un fichier. Le service renverra l’URL du fichier dans S3.
2. **Suppression de fichier** : Accédez à `DELETE /api/BookMagazine/delete/{fileKey}` en passant le nom du fichier. Cette action supprimera le fichier du stockage S3.
3. **Vérifiez S3** pour voir si le fichier est bien stocké ou supprimé.

---





**partie 9 - Gestion des fichiers avec Azure Blob Storage**

### Prérequis

1. **Compte Azure** : Créez un compte Azure si vous n'en avez pas encore.
2. **Création d’un Container de stockage** : Connectez-vous au portail Azure, créez un compte de stockage (Standard, BlobStorage) et créez un conteneur (par exemple : "bookmagazines") pour stocker les fichiers.

### Étapes

---

## 1. **Installer le package Azure Storage SDK**

Pour interagir avec Azure Blob Storage, installez le package `Azure.Storage.Blobs`.

```bash
dotnet add package Azure.Storage.Blobs
```

---

## 2. **Configurer l’accès Azure dans `appsettings.json`**

Ajoutez les informations de configuration Azure Blob dans votre fichier `appsettings.json` :

```json
"AzureBlobStorage": {
    "ConnectionString": "your-azure-blob-connection-string",
    "ContainerName": "bookmagazines"
}
```

---

## 3. **Configurer l’API pour Azure Blob Storage dans `Program.cs`**

Modifiez `Program.cs` pour ajouter les services Azure Blob afin qu'ils soient injectés dans vos contrôleurs.

### `Program.cs`

```csharp
using Azure.Storage.Blobs;

builder.Services.AddSingleton(x => new BlobServiceClient(builder.Configuration["AzureBlobStorage:ConnectionString"]));
builder.Services.AddScoped<BlobStorageService>(); // Ajout du service de gestion des blobs
```

---

## 4. **Créer le service de gestion de Blob Storage**

Créez un service, `BlobStorageService.cs`, qui contiendra la logique de téléchargement, récupération, et suppression des fichiers sur Azure Blob Storage.

### `BlobStorageService.cs`

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public BlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["AzureBlobStorage:ContainerName"];
    }

    // Méthode pour télécharger un fichier dans Azure Blob Storage
    public async Task<string> UploadFileAsync(IFormFile file, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();
        await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob); // Pour un accès public aux fichiers si nécessaire

        var blobClient = containerClient.GetBlobClient(blobName);
        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blobClient.Uri.ToString(); // Retourne l'URL du fichier
    }

    // Méthode pour supprimer un fichier depuis Azure Blob Storage
    public async Task DeleteFileAsync(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }
}
```

---

## 5. **Modifier le contrôleur `BookMagazineController` pour utiliser Azure Blob Storage**

Dans `BookMagazineController`, modifiez les actions pour télécharger et supprimer des fichiers en utilisant `BlobStorageService`.

### `BookMagazineController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

[ApiController]
[Route("api/[controller]")]
public class BookMagazineController : ControllerBase
{
    private readonly BlobStorageService _blobStorageService;

    public BookMagazineController(BlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    // Endpoint pour ajouter un nouveau livre ou magazine avec un fichier dans Azure Blob Storage
    [HttpPost("upload")]
    public async Task<IActionResult> UploadBookMagazine([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is missing.");

        var blobName = $"{Guid.NewGuid()}_{file.FileName}"; // Nom unique pour le fichier dans Azure Blob
        var fileUrl = await _blobStorageService.UploadFileAsync(file, blobName);

        // Enregistrer l’URL dans la base de données si nécessaire
        // Ex : bookMagazine.FileUrl = fileUrl;

        return Ok(new { FileUrl = fileUrl });
    }

    // Endpoint pour supprimer un livre ou magazine
    [HttpDelete("delete/{blobName}")]
    public async Task<IActionResult> DeleteBookMagazine(string blobName)
    {
        await _blobStorageService.DeleteFileAsync(blobName);
        return Ok("File deleted successfully.");
    }
}
```

---

## 6. **Ajouter une migration pour stocker l’URL du fichier dans la base de données**

Ajoutez un champ `FileUrl` à votre modèle `BookMagazine` pour stocker l’URL du fichier sur Azure Blob Storage.

### Modèle `BookMagazine.cs`

```csharp
public class BookMagazine
{
    public int Id { get; set; }
    public string Title { get; set; }
    // Autres propriétés...

    public string FileUrl { get; set; } // URL du fichier dans Azure Blob Storage
}
```

### Créer et appliquer la migration

```bash
dotnet ef migrations add AddFileUrlToBookMagazine
dotnet ef database update
```

---

## 7. **Configurer la compression des fichiers avant le téléchargement**

Pour compresser les fichiers avant de les télécharger, installez un package de compression, comme `System.IO.Compression`.

```bash
dotnet add package System.IO.Compression
```

Ensuite, modifiez la méthode `UploadFileAsync` dans `BlobStorageService` pour compresser les fichiers avant de les envoyer sur Azure Blob Storage.

```csharp
using System.IO.Compression;

// Compression avant téléchargement
public async Task<string> UploadFileAsync(IFormFile file, string blobName)
{
    using var compressedStream = new MemoryStream();
    using (var zipArchive = new ZipArchive(compressedStream, ZipArchiveMode.Create, true))
    {
        var zipEntry = zipArchive.CreateEntry(file.FileName, CompressionLevel.Optimal);
        using var originalFileStream = file.OpenReadStream();
        using var entryStream = zipEntry.Open();
        await originalFileStream.CopyToAsync(entryStream);
    }

    compressedStream.Seek(0, SeekOrigin.Begin);  // Revenir au début du flux pour le téléchargement

    var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
    var blobClient = containerClient.GetBlobClient(blobName);
    await blobClient.UploadAsync(compressedStream, new BlobHttpHeaders { ContentType = "application/zip" });

    return blobClient.Uri.ToString();
}
```

---

## 8. **Configurer le CORS pour autoriser l’accès public (si nécessaire)**

Si vous souhaitez permettre l’accès aux fichiers depuis des applications externes, configurez le CORS dans le portail Azure pour votre compte de stockage ou configurez le CORS dans votre application.

### `Program.cs`

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicApiPolicy", builder =>
    {
        builder.WithOrigins("https://trustedwebsite.com")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

app.UseCors("PublicApiPolicy");
```

---

## 9. **Tester et vérifier**

1. **Téléchargement de fichier** : Accédez à `POST /api/BookMagazine/upload` pour tester le téléchargement d’un fichier. L'API doit renvoyer l’URL du fichier sur Azure Blob Storage.
2. **Suppression de fichier** : Accédez à `DELETE /api/BookMagazine/delete/{blobName}` avec le nom du fichier dans Azure pour le supprimer.
3. **Vérifiez dans Azure Blob Storage** pour confirmer que le fichier est bien stocké ou supprimé.

