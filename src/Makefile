# Makefile pour LibraryAPI .NET 8
# Usage: make [command]

# Variables de base
PROJECT_NAME = src
PROJECT_FILE = $(PROJECT_NAME).csproj
DOCKER_IMAGE = library-api
DOCKER_TAG = latest

# === Variables de version Git avec fallbacks sécurisés ===
VERSION ?= $(shell git describe --tags --always --dirty 2>/dev/null || echo "dev")
BUILD_TIME = $(shell date -u +%Y-%m-%dT%H:%M:%SZ)
GIT_COMMIT = $(shell git rev-parse HEAD 2>/dev/null || echo "unknown")
GIT_TAG = $(shell git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0-dev")
GIT_BRANCH = $(shell git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")
GIT_DIRTY = $(shell git diff --quiet 2>/dev/null || echo "-dirty")

# Version nettoyée pour .NET (supprime le 'v' prefix)
DOTNET_VERSION = $(shell echo $(VERSION) | sed 's/^v//' | sed 's/-dirty//' | sed 's/-dev//' | sed 's/-.*/.0/')
# Version informative complète
VERSION_FULL = $(VERSION)+$(shell date +%Y%m%d%H%M).$(shell echo $(GIT_COMMIT) | cut -c1-7)

# Détection de l'environnement
IS_GIT_REPO = $(shell git rev-parse --git-dir >/dev/null 2>&1 && echo "true" || echo "false")
IS_CI = $(shell [ -n "$$CI" ] && echo "true" || echo "false")

# Couleurs pour l'affichage
GREEN = \033[0;32m
YELLOW = \033[1;33m
RED = \033[0;31m
NC = \033[0m # No Color

.PHONY: help install build test clean run-dev run-prod run-railway docker ssl-dev ssl-clean migration-add migration-update migration-list migration-remove migration-script railway-login railway-deploy dev-setup ef-install ef-check ubuntu-setup dotnet-install vscode-install tools-install system-update nuget-restore nuget-update nuget-clean nuget-outdated version-info build-dev build-ci build-release auto-build check-env docker-build-versioned create-dev-tag clean-dev-tags version-env

# Affichage de l'aide par défaut
help:
	@echo "$(GREEN)📚 LibraryAPI - Commandes disponibles$(NC)"
	@echo ""
	@echo "$(YELLOW)🐧 Installation Ubuntu :$(NC)"
	@echo "  make ubuntu-setup    - Installation complète environnement Ubuntu"
	@echo "  make dotnet-install  - Installer .NET 8 SDK"
	@echo "  make vscode-install  - Installer Visual Studio Code + extensions"
	@echo "  make tools-install   - Installer tous les outils de développement"
	@echo "  make system-update   - Mettre à jour le système Ubuntu"
	@echo ""
	@echo "$(YELLOW)🛠️  Développement :$(NC)"
	@echo "  make install         - Installer les dépendances"
	@echo "  make build           - Builder le projet avec version Git auto"
	@echo "  make test            - Lancer les tests"
	@echo "  make clean           - Nettoyer les artifacts"
	@echo "  make run-dev         - Lancer en mode développement"
	@echo "  make run-prod        - Lancer en mode production"
	@echo "  make run-railway     - Lancer en mode Railway"
	@echo "  make dev-setup       - Configuration complète développement"
	@echo ""
	@echo "$(YELLOW)📦 Gestion des versions intelligente :$(NC)"
	@echo "  make version-info    - Afficher toutes les infos de version"
	@echo "  make build-dev       - Build forcé en mode développement"
	@echo "  make build-ci        - Build pour CI/CD (vérifie la version)"
	@echo "  make build-release   - Build release (tag requis, repo propre)"
	@echo "  make auto-build      - Build intelligent selon l'environnement"
	@echo "  make check-env       - Vérifier l'environnement Git"
	@echo "  make version-env     - Versions pour chaque environnement"
	@echo "  make create-dev-tag TAG=v1.0.0-alpha - Créer tag développement"
	@echo ""
	@echo "$(YELLOW)🔧 Entity Framework Core :$(NC)"
	@echo "  make ef-install      - Installer l'outil Entity Framework Core globalement"
	@echo "  make ef-check        - Vérifier la version d'Entity Framework Core"
	@echo ""
	@echo "$(YELLOW)📦 Gestion des packages :$(NC)"
	@echo "  make nuget-restore   - Restaurer les packages NuGet"
	@echo "  make nuget-update    - Mettre à jour les packages NuGet"
	@echo "  make nuget-clean     - Nettoyer le cache NuGet"
	@echo "  make nuget-outdated  - Lister les packages obsolètes"
	@echo ""
	@echo "$(YELLOW)🔒 Certificats SSL :$(NC)"
	@echo "  make ssl-dev         - Générer certificats développement"
	@echo "  make ssl-clean       - Nettoyer certificats"
	@echo "  make ssl-trust       - Approuver certificats système"
	@echo ""
	@echo "$(YELLOW)🗄️  Migrations Entity Framework :$(NC)"
	@echo "  make migration-add NAME=nom    - Ajouter migration"
	@echo "  make migration-update          - Appliquer migrations"
	@echo "  make migration-list            - Lister migrations"
	@echo "  make migration-remove          - Supprimer dernière migration"
	@echo "  make migration-script          - Générer script SQL"
	@echo ""
	@echo "$(YELLOW)🚀 Railway :$(NC)"
	@echo "  make railway-login   - Se connecter à Railway"
	@echo "  make railway-deploy  - Déployer sur Railway"
	@echo "  make railway-logs    - Voir les logs Railway"
	@echo "  make railway-vars    - Voir variables Railway"
	@echo ""
	@echo "$(YELLOW)🐳 Docker :$(NC)"
	@echo "  make docker-build    - Builder image Docker"
	@echo "  make docker-build-versioned - Docker avec version Git"
	@echo "  make docker-run      - Lancer conteneur"
	@echo "  make docker-clean    - Nettoyer images Docker"

# === Gestion des versions ===

# Affichage des informations de version
version-info:
	@echo "$(GREEN)📦 Informations de version:$(NC)"
	@echo "  Version: $(VERSION)"
	@echo "  Version .NET: $(DOTNET_VERSION)"
	@echo "  Version complète: $(VERSION_FULL)"
	@echo "  Tag Git: $(GIT_TAG)"
	@echo "  Commit: $(GIT_COMMIT)"
	@echo "  Branche: $(GIT_BRANCH)"
	@echo "  État: $(if $(findstring dirty,$(VERSION)),sale,propre)"
	@echo "  Build: $(BUILD_TIME)"
	@echo "  Repo Git: $(IS_GIT_REPO)"
	@echo "  CI/CD: $(IS_CI)"

# Vérification de l'environnement
check-env:
	@echo "$(GREEN)🔍 Vérification de l'environnement...$(NC)"
	@if [ "$(IS_GIT_REPO)" = "false" ]; then \
		echo "$(YELLOW)⚠️  Pas de repository Git - version par défaut 'dev'$(NC)"; \
	elif [ "$(VERSION)" = "dev" ]; then \
		echo "$(YELLOW)⚠️  Aucun tag Git trouvé - version 'dev'$(NC)"; \
		echo "$(YELLOW)Pour créer un tag: git tag v1.0.0$(NC)"; \
	elif echo "$(VERSION)" | grep -q dirty; then \
		echo "$(YELLOW)⚠️  Repository sale - version '$(VERSION)'$(NC)"; \
	else \
		echo "$(GREEN)✅ Environnement Git propre - version '$(VERSION)'$(NC)"; \
	fi

# Version pour différents environnements
version-env:
	@echo "$(GREEN)📋 Versions par environnement:$(NC)"
	@echo "  Développement: $(VERSION)"
	@echo "  Production: $(shell echo $(VERSION) | sed 's/-dirty//' | sed 's/-dev//')"
	@echo "  Docker: $(if $(findstring dev,$(VERSION)),dev,$(shell echo $(VERSION) | sed 's/^v//'))"
	@echo "  .NET: $(DOTNET_VERSION)"

# === Build avec versions ===

# Installation des dépendances
install:
	@echo "$(GREEN)📦 Installation des dépendances...$(NC)"
	dotnet restore $(PROJECT_FILE)
	@echo "$(GREEN)✅ Dépendances installées$(NC)"

# Build avec gestion intelligente des versions
build:
	@echo "$(GREEN)🔨 Build du projet $(VERSION)...$(NC)"
	@if [ "$(VERSION)" = "dev" ]; then \
		echo "$(YELLOW)⚠️  Mode développement (pas de Git)$(NC)"; \
	elif [ "$(IS_GIT_REPO)" = "false" ]; then \
		echo "$(YELLOW)⚠️  Pas de repository Git détecté$(NC)"; \
	fi
	dotnet build $(PROJECT_FILE) \
		--configuration Release \
		--property:Version="$(DOTNET_VERSION)" \
		--property:AssemblyVersion="$(DOTNET_VERSION)" \
		--property:FileVersion="$(DOTNET_VERSION)" \
		--property:InformationalVersion="$(VERSION_FULL)" \
		--property:GitTag="$(GIT_TAG)" \
		--property:GitCommit="$(GIT_COMMIT)" \
		--property:GitBranch="$(GIT_BRANCH)" \
		--property:GitDirty="$(GIT_DIRTY)" \
		--property:BuildTime="$(BUILD_TIME)" \
		--property:BuildEnvironment="$(if $(IS_CI),CI,Local)"
	@echo "$(GREEN)✅ Build terminé $(VERSION)$(NC)"

# Build développement (force version dev)
build-dev:
	@echo "$(GREEN)🔨 Build développement...$(NC)"
	$(MAKE) build VERSION=dev DOTNET_VERSION=0.0.1

# Build pour CI/CD
build-ci:
	@echo "$(GREEN)🔨 Build CI/CD...$(NC)"
	@if [ "$(VERSION)" = "dev" ] && [ "$(IS_CI)" = "true" ]; then \
		echo "$(RED)❌ Version 'dev' non autorisée en CI/CD$(NC)"; \
		echo "$(YELLOW)Créez un tag ou définissez VERSION$(NC)"; \
		exit 1; \
	fi
	$(MAKE) build

# Build release sécurisé
build-release: check-env
	@if [ "$(VERSION)" = "dev" ]; then \
		echo "$(RED)❌ Impossible de faire un build release sans tag Git$(NC)"; \
		echo "$(YELLOW)Créez un tag: git tag v1.0.0 && git push origin v1.0.0$(NC)"; \
		exit 1; \
	fi
	@if echo "$(VERSION)" | grep -q dirty; then \
		echo "$(RED)❌ Repository sale, commitez vos changements$(NC)"; \
		exit 1; \
	fi
	@echo "$(GREEN)🚀 Build release $(VERSION)...$(NC)"
	$(MAKE) build

# Workflow intelligent selon l'environnement
auto-build:
	@echo "$(GREEN)🤖 Build automatique...$(NC)"
	@if [ "$(IS_CI)" = "true" ]; then \
		echo "$(GREEN)Environnement CI/CD détecté$(NC)"; \
		$(MAKE) build-ci; \
	elif [ "$(VERSION)" = "dev" ]; then \
		echo "$(GREEN)Environnement développement détecté$(NC)"; \
		$(MAKE) build-dev; \
	else \
		echo "$(GREEN)Build standard$(NC)"; \
		$(MAKE) build; \
	fi

# Créer un tag de développement
create-dev-tag:
	@if [ -z "$(TAG)" ]; then \
		echo "$(RED)❌ Usage: make create-dev-tag TAG=v1.0.0-alpha$(NC)"; \
		exit 1; \
	fi
	@echo "$(GREEN)🏷️  Création du tag développement $(TAG)...$(NC)"
	git tag -a $(TAG) -m "Development tag $(TAG)"
	@echo "$(GREEN)✅ Tag $(TAG) créé (push avec: git push origin $(TAG))$(NC)"

# Nettoyage des tags de développement
clean-dev-tags:
	@echo "$(GREEN)🧹 Suppression des tags de développement...$(NC)"
	@git tag -l | grep -E "(dev|alpha|beta|rc)" | while read tag; do \
		echo "Suppression de $$tag"; \
		git tag -d $$tag; \
	done
	@echo "$(GREEN)✅ Tags de développement supprimés$(NC)"

# Tests
test:
	@echo "$(GREEN)🧪 Lancement des tests...$(NC)"
	dotnet test $(PROJECT_FILE) --logger "console;verbosity=detailed"

# Nettoyage
clean:
	@echo "$(GREEN)🧹 Nettoyage des artifacts...$(NC)"
	dotnet clean $(PROJECT_FILE)
	rm -rf bin obj
	@echo "$(GREEN)✅ Nettoyage terminé$(NC)"


# Cible interne (privée) pour lancer l'application.
# L'environnement est passé via une variable ENV.
_run: build
	@echo "$(GREEN)🚀 Lancement en mode $(ENV)...$(NC)"
	@echo "$(YELLOW)URL: http://localhost:5000 | https://localhost:5001$(NC)"
	ASPNETCORE_ENVIRONMENT=$(ENV) dotnet run \
		--project $(PROJECT_FILE) \
		--configuration Release \
		--no-build \
		--urls="http://localhost:5000;https://localhost:5001"

# Cibles publiques, faciles à utiliser
run-dev:
	@$(MAKE) _run ENV=Development

run-prod:
	@$(MAKE) _run ENV=Production

# Lancement Railway (simulation)
run-railway:
	@echo "$(GREEN)🚀 Lancement en mode Railway...$(NC)"
	@echo "$(YELLOW)URL: http://localhost:8080$(NC)"
	ASPNETCORE_ENVIRONMENT=ProductionRailway dotnet run --project $(PROJECT_FILE) --no-build --urls="http://localhost:8080"

# Configuration développement complète
dev-setup: ef-install ssl-dev install build
	@echo "$(GREEN)🎉 Environnement de développement configuré !$(NC)"
	@echo "$(YELLOW)Vous pouvez maintenant utiliser: make run-dev$(NC)"

# === Installation Ubuntu ===

# Installation complète environnement Ubuntu
ubuntu-setup: system-update dotnet-install vscode-install tools-install ef-install
	@echo "$(GREEN)🎉 Installation complète Ubuntu terminée !$(NC)"
	@echo "$(YELLOW)Redémarrez votre terminal pour que tous les outils soient disponibles$(NC)"

# Mise à jour du système Ubuntu
system-update:
	@echo "$(GREEN)🐧 Mise à jour du système Ubuntu...$(NC)"
	sudo apt update && sudo apt upgrade -y
	sudo apt install -y curl wget apt-transport-https software-properties-common
	@echo "$(GREEN)✅ Système Ubuntu mis à jour$(NC)"

# Installation de .NET 8 SDK
dotnet-install:
	@echo "$(GREEN)🔧 Installation de .NET 8 SDK...$(NC)"
	@if command -v dotnet >/dev/null 2>&1; then \
		echo "$(YELLOW)⚠️  .NET SDK déjà installé : $(shell dotnet --version)$(NC)"; \
	else \
		wget https://packages.microsoft.com/config/ubuntu/$(shell lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb; \
		sudo dpkg -i packages-microsoft-prod.deb; \
		rm packages-microsoft-prod.deb; \
		sudo apt update; \
		sudo apt install -y dotnet-sdk-8.0; \
		echo "$(GREEN)✅ .NET 8 SDK installé$(NC)"; \
	fi

# Installation de Visual Studio Code + extensions
vscode-install:
	@echo "$(GREEN)💻 Installation de Visual Studio Code...$(NC)"
	@if command -v code >/dev/null 2>&1; then \
		echo "$(YELLOW)⚠️  Visual Studio Code déjà installé$(NC)"; \
	else \
		wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > packages.microsoft.gpg; \
		sudo install -o root -g root -m 644 packages.microsoft.gpg /etc/apt/trusted.gpg.d/; \
		sudo sh -c 'echo "deb [arch=amd64,arm64,armhf signed-by=/etc/apt/trusted.gpg.d/packages.microsoft.gpg] https://packages.microsoft.com/repos/code stable main" > /etc/apt/sources.list.d/vscode.list'; \
		sudo apt update; \
		sudo apt install -y code; \
		echo "$(GREEN)✅ Visual Studio Code installé$(NC)"; \
	fi
	@echo "$(GREEN)🔌 Installation des extensions Visual Studio Code...$(NC)"
	code --install-extension ms-dotnettools.csharp
	code --install-extension ms-dotnettools.csdevkit
	code --install-extension ms-dotnettools.vscode-dotnet-runtime
	code --install-extension ms-vscode.vscode-json
	code --install-extension bradlc.vscode-tailwindcss
	code --install-extension esbenp.prettier-vscode
	code --install-extension ms-vscode.vscode-typescript-next
	code --install-extension formulahendry.auto-rename-tag
	code --install-extension christian-kohler.path-intellisense
	code --install-extension ms-vscode-remote.remote-containers
	code --install-extension ms-azuretools.vscode-docker
	code --install-extension humao.rest-client
	code --install-extension ms-dotnettools.dotnet-interactive-vscode
	@echo "$(GREEN)✅ Extensions Visual Studio Code installées$(NC)"

# Installation de tous les outils de développement
tools-install:
	@echo "$(GREEN)🛠️  Installation des outils de développement...$(NC)"

	# Git
	@if ! command -v git >/dev/null 2>&1; then \
		sudo apt install -y git; \
		echo "$(GREEN)✅ Git installé$(NC)"; \
	else \
		echo "$(YELLOW)⚠️  Git déjà installé : $(shell git --version)$(NC)"; \
	fi

	# Node.js et npm (pour outils front-end)
	@if ! command -v node >/dev/null 2>&1; then \
		curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -; \
		sudo apt install -y nodejs; \
		echo "$(GREEN)✅ Node.js installé$(NC)"; \
	else \
		echo "$(YELLOW)⚠️  Node.js déjà installé : $(shell node --version)$(NC)"; \
	fi

	# Docker
	@if ! command -v docker >/dev/null 2>&1; then \
		sudo apt install -y ca-certificates gnupg lsb-release; \
		curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg; \
		echo "deb [arch=$(shell dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(shell lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null; \
		sudo apt update; \
		sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin; \
		sudo usermod -aG docker $$USER; \
		echo "$(GREEN)✅ Docker installé$(NC)"; \
		echo "$(YELLOW)⚠️  Redémarrez votre session pour utiliser Docker sans sudo$(NC)"; \
	else \
		echo "$(YELLOW)⚠️  Docker déjà installé : $(shell docker --version)$(NC)"; \
	fi

	# MySQL Client
	@if ! command -v mysql >/dev/null 2>&1; then \
		sudo apt install -y mysql-client; \
		echo "$(GREEN)✅ MySQL Client installé$(NC)"; \
	else \
		echo "$(YELLOW)⚠️  MySQL Client déjà installé$(NC)"; \
	fi

	# Postman (alternative en ligne de commande : HTTPie)
	@if ! command -v http >/dev/null 2>&1; then \
		sudo apt install -y httpie; \
		echo "$(GREEN)✅ HTTPie installé$(NC)"; \
	else \
		echo "$(YELLOW)⚠️  HTTPie déjà installé$(NC)"; \
	fi

	# OpenSSL pour les certificats
	@if ! command -v openssl >/dev/null 2>&1; then \
		sudo apt install -y openssl; \
		echo "$(GREEN)✅ OpenSSL installé$(NC)"; \
	else \
		echo "$(YELLOW)⚠️  OpenSSL déjà installé$(NC)"; \
	fi

	# Railway CLI
	@if ! command -v railway >/dev/null 2>&1; then \
		curl -fsSL https://railway.app/install.sh | sh; \
		echo "$(GREEN)✅ Railway CLI installé$(NC)"; \
	else \
		echo "$(YELLOW)⚠️  Railway CLI déjà installé$(NC)"; \
	fi

	# Outils de build
	sudo apt install -y build-essential

	@echo "$(GREEN)✅ Tous les outils de développement installés$(NC)"

# === Gestion des packages NuGet ===

# Restaurer les packages NuGet
nuget-restore:
	@echo "$(GREEN)📦 Restauration des packages NuGet...$(NC)"
	dotnet restore $(PROJECT_FILE) --verbosity normal
	@echo "$(GREEN)✅ Packages NuGet restaurés$(NC)"

# Mettre à jour les packages NuGet
nuget-update:
	@echo "$(GREEN)📦 Mise à jour des packages NuGet...$(NC)"
	dotnet list $(PROJECT_FILE) package --outdated
	@echo "$(YELLOW)Pour mettre à jour un package spécifique, utilisez:$(NC)"
	@echo "$(YELLOW)  dotnet add package <PackageName>$(NC)"

# Nettoyer le cache NuGet
nuget-clean:
	@echo "$(GREEN)🧹 Nettoyage du cache NuGet...$(NC)"
	dotnet nuget locals all --clear
	@echo "$(GREEN)✅ Cache NuGet nettoyé$(NC)"

# Lister les packages obsolètes
nuget-outdated:
	@echo "$(GREEN)📋 Packages NuGet obsolètes:$(NC)"
	dotnet list $(PROJECT_FILE) package --outdated --include-transitive

# === Gestion d'Entity Framework Core ===

# Installation d'Entity Framework Core CLI
ef-install:
	@echo "$(GREEN)🔧 Installation d'Entity Framework Core globalement...$(NC)"
	dotnet tool install --global dotnet-ef
	@echo "$(GREEN)✅ Entity Framework Core installé$(NC)"

# Vérification de la version d'Entity Framework Core
ef-check:
	@echo "$(GREEN)🔍 Vérification de la version d'Entity Framework Core...$(NC)"
	@if command -v dotnet-ef >/dev/null 2>&1; then \
		dotnet ef --version; \
		echo "$(GREEN)✅ Entity Framework Core est installé$(NC)"; \
	else \
		echo "$(RED)❌ Entity Framework Core n'est pas installé$(NC)"; \
		echo "$(YELLOW)Utilisez: make ef-install$(NC)"; \
	fi

# === Gestion des certificats SSL ===

# Nettoyage certificats
ssl-clean:
	@echo "$(GREEN)🧹 Nettoyage des certificats...$(NC)"
	dotnet dev-certs https --clean
	rm -f localhost.pfx localhost.crt localhost.key
	@echo "$(GREEN)✅ Certificats nettoyés$(NC)"

# Génération certificats développement
ssl-dev:
	@echo "$(GREEN)🔒 Génération des certificats de développement...$(NC)"
	@echo "$(YELLOW)Méthode 1: Certificats .NET$(NC)"
	dotnet dev-certs https --clean
	dotnet dev-certs https --trust
	@echo "$(YELLOW)Méthode 2: Certificats OpenSSL personnalisés$(NC)"
	@if command -v openssl >/dev/null 2>&1; then \
		openssl req -x509 -newkey rsa:4096 -keyout localhost.key -out localhost.crt -days 365 -nodes \
			-subj "/C=FR/ST=Normandy/L=Rouen/O=LibraryAPI/CN=localhost" \
			-addext "subjectAltName=DNS:localhost,IP:127.0.0.1"; \
		openssl pkcs12 -export -out localhost.pfx -inkey localhost.key -in localhost.crt -password pass:DevPassword123; \
		echo "$(GREEN)✅ Certificats OpenSSL créés$(NC)"; \
	else \
		echo "$(RED)❌ OpenSSL non installé, utilisation des certificats .NET uniquement$(NC)"; \
	fi

# Approbation certificats système
ssl-trust:
	@echo "$(GREEN)🔒 Approbation des certificats système...$(NC)"
	@if [ -f localhost.crt ]; then \
		sudo cp localhost.crt /usr/local/share/ca-certificates/localhost-dev.crt; \
		sudo update-ca-certificates; \
		echo "$(GREEN)✅ Certificat ajouté au système$(NC)"; \
	else \
		echo "$(RED)❌ Fichier localhost.crt non trouvé$(NC)"; \
	fi

# === Migrations Entity Framework ===

# Vérifier Entity Framework CLI
check-ef:
	@if ! command -v dotnet-ef >/dev/null 2>&1; then \
		echo "$(YELLOW)⚠️  Entity Framework CLI non installé, installation...$(NC)"; \
		make ef-install; \
	fi

# Ajouter migration
migration-add: check-ef
	@if [ -z "$(NAME)" ]; then \
		echo "$(RED)❌ Usage: make migration-add NAME=NomDeLaMigration$(NC)"; \
		exit 1; \
	fi
	@echo "$(GREEN)📝 Ajout de la migration: $(NAME)$(NC)"
	dotnet ef migrations add $(NAME) --project $(PROJECT_FILE)

# Appliquer migrations
migration-update: check-ef
	@echo "$(GREEN)📝 Application des migrations...$(NC)"
	dotnet ef database update --project $(PROJECT_FILE)
	@echo "$(GREEN)✅ Migrations appliquées$(NC)"

# Lister migrations
migration-list: check-ef
	@echo "$(GREEN)📝 Liste des migrations:$(NC)"
	dotnet ef migrations list --project $(PROJECT_FILE)

# Supprimer dernière migration
migration-remove: check-ef
	@echo "$(GREEN)❌ Suppression de la dernière migration...$(NC)"
	dotnet ef migrations remove --project $(PROJECT_FILE)

# Générer script SQL
migration-script: check-ef
	@echo "$(GREEN)📝 Génération du script SQL...$(NC)"
	dotnet ef migrations script --project $(PROJECT_FILE) --output migrations.sql
	@echo "$(GREEN)✅ Script généré: migrations.sql$(NC)"

# === Railway ===

# Vérifier Railway CLI
check-railway:
	@if ! command -v railway >/dev/null 2>&1; then \
		echo "$(YELLOW)⚠️  Railway CLI non installé, installation...$(NC)"; \
		curl -fsSL https://railway.app/install.sh | sh; \
	fi

# Connexion Railway
railway-login: check-railway
	@echo "$(GREEN)🚀 Connexion à Railway...$(NC)"
	railway login

# Déploiement Railway
railway-deploy: check-railway build
	@echo "$(GREEN)🚀 Déploiement sur Railway...$(NC)"
	railway up

# Logs Railway
railway-logs: check-railway
	@echo "$(GREEN)📋 Logs Railway:$(NC)"
	railway logs

# Variables Railway
railway-vars: check-railway
	@echo "$(GREEN)🔧 Variables Railway:$(NC)"
	railway variables

# === Docker ===

# Build image Docker
docker-build:
	@echo "$(GREEN)🐳 Build de l'image Docker...$(NC)"
	docker build -t $(DOCKER_IMAGE):$(DOCKER_TAG) .
	@echo "$(GREEN)✅ Image Docker créée$(NC)"

# Lancement conteneur
docker-run: docker-build
	@echo "$(GREEN)🐳 Lancement du conteneur...$(NC)"
	@echo "$(YELLOW)URL: http://localhost:8080$(NC)"
	docker run -p 8080:8080 --rm --name $(PROJECT_NAME) $(DOCKER_IMAGE):$(DOCKER_TAG)

# Nettoyage Docker
docker-clean:
	@echo "$(GREEN)🧹 Nettoyage Docker...$(NC)"
	docker rmi $(DOCKER_IMAGE):$(DOCKER_TAG) 2>/dev/null || true
	docker system prune -f

# === Utilitaires ===

# Vérification santé du projet
health-check:
	@echo "$(GREEN)🏥 Vérification de santé du projet...$(NC)"
	@echo "$(YELLOW)Version .NET:$(NC)"
	@dotnet --version
	@echo "$(YELLOW)Packages obsolètes:$(NC)"
	@dotnet list package --outdated
	@echo "$(YELLOW)Sécurité:$(NC)"
	@dotnet list package --vulnerable

# Installation outils de développement
install-tools: ef-install
	@echo "$(GREEN)🛠️  Installation des outils de développement globaux...$(NC)"
	@if ! command -v dotnet-outdated >/dev/null 2>&1; then \
		dotnet tool install --global dotnet-outdated-tool; \
		echo "$(GREEN)✅ dotnet-outdated installé$(NC)"; \
	else \
		echo "$(YELLOW)⚠️  dotnet-outdated déjà installé$(NC)"; \
	fi
	@echo "$(GREEN)✅ Outils globaux installés$(NC)"

# Mise à jour des packages
update-packages:
	@echo "$(GREEN)📦 Mise à jour des packages...$(NC)"
	dotnet-outdated upgrade
	@echo "$(GREEN)✅ Packages mis à jour$(NC)"

# Publish avec version Git
publish:
	@echo "$(GREEN)📦 Publication $(VERSION)...$(NC)"
	dotnet publish $(PROJECT_FILE) \
		--configuration Release \
		--output ./publish \
		-p:Version="$(DOTNET_VERSION)" \
		-p:InformationalVersion="$(VERSION_FULL)" \
		-p:GitTag="$(GIT_TAG)" \
		-p:GitCommit="$(GIT_COMMIT)" \
		-p:GitBranch="$(GIT_BRANCH)" \
		-p:BuildTime="$(BUILD_TIME)"
	@echo "$(GREEN)✅ Publication terminée dans ./publish$(NC)"

# Workflow complet de release
workflow-release:
	@if [ -z "$(TAG)" ]; then \
		echo "$(RED)❌ Usage: make workflow-release TAG=v1.2.3$(NC)"; \
		exit 1; \
	fi
	@echo "$(GREEN)🔄 Workflow de release pour $(TAG)...$(NC)"
	$(MAKE) check-env
	@if [ -n "$(shell git status --porcelain)" ]; then \
		echo "$(RED)❌ Repository sale, commit d'abord$(NC)"; \
		exit 1; \
	fi
	git tag -a $(TAG) -m "Release $(TAG)"
	$(MAKE) build
	git push origin $(TAG)
	@echo "$(GREEN)🎉 Release $(TAG) terminée et poussée !$(NC)"

# Aide pour les commandes de version
version-help:
	@echo "$(GREEN)📦 Guide des versions Git:$(NC)"
	@echo ""
	@echo "$(YELLOW)🏷️  Créer des tags:$(NC)"
	@echo "  git tag v1.0.0                    # Tag simple"
	@echo "  git tag -a v1.0.0 -m 'Release'    # Tag avec message"
	@echo "  git push origin v1.0.0            # Pousser le tag"
	@echo ""
	@echo "$(YELLOW)🔍 Vérifier l'état:$(NC)"
	@echo "  make version-info                 # Voir toutes les infos"
	@echo "  make check-env                    # Vérifier environnement"
	@echo "  git status                        # État du repository"
	@echo ""
	@echo "$(YELLOW)🚀 Builds:$(NC)"
	@echo "  make build                        # Build auto avec version Git"
	@echo "  make build-dev                    # Build développement"
	@echo "  make build-release                # Build release (exige tag propre)"
	@echo "  make auto-build                   # Build intelligent"
	@echo ""
	@echo "$(YELLOW)📋 Exemples de versions:$(NC)"
	@echo "  Sans Git: dev"
	@echo "  Pas de tag: dev"
	@echo "  Tag propre: v1.0.2"
	@echo "  Repository sale: v1.0.2-dirty"
	@echo "  Tag développement: v1.0.0-alpha"

# Docker avec version robuste
docker-build-versioned:
	@echo "$(GREEN)🐳 Build Docker $(VERSION)...$(NC)"
	docker build \
		--build-arg VERSION="$(DOTNET_VERSION)" \
		--build-arg VERSION_FULL="$(VERSION_FULL)" \
		--build-arg GIT_TAG="$(GIT_TAG)" \
		--build-arg GIT_COMMIT="$(GIT_COMMIT)" \
		--build-arg BUILD_TIME="$(BUILD_TIME)" \
		-t $(DOCKER_IMAGE):$(if $(findstring dev,$(VERSION)),dev,$(shell echo $(VERSION) | sed 's/^v//')) \
		-t $(DOCKER_IMAGE):latest .
	@echo "$(GREEN)✅ Image Docker créée$(NC)"


