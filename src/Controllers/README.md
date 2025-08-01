# Controllers

Ce dossier contient les contrôleurs principaux de l'API REST LibraryAPI. Chaque contrôleur est responsable de gérer les requêtes HTTP pour des fonctionnalités spécifiques avec une architecture robuste incluant logging dual, validation renforcée et gestion d'erreurs granulaire.

## 🏗️ Architecture des Contrôleurs

### Système de Logging Dual

- **Logs Techniques (Serilog)** : Surveillance système, erreurs infrastructure, performance
- **Logs d'Audit (AuditLogger)** : Traçabilité métier, conformité RGPD, actions utilisateur

### Sécurité et Protection

- **Rate Limiting** adaptatif selon les endpoints (GlobalPolicy, StrictPolicy, UploadPolicy, PublicPolicy)
- **Validation renforcée** avec filtres personnalisés
- **Gestion d'exceptions** granulaire par type
- **Authentification JWT** avec rôles (Admin, User)

## 📋 Liste des Contrôleurs

### **AuthController.cs**

**Route** : `/api/Auth`  
**Rôle** : Gestion complète de l'authentification et des utilisateurs

**Fonctionnalités :**

- ✅ Inscription avec validation renforcée et emails de bienvenue HTML
- ✅ Connexion/déconnexion avec génération JWT sécurisée
- ✅ Mise à jour de profil avec upload d'images
- ✅ Gestion administrative des utilisateurs (liste, recherche, filtrage par rôles)
- ✅ Récupération des notifications utilisateur

**Endpoints principaux :**

- `POST /register` - Inscription avec email de bienvenue automatique
- `POST /login` - Connexion avec génération JWT
- `POST /logout` - Déconnexion sécurisée
- `PUT /update-profile` - Mise à jour profil + upload image
- `GET /users` - Liste utilisateurs (Admin)
- `GET /users/search` - Recherche utilisateurs (Admin)

**Logging spécialisé :**

- Erreurs de configuration JWT/Identity
- Problèmes filesystem (upload images)
- Validations Identity échouées
- Incohérences système critiques

---

### **BookMagazineController.cs**

**Route** : `/api/BookMagazine`  
**Rôle** : Gestion complète de la bibliothèque numérique

**Fonctionnalités :**

- ✅ Upload sécurisé avec validation de signatures de fichiers
- ✅ CRUD complet avec gestion des relations (auteurs, catégories)
- ✅ Recherche avancée avec pagination optimisée
- ✅ Téléchargements avec compteurs de statistiques
- ✅ Système de notation et commentaires
- ✅ Notifications automatiques aux admins lors d'uploads
- ✅ Génération de rapports d'activité détaillés

**Endpoints principaux :**

- `POST /add` - Upload livre/magazine avec couverture
- `GET /list` - Liste complète avec métadonnées
- `GET /download/{id}` - Téléchargement sécurisé
- `GET /{id}` - Détails avec statistiques
- `GET /search/paged` - Recherche paginée avancée
- `POST /{id}/rate` - Notation (1-5 étoiles)
- `POST /{id}/comment` - Ajout de commentaires
- `DELETE /delete/{id}` - Suppression (Admin)

**Logging spécialisé :**

- Surveillance uploads volumineux (>100MB)
- Erreurs filesystem et permissions
- Calculs d'agrégation problématiques
- Détection d'incohérences de données

---

### **FavoritesController.cs**

**Route** : `/api/Favorites`  
**Rôle** : Gestion des favoris utilisateur

**Fonctionnalités :**

- ✅ Ajout/suppression de favoris avec validation
- ✅ Liste des favoris avec détails complets
- ✅ Gestion des doublons et conflits
- ✅ Vérification d'intégrité des relations

**Endpoints principaux :**

- `POST /add-favorite/{id}` - Ajouter aux favoris
- `GET /my-favorites` - Liste des favoris personnelle
- `DELETE /remove-favorite/{id}` - Retirer des favoris

**Logging spécialisé :**

- Détection de favoris avec références nulles
- Erreurs de concurrence lors des suppressions
- Problèmes de navigation EF

---

### **ReadingHistoryController.cs**

**Route** : `/api/ReadingHistory`  
**Rôle** : Suivi de l'historique de lecture

**Fonctionnalités :**

- ✅ Mise à jour automatique de l'historique
- ✅ Récupération de l'historique avec métadonnées
- ✅ Gestion des lectures multiples d'un même livre
- ✅ Détection d'historiques volumineux (>1000 entrées)

**Endpoints principaux :**

- `POST /update-history/{id}` - Marquer comme lu
- `GET /reading-history` - Historique personnel complet

**Logging spécialisé :**

- Surveillance des historiques volumineux
- Problèmes de jointures complexes
- Incohérences de relations BookMagazine

---

### **NotificationController.cs**

