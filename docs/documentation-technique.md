# LibraryAPI - Documentation Technique

## 📚 Vue d'ensemble

**LibraryAPI** est une API REST développée en .NET 8 pour la gestion de livres et magazines numériques. Elle offre un système complet de bibliothèque avec authentification JWT, gestion des utilisateurs, favoris, historique de lecture, système de notifications, et **protection avancée contre les abus via rate limiting**.

### 🎯 Objectifs
- Gestion centralisée de livres et magazines numériques
- Système d'authentification et d'autorisation robuste
- **Protection contre les attaques DDoS et abus via rate limiting**
- Interface publique pour les statistiques et contenus populaires
- Gestion des favoris et historique de lecture personnalisés
- Système de notifications et d'emails
- Architecture sécurisée prête pour la production

## 🏗️ Architecture Technique

### Stack Technologique
- **Framework** : .NET 8
- **Base de données** : MariaDB/MySQL
- **ORM** : Entity Framework Core 8.0.8
- **Authentification** : ASP.NET Core Identity + JWT Bearer
- **Rate Limiting** : .NET 8 intégré avec politiques adaptatives
- **Documentation API** : Swagger/OpenAPI
- **Tests** : xUnit + Moq + Couverture de code
- **Déploiement** : Railway (Production)

### Structure du Projet
```
LibraryAPI/
├── src/
│   ├── Controllers/         # Contrôleurs API REST avec rate limiting
│   ├── Models/             # Entités et DTOs
│   │   ├── DTOs/           # Modèles de transfert
│   │   └── Validation/     # Validation personnalisée
│   ├── Data/               # Contexte de base de données
│   ├── Services/           # Services métier (Email)
│   ├── Middleware/         # Validation et logging
│   ├── Filters/            # Filtres de sécurité
│   ├── Migrations/         # Migrations EF Core
│   └── wwwroot/           # Fichiers statiques
├── tests/                  # Tests unitaires
└── docs/                   # Documentation
```

## 🛡️ Système de Rate Limiting

### Vue d'ensemble
LibraryAPI implémente un système de rate limiting natif .NET 8 pour protéger l'API contre :
- Les attaques par déni de service (DDoS)
- L'usage abusif des ressources
- Les tentatives de force brute
- La surcharge des endpoints sensibles

### Politiques de Limitation

#### 🌐 GlobalPolicy (Politique Générale)
- **Limite** : 200 requêtes par minute
- **Usage** : Endpoints généraux (livres, favoris, historique)
- **File d'attente** : 50 requêtes maximum
- **Contrôleurs** : `BookMagazineController`, `FavoritesController`, `ReadingHistoryController`

#### 🔒 StrictPolicy (Politique Stricte)
- **Limite** : 10 requêtes par minute
- **Usage** : Actions sensibles (authentification, notifications)
- **File d'attente** : 5 requêtes maximum
- **Contrôleurs** : `AuthController`, `NotificationController`

#### 📤 UploadPolicy (Politique Upload)
- **Limite** : 3 uploads par 15 minutes
- **Usage** : Upload de fichiers volumineux
- **File d'attente** : 2 requêtes maximum
- **Endpoints** : Upload de livres/magazines

#### 📖 PublicPolicy (Politique Publique)
- **Limite** : 1000 requêtes par minute
- **Usage** : API publique et consultation
- **File d'attente** : 100 requêtes maximum
- **Contrôleurs** : `PublicApiController`

### Gestion des Rejets
Quand les limites sont dépassées, l'API retourne :
- **Status HTTP** : `429 Too Many Requests`
- **Content-Type** : `application/json`
- **Réponse** :
```json
{
    "Message": "Trop de requêtes. Veuillez réessayer plus tard.",
    "RetryAfter": "60 seconds",
    "Timestamp": "2024-01-15T10:30:00Z"
}
```

## 🔐 Système d'Authentification

### Fonctionnalités
- **Registration/Login** : Inscription et connexion avec JWT
- **Rôles** : Admin et User avec permissions différenciées
- **Profils utilisateur** : Gestion complète des profils avec photos
- **Sécurité** : Tokens JWT avec expiration configurable
- **Protection** : Rate limiting strict sur les endpoints d'auth

### Endpoints Principaux
- `POST /api/auth/register` - Inscription *(StrictPolicy: 10/min)*
- `POST /api/auth/login` - Connexion *(StrictPolicy: 10/min)*
- `PUT /api/auth/update-profile` - Mise à jour profil *(StrictPolicy: 10/min)*
- `GET /api/auth/users` - Liste utilisateurs (Admin) *(StrictPolicy: 10/min)*
- `GET /api/auth/users/{id}` - Détails utilisateur (Admin) *(StrictPolicy: 10/min)*
- `GET /api/auth/users/role/{roleName}` - Utilisateurs par rôle (Admin) *(StrictPolicy: 10/min)*
- `GET /api/auth/users/search` - Recherche d'utilisateurs (Admin) *(StrictPolicy: 10/min)*

## 📖 Gestion des Livres et Magazines

### Fonctionnalités Avancées
- **Upload de fichiers** : Livres/magazines avec couvertures *(UploadPolicy: 3/15min)*
- **Métadonnées riches** : Titre, auteur, catégorie, description, tags
- **Système UUID** : Noms de fichiers uniques pour éviter les conflits
- **Statistiques** : Compteurs de vues et téléchargements
- **Recherche avancée** : Multi-critères avec pagination
- **Protection** : Rate limiting global pour éviter la surcharge

