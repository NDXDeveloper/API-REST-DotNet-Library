
# Data

Ce dossier contient le fichier de configuration de la base de données de l'application.

- **ApplicationDbContext.cs** : Définit le contexte de la base de données de l'application en utilisant Entity Framework Core. `ApplicationDbContext` gère les interactions avec la base de données pour les entités de l'application, comme `User`, `BookMagazine`, `Category`, et autres. Il est également utilisé dans les contrôleurs pour effectuer des opérations CRUD (Create, Read, Update, Delete).

Ce contexte permet la configuration des relations entre les entités et la génération de migrations pour créer et maintenir le schéma de la base de données.

