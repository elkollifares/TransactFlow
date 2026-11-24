using System.Data;
using System.Data.Common;

namespace TransactFlow.Api.Services;

public sealed class OracleConnectionFactory
{
    private const string OracleConnectionType =
        "Oracle.ManagedDataAccess.Client.OracleConnection, Oracle.ManagedDataAccess";

    private readonly string _connectionString;

    public OracleConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Oracle")
            ?? throw new InvalidOperationException(
                "La chaîne ConnectionStrings:Oracle est requise lorsque Storage:Provider vaut Oracle.");
    }

    public DbConnection CreateConnection()
    {
        var connectionType = Type.GetType(OracleConnectionType, throwOnError: false);
        if (connectionType is null)
        {
            throw new InvalidOperationException(
                "Le pilote Oracle est absent. Installez Oracle.ManagedDataAccess.Core dans le projet API.");
        }

        return Activator.CreateInstance(connectionType, _connectionString) as DbConnection
            ?? throw new InvalidOperationException("Impossible de créer la connexion Oracle.");
    }

    public static DbCommand CreateCommand(DbConnection connection, string commandText, CommandType commandType = CommandType.Text)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandType = commandType;
        command.CommandTimeout = 30;

        // Oracle lie les paramètres par position par défaut. BindByName évite les erreurs
        // lorsque l'ordre des paramètres évolue.
        command.GetType().GetProperty("BindByName")?.SetValue(command, true);
        return command;
    }

    public static DbParameter AddParameter(
        DbCommand command,
        string name,
        object? value,
        DbType? dbType = null,
        ParameterDirection direction = ParameterDirection.Input)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        parameter.Direction = direction;
        if (dbType.HasValue) parameter.DbType = dbType.Value;
        command.Parameters.Add(parameter);
        return parameter;
    }
}

