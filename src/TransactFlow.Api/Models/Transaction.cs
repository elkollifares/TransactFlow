namespace TransactFlow.Api.Models;

public enum TransactionType
{
    Income,
    Expense
}

public sealed record Transaction
{
    public Guid Id { get; init; }
    public required string Description { get; init; }
    public decimal Amount { get; init; }
    public TransactionType Type { get; init; }
    public required string Category { get; init; }
    public DateOnly Date { get; init; }
    public string? Note { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record TransactionRequest(
    string Description,
    decimal Amount,
    TransactionType Type,
    string Category,
    DateOnly Date,
    string? Note);

public sealed record CategoryTotal(string Category, decimal Amount);
public sealed record MonthlyTotal(string Month, decimal Income, decimal Expense);

public sealed record TransactionSummary(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    int TransactionCount,
    IReadOnlyList<CategoryTotal> ExpensesByCategory,
    IReadOnlyList<MonthlyTotal> MonthlyTrend,
    DateTimeOffset CalculatedAt,
    string CalculationEngine);

