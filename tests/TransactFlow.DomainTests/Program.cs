using TransactFlow.Api.Models;
using TransactFlow.Api.Services;

var now = DateTimeOffset.UtcNow;
var transactions = new[]
{
    NewTransaction(1_000m, TransactionType.Income, "Salaire", new DateOnly(2026, 1, 10)),
    NewTransaction(250m, TransactionType.Expense, "Transport", new DateOnly(2026, 1, 12)),
    NewTransaction(100m, TransactionType.Expense, "Transport", new DateOnly(2026, 2, 2)),
    NewTransaction(50m, TransactionType.Expense, "Repas", new DateOnly(2025, 12, 20))
};

var summary = TransactionCalculator.Calculate(transactions, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
AssertEqual(1_000m, summary.TotalIncome, "total des revenus");
AssertEqual(350m, summary.TotalExpense, "total des dépenses");
AssertEqual(650m, summary.Balance, "solde");
AssertEqual(3, summary.TransactionCount, "nombre filtré");
AssertEqual(350m, summary.ExpensesByCategory.Single().Amount, "agrégation par catégorie");
AssertEqual(2, summary.MonthlyTrend.Count, "tendance mensuelle");

var procedure = new LocalTransactionSummaryProcedure(new FakeRepository(transactions));
var procedureSummary = await procedure.ExecuteAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
AssertEqual(650m, procedureSummary.Balance, "recalcul via procédure locale");
AssertEqual("local-procedure-simulation", procedureSummary.CalculationEngine, "moteur de calcul local");

Console.WriteLine("✓ 8 assertions métier réussies");
return;

Transaction NewTransaction(decimal amount, TransactionType type, string category, DateOnly date) => new()
{
    Id = Guid.NewGuid(), Description = "Test", Amount = amount, Type = type,
    Category = category, Date = date, CreatedAt = now, UpdatedAt = now
};

static void AssertEqual<T>(T expected, T actual, string label) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Échec ({label}) : attendu {expected}, obtenu {actual}");
}

sealed class FakeRepository(IReadOnlyList<Transaction> transactions) : ITransactionRepository
{
    public Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(transactions);

    public Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(transactions.FirstOrDefault(item => item.Id == id));

    public Task<Transaction> CreateAsync(TransactionRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Transaction?> UpdateAsync(Guid id, TransactionRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
