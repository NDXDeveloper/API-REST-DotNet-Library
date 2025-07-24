# Makefile pour les tests LibraryAPI
# Usage: make [command]
# Ce Makefile doit être situé dans le dossier racine du projet

# Variables
PROJECT_NAME = LibraryAPI
SRC_PROJECT = src/src.csproj
TEST_PROJECT = tests/LibraryAPI.Tests/LibraryAPI.Tests.csproj
SOLUTION_FILE = LibraryAPI.sln

# Couleurs pour l'affichage
GREEN = \033[0;32m
YELLOW = \033[1;33m
RED = \033[0;31m
BLUE = \033[0;34m
NC = \033[0m # No Color

.PHONY: help test test-debug test-watch test-coverage create-test-project setup-tests clean-tests build-all build-debug run-tests-only

# Affichage de l'aide par défaut
help:
	@echo "$(GREEN)🧪 LibraryAPI - Gestion des tests$(NC)"
	@echo ""
	@echo "$(YELLOW)🧪 Tests :$(NC)"
	@echo "  make test            - Lancer tous les tests"
	@echo "  make test-debug      - Lancer les tests en mode Debug"
	@echo "  make test-watch      - Lancer les tests en mode watch"
	@echo "  make test-coverage   - Lancer les tests avec couverture de code"
	@echo "  make run-tests-only  - Lancer uniquement les tests (sans build)"
	@echo ""
	@echo "$(YELLOW)🏗️  Configuration :$(NC)"
	@echo "  make setup-tests     - Configurer l'environnement de test complet"
	@echo "  make create-test-project - Créer le projet de test"
	@echo "  make create-solution - Créer le fichier solution"
	@echo "  make fix-src-tests   - Exclure les tests du projet src"
	@echo ""
	@echo "$(YELLOW)🛠️  Build :$(NC)"
	@echo "  make build-all       - Builder toute la solution (Release)"
	@echo "  make build-debug     - Builder toute la solution (Debug)"
	@echo "  make build-src       - Builder uniquement le projet src"
	@echo "  make build-tests     - Builder uniquement les tests"
	@echo ""
	@echo "$(YELLOW)🧹 Nettoyage :$(NC)"
	@echo "  make clean-tests     - Nettoyer les artifacts de test"
	@echo "  make clean-all       - Nettoyer tous les artifacts"
	@echo ""
	@echo "$(BLUE)📍 Commandes utiles depuis le dossier racine :$(NC)"
	@echo "  cd src && make run-dev    - Lancer l'API en développement"
	@echo "  cd src && make help       - Voir l'aide du projet principal"

# === Tests ===

# Lancer tous les tests
test: build-all
	@echo "$(GREEN)🧪 Lancement de tous les tests...$(NC)"
	@if [ -f "$(SOLUTION_FILE)" ]; then \
		dotnet test $(SOLUTION_FILE) --configuration Release --logger "console;verbosity=normal" --no-build; \
	elif [ -f "$(TEST_PROJECT)" ]; then \
		dotnet test $(TEST_PROJECT) --configuration Release --logger "console;verbosity=normal"; \
	else \
		echo "$(RED)❌ Aucun projet de test trouvé$(NC)"; \
		echo "$(YELLOW)Utilisez: make setup-tests$(NC)"; \
		exit 1; \
	fi
	@echo "$(GREEN)✅ Tests terminés$(NC)"

# Lancer les tests en mode Debug (pour le développement)
test-debug: build-debug
	@echo "$(GREEN)🧪 Lancement des tests en mode Debug...$(NC)"
	@if [ -f "$(SOLUTION_FILE)" ]; then \
		dotnet test $(SOLUTION_FILE) --configuration Debug --logger "console;verbosity=normal" --no-build; \
	elif [ -f "$(TEST_PROJECT)" ]; then \
		dotnet test $(TEST_PROJECT) --configuration Debug --logger "console;verbosity=normal" --no-build; \
	else \
		echo "$(RED)❌ Aucun projet de test trouvé$(NC)"; \
		echo "$(YELLOW)Utilisez: make setup-tests$(NC)"; \
		exit 1; \
	fi
	@echo "$(GREEN)✅ Tests terminés$(NC)"

# Lancer les tests en mode watch
test-watch:
	@echo "$(GREEN)👁️  Lancement des tests en mode watch...$(NC)"
	@if [ -f "$(TEST_PROJECT)" ]; then \
		dotnet watch test $(TEST_PROJECT) --configuration Debug --logger "console;verbosity=normal"; \
	else \
		echo "$(RED)❌ Projet de test non trouvé$(NC)"; \
		echo "$(YELLOW)Utilisez: make setup-tests$(NC)"; \
	fi

# Tests avec couverture de code
test-coverage:
	@echo "$(GREEN)📊 Tests avec couverture de code...$(NC)"
	@if [ -f "$(TEST_PROJECT)" ]; then \
		dotnet test $(TEST_PROJECT) --configuration Debug --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"; \
		echo "$(GREEN)✅ Rapports de couverture générés dans TestResults/$(NC)"; \
	else \
		echo "$(RED)❌ Projet de test non trouvé$(NC)"; \
	fi

# Lancer uniquement les tests sans build
run-tests-only:
	@echo "$(GREEN)🧪 Lancement des tests (sans build)...$(NC)"
	@if [ -f "$(SOLUTION_FILE)" ]; then \
		dotnet test $(SOLUTION_FILE) --configuration Release --no-build --logger "console;verbosity=normal"; \
	elif [ -f "$(TEST_PROJECT)" ]; then \
		dotnet test $(TEST_PROJECT) --configuration Release --no-build --logger "console;verbosity=normal"; \
	else \
		echo "$(RED)❌ Aucun projet de test trouvé$(NC)"; \
	fi

# === Configuration ===

# Configuration complète de l'environnement de test
setup-tests: create-test-project create-solution fix-src-tests
	@echo "$(GREEN)🎉 Environnement de test configuré !$(NC)"
	@echo "$(YELLOW)Vous pouvez maintenant utiliser: make test$(NC)"

# Créer le projet de test
create-test-project:
	@echo "$(GREEN)🏗️  Création du projet de test...$(NC)"
	@if [ -f "$(TEST_PROJECT)" ]; then \
		echo "$(YELLOW)⚠️  Le projet de test existe déjà$(NC)"; \
	else \
		mkdir -p tests/LibraryAPI.Tests; \
		cat > $(TEST_PROJECT) << 'EOF';\
<Project Sdk="Microsoft.NET.Sdk">\
\n\
\n  <PropertyGroup>\
\n    <TargetFramework>net8.0</TargetFramework>\
\n    <ImplicitUsings>enable</ImplicitUsings>\
\n    <Nullable>enable</Nullable>\
\n    <IsPackable>false</IsPackable>\
\n    <IsTestProject>true</IsTestProject>\
\n  </PropertyGroup>\
\n\
\n  <ItemGroup>\
\n    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.6.0" />\
\n    <PackageReference Include="xunit" Version="2.9.2" />\
\n    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">\
\n      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>\
\n      <PrivateAssets>all</PrivateAssets>\
\n    </PackageReference>\
\n    <PackageReference Include="Moq" Version="4.20.72" />\
\n    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.8" />\
\n    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.8" />\
\n    <PackageReference Include="coverlet.collector" Version="6.0.0">\
\n      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>\
\n      <PrivateAssets>all</PrivateAssets>\
\n    </PackageReference>\
\n  </ItemGroup>\
\n\
\n  <ItemGroup>\
\n    <ProjectReference Include="..\..\src\src.csproj" />\
\n  </ItemGroup>\
\n\
\n</Project>\
EOF\
		echo "$(GREEN)✅ Projet de test créé$(NC)"; \
	fi

# Créer le fichier solution
create-solution:
	@echo "$(GREEN)🏗️  Création du fichier solution...$(NC)"
	@if [ -f "$(SOLUTION_FILE)" ]; then \
		echo "$(YELLOW)⚠️  Le fichier solution existe déjà$(NC)"; \
	else \
		dotnet new sln -n $(PROJECT_NAME); \
		dotnet sln $(SOLUTION_FILE) add $(SRC_PROJECT); \
		if [ -f "$(TEST_PROJECT)" ]; then \
			dotnet sln $(SOLUTION_FILE) add $(TEST_PROJECT); \
		fi; \
		echo "$(GREEN)✅ Fichier solution créé$(NC)"; \
	fi

# Exclure les tests du projet src
fix-src-tests:
	@echo "$(GREEN)🔧 Exclusion des tests du projet src...$(NC)"
	@if grep -q "tests/\*\*" $(SRC_PROJECT); then \
		echo "$(YELLOW)⚠️  L'exclusion des tests est déjà configurée$(NC)"; \
	else \
		cp $(SRC_PROJECT) $(SRC_PROJECT).backup; \
		sed -i '/<\/PropertyGroup>/a\\n  <ItemGroup>\n    <Compile Remove="tests/**/*.cs" />\n    <Content Remove="tests/**/*" />\n    <None Remove="tests/**/*" />\n  </ItemGroup>' $(SRC_PROJECT); \
		echo "$(GREEN)✅ Tests exclus du projet src$(NC)"; \
		echo "$(YELLOW)Backup sauvegardé dans $(SRC_PROJECT).backup$(NC)"; \
	fi

# === Build ===

# Builder toute la solution
build-all:
	@echo "$(GREEN)🔨 Build de toute la solution (Release)...$(NC)"
	@if [ -f "$(SOLUTION_FILE)" ]; then \
		dotnet build $(SOLUTION_FILE) --configuration Release; \
	else \
		echo "$(YELLOW)⚠️  Pas de solution, build des projets individuellement$(NC)"; \
		make build-src; \
		make build-tests; \
	fi
	@echo "$(GREEN)✅ Build terminé$(NC)"

# Builder toute la solution en Debug
build-debug:
	@echo "$(GREEN)🔨 Build de toute la solution (Debug)...$(NC)"
	@if [ -f "$(SOLUTION_FILE)" ]; then \
		dotnet build $(SOLUTION_FILE) --configuration Debug; \
	else \
		echo "$(YELLOW)⚠️  Pas de solution, build des projets individuellement$(NC)"; \
		dotnet build $(SRC_PROJECT) --configuration Debug; \
		if [ -f "$(TEST_PROJECT)" ]; then \
			dotnet build $(TEST_PROJECT) --configuration Debug; \
		fi; \
	fi
	@echo "$(GREEN)✅ Build Debug terminé$(NC)"

# Builder uniquement le projet src
build-src:
	@echo "$(GREEN)🔨 Build du projet src...$(NC)"
	@if [ -f "$(SRC_PROJECT)" ]; then \
		dotnet build $(SRC_PROJECT) --configuration Release; \
	else \
		echo "$(RED)❌ Projet src non trouvé$(NC)"; \
	fi

# Builder uniquement les tests
build-tests:
	@echo "$(GREEN)🔨 Build des tests...$(NC)"
	@if [ -f "$(TEST_PROJECT)" ]; then \
		dotnet build $(TEST_PROJECT) --configuration Release; \
	else \
		echo "$(YELLOW)⚠️  Projet de test non trouvé$(NC)"; \
	fi

# === Nettoyage ===

# Nettoyer les artifacts de test
clean-tests:
	@echo "$(GREEN)🧹 Nettoyage des artifacts de test...$(NC)"
	@if [ -d "tests" ]; then \
		find tests -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true; \
		find tests -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true; \
		rm -rf TestResults; \
	fi
	@echo "$(GREEN)✅ Artifacts de test nettoyés$(NC)"

# Nettoyer tous les artifacts
clean-all: clean-tests
	@echo "$(GREEN)🧹 Nettoyage de tous les artifacts...$(NC)"
	@if [ -f "$(SOLUTION_FILE)" ]; then \
		dotnet clean $(SOLUTION_FILE); \
	fi
	@if [ -d "src" ]; then \
		find src -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true; \
		find src -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true; \
	fi
	@echo "$(GREEN)✅ Tous les artifacts nettoyés$(NC)"

# === Utilitaires ===

# Afficher la structure du projet
show-structure:
	@echo "$(GREEN)📁 Structure du projet:$(NC)"
	@tree -I 'bin|obj|node_modules|TestResults' -a --dirsfirst || find . -type d \( -name bin -o -name obj -o -name node_modules -o -name TestResults \) -prune -o -type f -print | head -20

# Informations sur les projets
info:
	@echo "$(GREEN)ℹ️  Informations sur le projet:$(NC)"
	@echo "$(YELLOW)Projet principal:$(NC) $(SRC_PROJECT)"
	@echo "$(YELLOW)Projet de test:$(NC) $(TEST_PROJECT)"
	@echo "$(YELLOW)Solution:$(NC) $(SOLUTION_FILE)"
	@echo ""
	@if [ -f "$(SOLUTION_FILE)" ]; then \
		echo "$(GREEN)📋 Projets dans la solution:$(NC)"; \
		dotnet sln $(SOLUTION_FILE) list; \
	fi
