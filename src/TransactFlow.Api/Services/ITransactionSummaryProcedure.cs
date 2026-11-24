using TransactFlow.Api.Models;

namespace TransactFlow.Api.Services;

public interface ITransactionSummaryProcedure
{
    Task<TransactionSummary> ExecuteAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
}

public sealed class LocalTransactionSummaryProcedure(ITransactionRepository repository) : ITransactionSummaryProcedure
{
    public async Task<TransactionSummary> ExecuteAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default) =>
        TransactionCalculator.Calculate(
            await repository.GetAllAsync(cancellationToken),
            from,
            to,
            "local-procedure-simulation");
}

