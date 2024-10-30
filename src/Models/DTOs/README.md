
# README.md - Dossier DTOs

Ce dossier contient les Data Transfer Objects (DTOs) utilisés dans notre application pour faciliter le transfert de données entre les différentes couches de l'application, en particulier entre les contrôleurs et les services.

## Qu'est-ce qu'un DTO ?

Un DTO (Data Transfer Object) est un objet qui transporte des données entre les processus. Les DTOs sont utilisés pour encapsuler les données que vous souhaitez envoyer à un client ou recevoir d'un client, sans exposer les détails internes de votre modèle de données.

## Utilisation des DTOs

Dans ce projet, les DTOs sont principalement utilisés pour :

- **Simplifier les données renvoyées par les contrôleurs** : Les DTOs permettent de spécifier exactement quelles données sont renvoyées au client, sans inclure de données sensibles ou inutiles.
- **Faciliter la validation des données** : Les DTOs peuvent être annotés avec des attributs de validation pour garantir que les données reçues sont conformes aux attentes.
- **Réduire le couplage entre les couches** : En utilisant des DTOs, les modifications apportées aux modèles de données n'affectent pas nécessairement les contrats d'API, ce qui rend le code plus flexible.

## Liste des DTOs

### UserDto

- **Id** : Identifiant unique de l'utilisateur.
- **UserName** : Nom d'utilisateur.
- **Email** : Adresse e-mail de l'utilisateur.
- **CreatedAt** : Date de création de l'utilisateur (optionnelle).
- **Role** : Rôle de l'utilisateur (ex. : Admin, Utilisateur).

### BookMagazineModel

- **Title** : Titre du livre ou magazine.
- **Author** : Nom de l'auteur.
- **Category** : Catégorie du livre ou magazine.
- **Description** : Description du contenu.
- **Tags** : Mots-clés associés.
- **File** : Fichier du livre ou magazine (type `IFormFile`).
- **CoverImage** : Image de couverture (type `IFormFile`, optionnel).

## Conclusion

L'utilisation de DTOs est une pratique recommandée pour améliorer la structure de l'application et la gestion des données. En encapsulant les données nécessaires, nous assurons la sécurité, la validation et la maintenabilité de notre code.