**Route** : `/api/Notification`  
**Rôle** : Système de notifications avancé

**Fonctionnalités :**

- ✅ Création de notifications (Admin)
- ✅ Envoi d'emails avec gestion d'erreurs non-bloquantes
- ✅ Marquage comme lu/non lu
- ✅ Gestion des notifications en attente

**Endpoints principaux :**

- `POST /create` - Créer notification (Admin)
- `POST /send-emails` - Envoi emails en lot (Admin)
- `POST /mark-as-read/{id}` - Marquer comme lue

**Logging spécialisé :**

- Erreurs SMTP et timeouts
- Problèmes de configuration EmailService
- Statistiques d'envoi détaillées
- Intégrité des données utilisateur/notification

---

### **PublicApiController.cs**

**Route** : `/api/public`  
**Rôle** : API publique sans authentification

**Fonctionnalités :**

- ✅ Livres/magazines les plus populaires
- ✅ Statistiques générales de l'API
- ✅ Commentaires récents publics
- ✅ Rate limiting permissif pour consultation

**Endpoints principaux :**

- `GET /top-books-magazines` - Contenu populaire
- `GET /stats` - Statistiques globales
- `GET /recent-comments` - Commentaires récents

**Logging spécialisé :**

- Détection de datasets volumineux
- Erreurs de calculs d'agrégation
- Surveillance de l'usage API publique

---

### **HealthController.cs** + **RootHealthController.cs**

**Route** : `/api/health` et `/health`  
**Rôle** : Monitoring et health checks

**Fonctionnalités :**

- ✅ Health check standard avec métriques système
- ✅ Health check détaillé (Admin) avec infos Git
- ✅ Health check simple pour Docker/Railway
- ✅ Surveillance base de données et performance

**Endpoints principaux :**

- `GET /api/health` - Health check complet
- `GET /api/health/detailed` - Diagnostics avancés (Admin)
- `GET /api/health/simple` - Check minimal
- `GET /health` - Endpoint legacy

**Métriques surveillées :**

- Connectivité base de données
- Utilisation mémoire et uptime
- Statistiques applicatives
- Informations de version Git

---

### **Contrôleurs Utilitaires**

#### **VersionController.cs**

- `GET /api/version` - Informations de version complètes avec métadonnées Git

#### **RoutesController.cs**

- `GET /api/routes/list` - Liste de toutes les routes avec autorisations

#### **Admin/AuditController.cs**

- `GET /api/admin/audit/logs` - Consultation des logs d'audit (Admin)
- `GET /api/admin/audit/stats` - Statistiques d'audit (Admin)

## 🛠️ Fonctionnalités Transversales

### Rate Limiting Adaptatif

- **PublicPolicy** : 1000 req/min (API publique)
- **GlobalPolicy** : 200 req/min (endpoints standards)
- **StrictPolicy** : 10 req/min (auth, actions sensibles)
- **UploadPolicy** : 3 req/15min (uploads de fichiers)

### Validation Renforcée

- **ModelValidationFilter** : Validation automatique des modèles
- **FileValidationFilter** : Sécurité des fichiers (signatures, noms)
- **Attributs personnalisés** : SafeNameValidation, FileValidation, etc.

### Middleware Global

- **GlobalExceptionMiddleware** : Gestion centralisée des erreurs
- **ValidationExceptionMiddleware** : Traitement des erreurs de validation
- **ValidationLoggingMiddleware** : Logging des échecs de validation

### Système d'Audit Complet

- **Actions trackées** : LOGIN_SUCCESS, BOOK_CREATED, FAVORITE_ADDED, etc.
- **Métadonnées** : UserId, IP, Timestamp, détails action
- **Conformité RGPD** : Traçabilité complète des actions utilisateur

## 📊 Monitoring et Observabilité

### Logs Techniques (Serilog)

- **Fichiers rotatifs** : logs/app-{date}.log
- **Logs d'erreurs** : logs/errors-{date}.log
- **Emails critiques** : Alertes automatiques admin
- **Console colorée** : Développement local

### Métriques Surveillées

- Taux d'erreur par endpoint
- Temps de réponse et performance
- Utilisation des ressources
- Intégrité des données
- Patterns d'utilisation

## 🚀 Évolutions Recommandées

### Nouvelles Fonctionnalités

- **Cache Redis** pour les recherches fréquentes
- **Compression** automatique des gros fichiers
- **Notifications temps réel** avec SignalR
- **API versioning** pour évolutions futures

### Optimisations

- **Pagination curseur** pour très gros datasets
- **CDN** pour les images de couverture
- **Search index** Elasticsearch pour recherche avancée
- **Background jobs** pour tâches lourdes

L'architecture actuelle est robuste et prête pour une montée en charge, avec une séparation claire des responsabilités et une observabilité complète pour la maintenance et l'évolution.
