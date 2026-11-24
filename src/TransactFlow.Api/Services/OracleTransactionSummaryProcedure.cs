using System.Data;
using TransactFlow.Api.Models;

namespace TransactFlow.Api.Services;

public sealed class OracleTransactionSummaryProcedure(
    OracleConnectionFactory connectionFactory,
    ITransactionRepository repository) : ITransactionSummaryProcedure
{
    public async Task<TransactionSummary> ExecuteAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = OracleConnectionFactory.CreateCommand(
            connection,
            "sp_recalculate_transaction_summary",
            CommandType.StoredProcedure);

        OracleConnectionFactory.AddParameter(command, "p_from_date", from?.ToDateTime(TimeOnly.MinValue), DbType.Date);
        OracleConnectionFactory.AddParameter(command, "p_to_date", to?.ToDateTime(TimeOnly.MinValue), DbType.Date);
        var income = OracleConnectionFactory.AddParameter(command, "p_total_income", null, DbType.Decimal, ParameterDirection.Output);
        var expense = OracleConnectionFactory.AddParameter(command, "p_total_expense", null, DbType.Decimal, ParameterDirection.Output);
        var balance = OracleConnectionFactory.AddParameter(command, "p_balance", null, DbType.Decimal, ParameterDirection.Output);
        var count = OracleConnectionFactory.AddParameter(command, "p_transaction_count", null, DbType.Int32, ParameterDirection.Output);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var details = TransactionCalculator.Calculate(
            await repository.GetAllAsync(cancellationToken), from, to, "oracle-plsql");
        return details with
        {
            TotalIncome = Convert.ToDecimal(income.Value),
            TotalExpense = Convert.ToDecimal(expense.Value),
            Balance = Convert.ToDecimal(balance.Value),
            TransactionCount = Convert.ToInt32(count.Value),
            CalculationEngine = "oracle-plsql"
        };
    }
}

