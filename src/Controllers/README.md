 
# Controllers

Ce dossier contient les contrôleurs principaux de l'API REST. Chaque contrôleur est responsable de gérer les requêtes HTTP pour des fonctionnalités spécifiques. Voici un aperçu de chaque fichier :

- **AuthController.cs** : Gère l'authentification et la gestion des utilisateurs (inscription, connexion, etc.) en utilisant des jetons JWT pour la sécurité.
- **BookMagazineController.cs** : Gère les actions liées aux livres et magazines (ajout, lecture, téléchargement, notation, etc.).
- **FavoritesController.cs** : Permet aux utilisateurs de gérer leurs livres et magazines favoris.
- **NotificationController.cs** : Gère l'envoi de notifications, y compris les notifications par email et la gestion des notifications lues ou non.
- **PublicApiController.cs** : Contient les points de terminaison publics pour exposer certaines données et statistiques aux utilisateurs externes.
- **ReadingHistoryController.cs** : Gère l'historique de lecture de l'utilisateur, permettant de suivre les livres ou magazines déjà consultés.

Les contrôleurs utilisent `ApplicationDbContext` pour accéder à la base de données et réaliser les actions demandées.
