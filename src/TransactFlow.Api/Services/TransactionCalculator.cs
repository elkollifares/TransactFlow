using TransactFlow.Api.Models;

namespace TransactFlow.Api.Services;

public static class TransactionCalculator
{
    public static TransactionSummary Calculate(
        IEnumerable<Transaction> source,
        DateOnly? from = null,
        DateOnly? to = null,
        string engine = "dotnet")
    {
        var transactions = source
            .Where(item => (!from.HasValue || item.Date >= from.Value) && (!to.HasValue || item.Date <= to.Value))
            .ToArray();

        var income = transactions.Where(item => item.Type == TransactionType.Income).Sum(item => item.Amount);
        var expense = transactions.Where(item => item.Type == TransactionType.Expense).Sum(item => item.Amount);

        var categories = transactions
            .Where(item => item.Type == TransactionType.Expense)
            .GroupBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategoryTotal(group.Key, group.Sum(item => item.Amount)))
            .OrderByDescending(item => item.Amount)
            .ToArray();

        var trend = transactions
            .GroupBy(item => item.Date.ToString("yyyy-MM"))
            .OrderBy(group => group.Key)
            .Select(group => new MonthlyTotal(
                group.Key,
                group.Where(item => item.Type == TransactionType.Income).Sum(item => item.Amount),
                group.Where(item => item.Type == TransactionType.Expense).Sum(item => item.Amount)))
            .ToArray();

        return new TransactionSummary(
            income,
            expense,
            income - expense,
            transactions.Length,
            categories,
            trend,
            DateTimeOffset.UtcNow,
            engine);
    }
}

