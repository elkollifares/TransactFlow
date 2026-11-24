# Pipeline SSIS

Le projet cible SQL Server 2019 et documente le flux à ouvrir dans Visual Studio avec l’extension **SQL Server Integration Services Projects**.

1. Créer `dbo.StageTransaction` à partir des colonnes de `transactions.csv`.
2. Ajouter un **Flat File Source** utilisant le paramètre `InputCsvPath`.
3. Ajouter un **OLE DB Destination** vers `dbo.StageTransaction` en mode *table lock*.
4. Ajouter un **Execute SQL Task** vers `database/sqlserver/002_load_stage.sql`.
5. Déployer le `.ispac` dans `SSISDB` et renseigner la connexion SQL Server dans l’environnement cible.

Le fichier `.dtsx` fourni est volontairement sans secret ni connexion liée à une machine. Le script Linux `scripts/export-transactions.sh` produit son entrée de manière reproductible.

