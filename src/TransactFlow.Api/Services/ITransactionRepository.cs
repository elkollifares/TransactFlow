using TransactFlow.Api.Models;

namespace TransactFlow.Api.Services;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transaction> CreateAsync(TransactionRequest request, CancellationToken cancellationToken = default);
    Task<Transaction?> UpdateAsync(Guid id, TransactionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

