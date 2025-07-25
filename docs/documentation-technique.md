# LibraryAPI - Documentation Technique

## 📚 Vue d'ensemble

**LibraryAPI** est une API REST développée en .NET 8 pour la gestion de livres et magazines numériques. Elle offre un système complet de bibliothèque avec authentification JWT, gestion des utilisateurs, favoris, historique de lecture, et système de notifications.

### 🎯 Objectifs
- Gestion centralisée de livres et magazines numériques
- Système d'authentification et d'autorisation robuste
- Interface publique pour les statistiques et contenus populaires
- Gestion des favoris et historique de lecture personnalisés
- Système de notifications et d'emails

## 🏗️ Architecture Technique

### Stack Technologique
- **Framework** : .NET 8
- **Base de données** : MariaDB/MySQL
- **ORM** : Entity Framework Core 8.0.8
- **Authentification** : ASP.NET Core Identity + JWT Bearer
- **Documentation API** : Swagger/OpenAPI
- **Déploiement** : Railway (Production)

### Structure du Projet
```
LibraryAPI/
├── Controllers/         # Contrôleurs API REST
├── Models/             # Entités et DTOs
├── Data/               # Contexte de base de données
├── Services/           # Services métier (Email)
├── Migrations/         # Migrations EF Core
├── wwwroot/           # Fichiers statiques
└── Configuration/     # Paramètres et configuration
```

## 🔐 Système d'Authentification

### Fonctionnalités
- **Registration/Login** : Inscription et connexion avec JWT
- **Rôles** : Admin et User avec permissions différenciées
- **Profils utilisateur** : Gestion complète des profils avec photos
- **Sécurité** : Tokens JWT avec expiration configurable

### Endpoints Principaux
- `POST /api/auth/register` - Inscription
- `POST /api/auth/login` - Connexion
- `PUT /api/auth/update-profile` - Mise à jour profil
- `GET /api/auth/users` - Liste utilisateurs (Admin)

## 📖 Gestion des Livres et Magazines

### Fonctionnalités Avancées
- **Upload de fichiers** : Livres/magazines avec couvertures
- **Métadonnées riches** : Titre, auteur, catégorie, description, tags
- **Système UUID** : Noms de fichiers uniques pour éviter les conflits
- **Statistiques** : Compteurs de vues et téléchargements
- **Recherche avancée** : Multi-critères avec pagination

### Endpoints Clés
- `POST /api/bookmagazine/add` - Ajouter contenu
- `GET /api/bookmagazine/list/paged` - Liste paginée
- `GET /api/bookmagazine/search/paged` - Recherche paginée
- `GET /api/bookmagazine/download/{id}` - Téléchargement
- `GET /api/bookmagazine/advanced-search/paged` - Recherche avancée

### Recherche et Filtrage
- **Recherche textuelle** : Titre, description, auteur, tags
- **Filtres** : Catégorie, auteur, date de publication
- **Tri** : Popularité, date, téléchargements
- **Pagination** : Performance optimisée pour grandes collections

## ⭐ Fonctionnalités Utilisateur

### Favoris
- Ajout/suppression de favoris personnels
- Protection contre les doublons
- Liste paginée des favoris

### Historique de Lecture
- Suivi automatique des consultations
- Horodatage des dernières lectures
- Suggestions basées sur l'historique

### Évaluations et Commentaires
- **Système de notes** : 1-5 étoiles avec moyenne
- **Commentaires hiérarchiques** : Commentaires et réponses
- **Restriction** : Seuls les lecteurs peuvent évaluer/commenter

## 🔔 Système de Notifications

### Types de Notifications
- Nouveaux contenus ajoutés
- Commentaires sur les publications
- Notifications administrateur

### Livraison
- **Interface web** : API pour récupérer les notifications
- **Email** : Service SMTP configurable
- **État de lecture** : Suivi des notifications lues/non lues

## 📊 API Publique et Statistiques

### Endpoints Publics
- `GET /api/public/top-books-magazines` - Contenus populaires
- `GET /api/public/stats` - Statistiques générales
- `GET /api/public/recent-comments` - Commentaires récents

### Rapports Administrateur
- Activité utilisateurs avec pagination
- Contenus les plus populaires
- Statistiques détaillées par contenu

## 🔧 Configuration et Déploiement

