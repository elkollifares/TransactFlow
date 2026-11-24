using System.Data;
using System.Data.Common;
using TransactFlow.Api.Models;

namespace TransactFlow.Api.Services;

public sealed class OracleTransactionRepository(OracleConnectionFactory connectionFactory) : ITransactionRepository
{
    private const string SelectColumns = """
        SELECT transaction_id, description, amount, transaction_type, category,
               transaction_date, note, created_at, updated_at
        FROM transactions
        """;

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = OracleConnectionFactory.CreateCommand(
            connection, $"{SelectColumns} ORDER BY transaction_date DESC, created_at DESC");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var transactions = new List<Transaction>();
        while (await reader.ReadAsync(cancellationToken)) transactions.Add(Map(reader));
        return transactions;
    }

    public async Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = OracleConnectionFactory.CreateCommand(
            connection, $"{SelectColumns} WHERE transaction_id = :id");
        OracleConnectionFactory.AddParameter(command, "id", id.ToByteArray(), DbType.Binary);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<Transaction> CreateAsync(TransactionRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Description = request.Description.Trim(),
            Amount = decimal.Round(request.Amount, 2),
            Type = request.Type,
            Category = request.Category.Trim(),
            Date = request.Date,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = OracleConnectionFactory.CreateCommand(connection, """
            INSERT INTO transactions (
                transaction_id, description, amount, transaction_type, category,
                transaction_date, note, created_at, updated_at
            ) VALUES (
                :id, :description, :amount, :type, :category,
                :transaction_date, :note, :created_at, :updated_at
            )
            """);
        AddTransactionParameters(command, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return transaction;
    }

    public async Task<Transaction?> UpdateAsync(Guid id, TransactionRequest request, CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(id, cancellationToken);
        if (current is null) return null;

        var updated = current with
        {
            Description = request.Description.Trim(),
            Amount = decimal.Round(request.Amount, 2),
            Type = request.Type,
            Category = request.Category.Trim(),
            Date = request.Date,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = OracleConnectionFactory.CreateCommand(connection, """
            UPDATE transactions SET
                description = :description,
                amount = :amount,
                transaction_type = :type,
                category = :category,
                transaction_date = :transaction_date,
                note = :note,
                updated_at = :updated_at
            WHERE transaction_id = :id
            """);
        OracleConnectionFactory.AddParameter(command, "description", updated.Description, DbType.String);
        OracleConnectionFactory.AddParameter(command, "amount", updated.Amount, DbType.Decimal);
        OracleConnectionFactory.AddParameter(command, "type", updated.Type.ToString(), DbType.String);
        OracleConnectionFactory.AddParameter(command, "category", updated.Category, DbType.String);
        OracleConnectionFactory.AddParameter(command, "transaction_date", updated.Date.ToDateTime(TimeOnly.MinValue), DbType.Date);
        OracleConnectionFactory.AddParameter(command, "note", updated.Note, DbType.String);
        OracleConnectionFactory.AddParameter(command, "updated_at", updated.UpdatedAt, DbType.DateTimeOffset);
        OracleConnectionFactory.AddParameter(command, "id", id.ToByteArray(), DbType.Binary);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0 ? updated : null;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = OracleConnectionFactory.CreateCommand(
            connection, "DELETE FROM transactions WHERE transaction_id = :id");
        OracleConnectionFactory.AddParameter(command, "id", id.ToByteArray(), DbType.Binary);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddTransactionParameters(DbCommand command, Transaction transaction)
    {
        OracleConnectionFactory.AddParameter(command, "id", transaction.Id.ToByteArray(), DbType.Binary);
        OracleConnectionFactory.AddParameter(command, "description", transaction.Description, DbType.String);
        OracleConnectionFactory.AddParameter(command, "amount", transaction.Amount, DbType.Decimal);
        OracleConnectionFactory.AddParameter(command, "type", transaction.Type.ToString(), DbType.String);
        OracleConnectionFactory.AddParameter(command, "category", transaction.Category, DbType.String);
        OracleConnectionFactory.AddParameter(command, "transaction_date", transaction.Date.ToDateTime(TimeOnly.MinValue), DbType.Date);
        OracleConnectionFactory.AddParameter(command, "note", transaction.Note, DbType.String);
        OracleConnectionFactory.AddParameter(command, "created_at", transaction.CreatedAt, DbType.DateTimeOffset);
        OracleConnectionFactory.AddParameter(command, "updated_at", transaction.UpdatedAt, DbType.DateTimeOffset);
    }

    private static Transaction Map(DbDataReader reader) => new()
    {
        Id = ReadGuid(reader.GetValue(0)),
        Description = reader.GetString(1),
        Amount = reader.GetDecimal(2),
        Type = Enum.Parse<TransactionType>(reader.GetString(3), ignoreCase: true),
        Category = reader.GetString(4),
        Date = DateOnly.FromDateTime(reader.GetDateTime(5)),
        Note = reader.IsDBNull(6) ? null : reader.GetString(6),
        CreatedAt = ReadTimestamp(reader.GetValue(7)),
        UpdatedAt = ReadTimestamp(reader.GetValue(8))
    };

    private static Guid ReadGuid(object value) => value switch
    {
        Guid guid => guid,
        byte[] bytes => new Guid(bytes),
        _ => Guid.Parse(Convert.ToString(value)!)
    };

    private static DateTimeOffset ReadTimestamp(object value) => value switch
    {
        DateTimeOffset timestamp => timestamp,
        DateTime dateTime => new DateTimeOffset(dateTime),
        _ => DateTimeOffset.Parse(Convert.ToString(value)!)
    };
}