### Endpoints Clés
- `POST /api/bookmagazine/add` - Ajouter contenu *(UploadPolicy: 3/15min)*
- `GET /api/bookmagazine/list/paged` - Liste paginée *(GlobalPolicy: 200/min)*
- `GET /api/bookmagazine/search/paged` - Recherche paginée *(GlobalPolicy: 200/min)*
- `GET /api/bookmagazine/download/{id}` - Téléchargement *(GlobalPolicy: 200/min)*
- `GET /api/bookmagazine/download-cover/{id}` - Télécharger couverture *(GlobalPolicy: 200/min)*
- `GET /api/bookmagazine/advanced-search/paged` - Recherche avancée *(GlobalPolicy: 200/min)*

### Recherche et Filtrage
- **Recherche textuelle** : Titre, description, auteur, tags
- **Filtres** : Catégorie, auteur, date de publication
- **Tri** : Popularité, date, téléchargements
- **Pagination** : Performance optimisée pour grandes collections
- **Suggestions** : Basées sur l'historique de lecture

## ⭐ Fonctionnalités Utilisateur

### Favoris *(GlobalPolicy: 200/min)*
- Ajout/suppression de favoris personnels
- Protection contre les doublons
- Liste paginée des favoris

### Historique de Lecture *(GlobalPolicy: 200/min)*
- Suivi automatique des consultations
- Horodatage des dernières lectures
- Suggestions basées sur l'historique

### Évaluations et Commentaires *(GlobalPolicy: 200/min)*
- **Système de notes** : 1-5 étoiles avec moyenne
- **Commentaires hiérarchiques** : Commentaires et réponses
- **Restriction intelligente** : Seuls les lecteurs peuvent évaluer/commenter

## 🔔 Système de Notifications

### Types de Notifications *(StrictPolicy: 10/min)*
- Nouveaux contenus ajoutés
- Commentaires sur les publications
- Notifications administrateur

### Livraison
- **Interface web** : API pour récupérer les notifications
- **Email** : Service SMTP configurable avec templates HTML
- **État de lecture** : Suivi des notifications lues/non lues
- **Protection** : Rate limiting strict pour éviter le spam

## 📊 API Publique et Statistiques

### Endpoints Publics *(PublicPolicy: 1000/min)*
- `GET /api/public/top-books-magazines` - Contenus populaires
- `GET /api/public/stats` - Statistiques générales
- `GET /api/public/recent-comments` - Commentaires récents
- `GET /api/routes/list` - Liste de toutes les routes API

### Avantages du Rate Limiting Permissif
- Permet une consultation intensive des données publiques
- Idéal pour les intégrations externes
- Performance optimisée pour les dashboards

### Rapports Administrateur
- Activité utilisateurs avec pagination
- Contenus les plus populaires
- Statistiques détaillées par contenu

## 🛡️ Sécurité et Performance

### Validation Multicouche
- **Validation des fichiers** : Signatures, extensions, noms malveillants
- **Attributs personnalisés** : SafeNameValidation, FileValidation, etc.
- **Middleware** : Gestion exceptions et logging des validations
- **UUID sécurisé** : Protection contre l'énumération de fichiers

### Stratégies de Performance
- **Pagination** : Toutes les listes sont paginées
- **Index de recherche** : Optimisation des requêtes fréquentes
- **Fichiers UUID** : Éviter les conflits et améliorer la sécurité
- **Lazy Loading** : Chargement optimisé des relations
- **Rate Limiting Intelligent** : Files d'attente FIFO pour gérer les pics

### Pipeline de Sécurité
Le middleware suit cet ordre critique :
1. **HTTPS Redirection**
2. **Authentication/Authorization**
3. **Rate Limiting** ← Protection DDoS
4. **CORS Policy**
5. **Controllers Mapping**

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
  },
  "RateLimiting": {
    "GlobalPolicy": {
      "PermitLimit": 200,
      "WindowMinutes": 1,
      "QueueLimit": 50
    }
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
make test              # Tests unitaires
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
- Relations Many-to-Many pour favoris et historique
- Relations One-to-Many pour auteurs et catégories
- Système de commentaires hiérarchiques
- Notifications liées aux utilisateurs

## 🧪 Tests et Qualité

### Infrastructure de Tests
- **Framework** : xUnit avec Moq pour les mocks
- **Couverture** : Tests avec rapports de couverture
- **Base de données** : In-Memory pour les tests

### Commandes de Test
```bash
make test              # Tests standard
make test-debug        # Tests en mode Debug
make test-coverage     # Tests avec couverture de code
```

## 📈 Métriques et Monitoring

### Statistiques Trackées
- Nombre total de contenus
- Vues et téléchargements
- Activité utilisateur
- Popularité des contenus
- **Nouveau** : Métriques rate limiting (rejets, files d'attente)

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
- **Sécurité multicouche** : Rate limiting natif + authentification + autorisation
- **Protection DDoS avancée** : 4 politiques adaptatives avec files d'attente
- **Fonctionnalités riches** : Gestion complète du cycle de vie des contenus
- **Performance optimisée** : Pagination, indexation, limitation intelligente
- **Déploiement facilité** : Makefile avancé, Railway, Docker
- **Monitoring intégré** : Métriques, logs, rapports de sécurité

**Point fort** : Le système de rate limiting natif .NET 8 offre une protection robuste contre les abus tout en maintenant une expérience utilisateur fluide grâce aux files d'attente intelligentes et aux politiques différenciées par type d'usage.

Cette API est prête pour la production et peut gérer des charges importantes tout en maintenant la sécurité et les performances.
