 
# Models

Le dossier `Models` contient les classes représentant les entités principales de l'application, correspondant aux tables de la base de données. Voici une description des principaux fichiers :

- **ApplicationUser.cs** : Représente un utilisateur avec des informations d'authentification étendues (hérite des fonctionnalités de `IdentityUser`).
- **Author.cs** : Modèle pour les auteurs de livres et magazines.
- **AuthModels.cs** : Contient des modèles auxiliaires pour gérer les requêtes d'authentification, comme `LoginModel` et `RegisterModel`.
- **BookMagazine.cs** : Représente les livres et magazines stockés dans l'application.
- **BookMagazineModel.cs** : Modèle utilisé pour gérer les données lors de l'ajout ou de la modification de livres et magazines.
- **Category.cs** : Représente une catégorie de livres ou de magazines.
- **Comment.cs** : Modèle pour les commentaires des utilisateurs sur les livres ou magazines.
- **CoverImageUuid.cs** : Contient les UUID des images de couverture, assurant l'unicité des noms de fichiers.
- **FileUuid.cs** : Contient les UUID pour chaque fichier de livre ou de magazine afin d'éviter les conflits de noms.
- **Notification.cs** : Représente une notification à destination des utilisateurs.
- **Rating.cs** : Modèle pour les évaluations et notes des livres et magazines.
- **UpdateProfileModel.cs** : Modèle pour les requêtes de mise à jour du profil utilisateur.
- **UserFavorite.cs** : Représente la liste des favoris pour chaque utilisateur.
- **UserNotification.cs** : Liaison entre les utilisateurs et les notifications envoyées.
- **UserReadingHistory.cs** : Historique de lecture de l'utilisateur.

Chaque modèle est mappé aux tables de la base de données via `ApplicationDbContext`.
