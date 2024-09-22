# API REST .NET pour la gestion de livres et magazines

Ce projet est une API REST développée en .NET 8.0 qui permet aux utilisateurs de stocker, consulter et télécharger des livres et magazines. L'API inclut plusieurs fonctionnalités telles que la gestion des utilisateurs, l'authentification, la gestion de contenus, et des statistiques.

## Fonctionnalités principales

- **Gestion des utilisateurs** : Inscription, connexion, gestion de profils avec rôles et autorisations.
- **Gestion des livres et magazines** : Ajout, consultation, et téléchargement de contenus organisés par catégories et tags.
- **Bibliothèque personnelle** : Gestion des favoris et de l'historique de lecture.
- **Recherche et filtres avancés** : Recherche par mots-clés, filtres par catégories, popularité, etc.
- **Notes et commentaires** : Possibilité de noter et commenter des livres/magazines.
- **Notifications** : Notifications par email ou push pour les nouveaux contenus ou mises à jour.
- **Sécurité et performance** : Authentification JWT, protection contre les attaques courantes, et mise en cache.

## Installation

### Prérequis

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL](https://dev.mysql.com/downloads/)
- [Git](https://git-scm.com/)

### Étapes d'installation

1. Clonez le dépôt GitHub :
   ```bash
   git clone https://github.com/nide65/API-REST-DotNet-Library.git
   cd API-REST-DotNet-Library
   ```

2. Configurez la connexion à la base de données MySQL dans `appsettings.json` :
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=LibraryDB;User=VotreNomUtilisateur;Password=VotreMotDePasse;"
     }
   }
   ```

3. Appliquez les migrations Entity Framework Core pour créer les tables dans la base de données :
   ```bash
   dotnet ef database update
   ```

4. Démarrez l'application :
   ```bash
   dotnet run
   ```

### Tests

Pour lancer les tests unitaires, exécutez la commande suivante :
```bash
dotnet test
```

## Fonctionnalités supplémentaires

- **Swagger** : L'API est documentée avec Swagger. Après avoir démarré l'application, accédez à `http://localhost:5000/swagger` pour voir la documentation interactive.
- **Authentification JWT** : Les utilisateurs s'authentifient à l'aide de JSON Web Tokens (JWT) pour sécuriser les endpoints sensibles.

## Structure du projet

Voici un aperçu de l'architecture du projet :

```
API-REST-DotNet-Library/
│
├── docs/                     # Documentation du projet
│   ├── introduction.md        # Introduction au projet
│   ├── gestion_utilisateurs.md # Documentation sur la gestion des utilisateurs
│   ├── gestion_livres.md      # Documentation sur la gestion des livres et magazines
│   └── ...                    # Autres fichiers de documentation
│
├── src/                      # Code source du projet
│   ├── Controllers/           # Contient les contrôleurs de l'API
│   ├── Models/                # Modèles de données (User, Book, etc.)
│   ├── Repositories/          # Accès aux données
│   ├── Services/              # Logique métier
│   ├── Program.cs             # Point d'entrée de l'application
│   └── appsettings.json       # Fichier de configuration
│
├── tests/                    # Tests unitaires et d'intégration
│   ├── UserTests.cs           # Tests pour la gestion des utilisateurs
│   ├── BookTests.cs           # Tests pour la gestion des livres
│   └── SearchTests.cs         # Tests pour la recherche
│
├── README.md                 # Documentation du projet (ce fichier)
├── .gitignore                # Fichier gitignore
└── LICENSE                   # Licence du projet
```

## Contribuer

Les contributions sont les bienvenues ! Veuillez soumettre un pull request ou ouvrir une issue pour discuter de vos idées.

## Licence

Ce projet est sous licence MIT - voir le fichier [LICENSE](LICENSE) pour plus de détails.

