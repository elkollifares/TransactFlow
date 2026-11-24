# TransactFlow

TransactFlow est un MVP fonctionnel de gestion de transactions financières. Il propose un CRUD complet, des indicateurs revenus/dépenses/solde, un dashboard Angular responsive et les artefacts d’intégration Oracle, PL/SQL, SQL Server, SSIS, Control‑M et Denodo.

![Tableau de bord TransactFlow](docs/dashboard.png)

## Démarrage local

Prérequis : .NET SDK 10 LTS, Node.js 24 LTS et npm 11+.

```bash
# Terminal 1 — API
DOTNET_CLI_HOME="$PWD/.dotnet" dotnet run --project src/TransactFlow.Api

# Terminal 2 — interface
cd src/TransactFlow.Web
npm install
npm start
```

Ouvrir <http://localhost:4200>. L’API répond sur <http://localhost:5090/api/health>. Au premier lancement, six transactions de démonstration sont créées dans `data/transactions.json` (relatif au dossier depuis lequel l’API est lancée).

Après le premier `npm install`, il est aussi possible de lancer les deux services avec :

```bash
chmod +x scripts/*.sh
./scripts/dev.sh
```

Pour produire une application autonome où .NET sert directement le dashboard Angular :

```bash
./scripts/package.sh
ASPNETCORE_URLS=http://localhost:5090 dotnet artifacts/transactflow/TransactFlow.Api.dll
```

## Fonctionnalités disponibles

- créer, rechercher, filtrer, modifier et supprimer une transaction ;
- calculer revenus, dépenses, solde, dépenses par catégorie et tendance mensuelle ;
- exécuter le recalcul local ou la vraie procédure Oracle via `POST /api/transactions/recalculate` ;
- validation côté API et formulaire ;
- stockage JSON atomique sans base externe pour la démonstration ;
- schéma Oracle et procédure `SP_RECALCULATE_TRANSACTION_SUMMARY` prêts à déployer ;
- modèle en étoile SQL Server et chargement incrémental `MERGE` ;
- squelette de projet SSIS, export Bash, job Control‑M et vue Denodo ;
- pipelines GitHub Actions et Azure DevOps.

## API REST

| Méthode | Route | Rôle |
| --- | --- | --- |
| `GET` | `/api/health` | Santé de l’API |
| `GET` | `/api/transactions` | Liste, avec `search`, `type`, `category` |
| `GET` | `/api/transactions/{id}` | Détail |
| `POST` | `/api/transactions` | Création |
| `PUT` | `/api/transactions/{id}` | Modification |
| `DELETE` | `/api/transactions/{id}` | Suppression |
| `GET` | `/api/transactions/summary` | Totaux, avec `from` et `to` optionnels |
| `POST` | `/api/transactions/recalculate` | Recalcul local ou exécution PL/SQL en mode Oracle |

Exemple :

```bash
curl -X POST http://localhost:5090/api/transactions \
  -H 'Content-Type: application/json' \
  -d '{"description":"Contrat conseil","amount":1250,"type":"Income","category":"Freelance","date":"2026-11-24"}'
```

## Passage aux bases d’entreprise

1. Exécuter `database/oracle/001_schema.sql`, puis `002_stored_procedure.sql` avec SQL*Plus ou SQLcl.
2. Installer le pilote Oracle (le repository ADO.NET et l’appel de procédure sont déjà implémentés) :

   ```bash
   dotnet add src/TransactFlow.Api package Oracle.ManagedDataAccess.Core \
     --source https://api.nuget.org/v3/index.json
   ```

3. Fournir les paramètres par variables d’environnement, sans stocker de secret dans Git :

   ```bash
   export Storage__Provider=Oracle
   export ConnectionStrings__Oracle='User Id=transactflow;Password=***;Data Source=localhost:1521/FREEPDB1'
   dotnet run --project src/TransactFlow.Api
   ```

   L’API sélectionne alors `OracleTransactionRepository`, et `/recalculate` appelle réellement `SP_RECALCULATE_TRANSACTION_SUMMARY`.
4. Exécuter `database/sqlserver/001_dw_schema.sql` sur SQL Server.
5. Configurer les connexions du projet `etl/ssis/TransactFlowETL` et le déployer dans `SSISDB`.
6. Importer `control-m/transactflow-jobs.json` avec Automation API, puis adapter l’agent et les variables d’environnement.
7. Créer les deux sources JDBC Denodo et exécuter `denodo/transactflow_views.vql`.

Le dépôt ne contient volontairement aucun mot de passe ni chaîne de connexion de production.

## Vérification

```bash
DOTNET_CLI_HOME="$PWD/.dotnet" dotnet restore TransactFlow.sln --configfile NuGet.Config
DOTNET_CLI_HOME="$PWD/.dotnet" dotnet build TransactFlow.sln --no-restore
DOTNET_CLI_HOME="$PWD/.dotnet" dotnet run --project tests/TransactFlow.DomainTests --no-build

cd src/TransactFlow.Web
npm ci
npm run build
```

## Structure

```text
src/                 API .NET et application Angular
tests/               tests métier sans dépendance externe
database/oracle/     schéma transactionnel et procédure PL/SQL
database/sqlserver/  Data Warehouse et chargement incrémental
etl/ssis/            projet et documentation SSIS
scripts/             démarrage local et export Linux
control-m/           définition du job planifié
denodo/              vue virtualisée VQL
```

