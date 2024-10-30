 
# Services

Le dossier `Services` contient des services supplémentaires qui fournissent des fonctionnalités auxiliaires à l'application.

- **EmailService.cs** : Gère l'envoi d'emails pour l'application, utilisé notamment pour les notifications par email. Configure les paramètres SMTP définis dans `appsettings.json` pour envoyer des emails de notification aux utilisateurs.

Les services sont injectés dans les contrôleurs et les autres parties de l'application selon les besoins pour garantir une architecture propre et modulaire.