### Environnements Supportés
- **Development** : Configuration locale avec Swagger
- **Production** : Optimisé pour Railway
- **ProductionRailway** : Variables d'environnement spécifiques

### Variables de Configuration
```json
{
  "ConnectionStrings": {
    "MariaDBConnection": "..."
  },
  "Jwt": {
    "Key": "...",
    "Issuer": "LibraryApi",
    "Audience": "LibraryApiUsers"
  },
  "EmailSettings": {
    "SmtpServer": "...",
    "Port": 587,
    "SenderEmail": "..."
  }
}
```

### Makefile Avancé
Le projet inclut un Makefile sophistiqué avec :
- **Gestion des versions Git** : Tags automatiques et métadonnées
- **Builds intelligents** : Selon l'environnement (dev/CI/prod)
- **Outils de développement** : Installation automatique Ubuntu
- **Docker** : Images avec versioning Git
- **Railway** : Déploiement et logs

### Commandes Principales
```bash
make ubuntu-setup      # Installation complète environnement
make build             # Build avec version Git auto
make run-dev           # Lancement développement
make railway-deploy    # Déploiement Railway
make ssl-dev           # Certificats développement
```

## 🗄️ Modèle de Données

### Entités Principales
- **ApplicationUser** : Utilisateurs avec Identity
- **BookMagazine** : Contenus avec métadonnées
- **Author/Category** : Référentiels
- **UserFavorite** : Favoris utilisateur
- **UserReadingHistory** : Historique de lecture
- **Rating/Comment** : Évaluations et commentaires
- **Notification** : Système de notifications

### Relations
- RelationsMany-to-Many pour favoris et historique
- Relations One-to-Many pour auteurs et catégories
- Système de commentaires hiérarchiques
- Notifications liées aux utilisateurs

## 🚀 Performance et Optimisation

### Stratégies
- **Pagination** : Toutes les listes sont paginées
- **Index de recherche** : Optimisation des requêtes fréquentes
- **Fichiers UUID** : Éviter les conflits et améliorer la sécurité
- **Lazy Loading** : Chargement optimisé des relations

### Sécurité
- **JWT avec expiration** : Tokens sécurisés
- **Autorisation par rôles** : Admin/User différenciés
- **Validation des entrées** : Attributs de validation
- **Fichiers sécurisés** : UUID pour éviter l'énumération

## 📈 Métriques et Monitoring

### Statistiques Trackées
- Nombre total de contenus
- Vues et téléchargements
- Activité utilisateur
- Popularité des contenus

### Endpoints de Monitoring
- `GET /api/version` - Informations de version et build
- `GET /api/bookmagazine/{id}/stats` - Statistiques par contenu
- `GET /api/bookmagazine/reports/popular` - Rapports de popularité

## 🔄 Cycle de Développement

### Versioning Git Intelligent
- **Tags automatiques** : Versions basées sur Git
- **Métadonnées de build** : Commit, branche, état
- **Builds conditionnels** : Selon l'environnement
- **Validation CI/CD** : Checks automatiques

### Migrations et Base de Données
```bash
make migration-add NAME=NomMigration
make migration-update
make ef-check
```

## 🌐 Intégration et Extensions

### APIs Externes
- **Service Email** : SMTP configurable
- **Railway** : Déploiement cloud
- **Swagger** : Documentation interactive

### Extensibilité
- Architecture modulaire avec DI
- Services configurables
- Middleware personnalisable
- Support multi-environnements

## 📝 Documentation API

### Swagger/OpenAPI
- Documentation interactive complète
- Tests d'endpoints intégrés
- Authentification JWT dans l'interface
- Schémas et exemples détaillés

### Accès
- **Développement** : `/swagger`
- **Production** : Configurable via `EnableSwagger`

## 🎯 Conclusion

LibraryAPI est une solution complète et robuste pour la gestion de bibliothèques numériques, offrant :

- **Architecture moderne** : .NET 8, Entity Framework Core, JWT
- **Fonctionnalités riches** : Gestion complète du cycle de vie des contenus
- **Sécurité avancée** : Authentification, autorisation, validation
- **Performance optimisée** : Pagination, indexation, caching
- **Déploiement facilité** : Makefile avancé, Railway, Docker
- **Monitoring intégré** : Métriques, logs, rapports

Cette API est prête pour la production et peut facilement être étendue pour répondre à des besoins spécifiques supplémentaires.

