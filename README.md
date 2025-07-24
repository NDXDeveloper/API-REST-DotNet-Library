# API REST .NET pour la gestion de livres et magazines

**Auteur :** Nicolas DEOUX (NDXDev@gmail.com)

Ce projet est une API REST développée en .NET 8.0 qui permet aux utilisateurs de stocker, consulter et télécharger des livres et magazines. L'API inclut plusieurs fonctionnalités telles que la gestion des utilisateurs, l'authentification, la gestion de contenus, et des statistiques.

## 🚀 Fonctionnalités principales

- **Gestion des utilisateurs** : Inscription, connexion, gestion de profils avec rôles et autorisations
- **Gestion des livres et magazines** : Ajout, consultation, et téléchargement de contenus organisés par catégories et tags
- **Bibliothèque personnelle** : Gestion des favoris et de l'historique de lecture
- **Recherche et filtres avancés** : Recherche par mots-clés, filtres par catégories, popularité, etc.
- **Notes et commentaires** : Possibilité de noter et commenter des livres/magazines
- **Notifications** : Notifications par email pour les nouveaux contenus ou mises à jour
- **Sécurité et performance** : Authentification JWT, protection contre les attaques courantes

## 🛠️ Technologies utilisées

- **.NET 8.0** - Framework principal
- **ASP.NET Core** - API REST
- **Entity Framework Core** - ORM pour l'accès aux données
- **MariaDB/MySQL** - Base de données
- **JWT Bearer** - Authentification et autorisation
- **Swagger** - Documentation interactive de l'API
- **Identity** - Gestion des utilisateurs et des rôles

## 📋 Prérequis

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL/MariaDB](https://dev.mysql.com/downloads/) ou [Docker](https://www.docker.com/)
- [Git](https://git-scm.com/)

## 🔧 Installation

### Installation rapide avec Makefile (Ubuntu/Linux)

```bash
# Installation complète de l'environnement Ubuntu
make ubuntu-setup

# Configuration développement
make dev-setup

# Lancement en développement
make run-dev
```

### Installation manuelle

1. **Clonez le dépôt GitHub :**
   ```bash
   git clone https://github.com/NDXDeveloper/API-REST-DotNet-Library.git
   cd API-REST-DotNet-Library/src
   ```

2. **Configurez la connexion à la base de données dans `appsettings.json` :**
   ```json
   {
     "ConnectionStrings": {
       "MariaDBConnection": "server=localhost;port=3306;database=librarydb;user=votre_user;password=votre_password"
     },
     "Jwt": {
       "Key": "VotreCleSecreteTresLongueEtComplexe",
       "Issuer": "LibraryApi",
       "Audience": "LibraryApiUsers"
     }
   }
   ```

3. **Installez les dépendances :**
   ```bash
   dotnet restore
   ```

4. **Appliquez les migrations Entity Framework Core :**
   ```bash
   dotnet ef database update
   ```

5. **Démarrez l'application :**
   ```bash
   dotnet run
   ```

## 🐳 Déploiement avec Docker

```bash
# Construction de l'image Docker
make docker-build

# Lancement du conteneur
make docker-run
```

## 🚀 Déploiement Railway

```bash
# Connexion à Railway
make railway-login

# Déploiement
make railway-deploy
```

## 📖 Documentation

- **Swagger UI** : Après avoir démarré l'application, accédez à `http://localhost:5000/swagger` pour la documentation interactive
- **Endpoints principaux** :
  - `/api/auth` - Authentification et gestion des utilisateurs
  - `/api/bookmagazine` - Gestion des livres et magazines
  - `/api/favorites` - Gestion des favoris
  - `/api/readinghistory` - Historique de lecture
  - `/api/notification` - Notifications
  - `/api/public` - Endpoints publics

## 🔐 Utilisateur administrateur par défaut

L'application crée automatiquement un utilisateur administrateur au premier démarrage :

- **Email :** `admin@library.com`
- **Mot de passe :** `AdminPass123!`

## 📁 Structure du projet

```
API-REST-DotNet-Library/
├── src/
│   ├── Controllers/           # Contrôleurs API
│   │   ├── AuthController.cs
│   │   ├── BookMagazineController.cs
│   │   ├── FavoritesController.cs
│   │   └── ...
│   ├── Models/               # Modèles de données et DTOs
│   │   ├── ApplicationUser.cs
│   │   ├── BookMagazine.cs
│   │   ├── DTOs/
│   │   └── ...
│   ├── Data/                 # Contexte de base de données
│   │   └── ApplicationDbContext.cs
│   ├── Services/             # Services métier
│   │   └── EmailService.cs
│   ├── Migrations/           # Migrations Entity Framework
│   ├── wwwroot/             # Fichiers statiques
│   │   ├── files/           # Livres et magazines uploadés
│   │   └── images/          # Images de couverture et profils
│   ├── Program.cs           # Point d'entrée
│   ├── appsettings.json     # Configuration
│   └── Makefile            # Automatisation des tâches
├── README.md
└── .gitignore
```

## 🧪 Tests

Pour lancer les tests unitaires :
```bash
dotnet test
```

Ou avec le Makefile :
```bash
make test
```

## 📊 Fonctionnalités détaillées

### Authentification JWT
- Inscription et connexion sécurisées
- Gestion des rôles (Admin/User)
- Tokens avec expiration configurable

### Gestion des contenus
- Upload de fichiers (PDF, images)
- Génération automatique d'UUID pour éviter les conflits
- Système de catégories et tags
- Compteurs de vues et téléchargements

### Recherche avancée
- Recherche textuelle avec pagination
- Filtres par catégorie, auteur, date
- Tri par popularité ou date
- Suggestions basées sur l'historique

### Système social
- Notes et commentaires avec réponses
- Favoris personnalisés
- Historique de lecture
- Notifications par email

## 🔧 Commandes Makefile disponibles

```bash
# Développement
make install         # Installer les dépendances
make build          # Builder le projet
make run-dev        # Lancer en développement
make run-prod       # Lancer en production

# Base de données
make migration-add NAME=nom    # Ajouter une migration
make migration-update          # Appliquer les migrations

# Déploiement
make docker-build   # Builder l'image Docker
make railway-deploy # Déployer sur Railway

# Outils
make ssl-dev        # Générer certificats SSL
make clean          # Nettoyer les artifacts
```

## 🤝 Contribuer

Les contributions sont les bienvenues ! Veuillez :

1. Forker le projet
2. Créer une branche pour votre fonctionnalité (`git checkout -b feature/ma-fonctionnalite`)
3. Commiter vos changements (`git commit -m 'Ajout de ma fonctionnalité'`)
4. Pousser vers la branche (`git push origin feature/ma-fonctionnalite`)
5. Ouvrir une Pull Request

## 📄 Licence

Ce projet est sous licence MIT - voir le fichier [LICENSE](LICENSE) pour plus de détails.

## 📞 Contact

**Nicolas DEOUX**
Email : NDXDev@gmail.com
GitHub : [NDXDeveloper](https://github.com/NDXDeveloper)

---

⭐ N'hésitez pas à mettre une étoile au projet si vous le trouvez utile !
