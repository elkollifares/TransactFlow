using System.Text.Json;
using System.Text.Json.Serialization;
using TransactFlow.Api.Models;

namespace TransactFlow.Api.Services;

public sealed class JsonTransactionRepository : ITransactionRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonTransactionRepository(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:JsonPath"] ?? "data/transactions.json";
        _filePath = Path.GetFullPath(configuredPath, environment.ContentRootPath);
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await GetAllAsync(cancellationToken)).FirstOrDefault(item => item.Id == id);

    public async Task<Transaction> CreateAsync(TransactionRequest request, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var transactions = await ReadUnsafeAsync(cancellationToken);
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

            transactions.Add(transaction);
            await WriteUnsafeAsync(transactions, cancellationToken);
            return transaction;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Transaction?> UpdateAsync(Guid id, TransactionRequest request, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var transactions = await ReadUnsafeAsync(cancellationToken);
            var index = transactions.FindIndex(item => item.Id == id);
            if (index < 0) return null;

            var current = transactions[index];
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

            transactions[index] = updated;
            await WriteUnsafeAsync(transactions, cancellationToken);
            return updated;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var transactions = await ReadUnsafeAsync(cancellationToken);
            var removed = transactions.RemoveAll(item => item.Id == id) > 0;
            if (removed) await WriteUnsafeAsync(transactions, cancellationToken);
            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<Transaction>> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            var seed = SeedTransactions();
            await WriteUnsafeAsync(seed, cancellationToken);
            return seed;
        }

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<Transaction>>(stream, _jsonOptions, cancellationToken) ?? [];
    }

    private async Task WriteUnsafeAsync(List<Transaction> transactions, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temporaryPath = _filePath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, transactions, _jsonOptions, cancellationToken);
        }
        File.Move(temporaryPath, _filePath, true);
    }

    private static List<Transaction> SeedTransactions()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            NewSeed("Salaire mensuel", 5200m, TransactionType.Income, "Salaire", DateOnly.FromDateTime(DateTime.Today.AddDays(-18)), now),
            NewSeed("Loyer du bureau", 1450m, TransactionType.Expense, "Logement", DateOnly.FromDateTime(DateTime.Today.AddDays(-15)), now),
            NewSeed("Mission conseil", 2400m, TransactionType.Income, "Freelance", DateOnly.FromDateTime(DateTime.Today.AddDays(-11)), now),
            NewSeed("Abonnements logiciels", 286.40m, TransactionType.Expense, "Logiciels", DateOnly.FromDateTime(DateTime.Today.AddDays(-8)), now),
            NewSeed("Transport", 164.75m, TransactionType.Expense, "Transport", DateOnly.FromDateTime(DateTime.Today.AddDays(-4)), now),
            NewSeed("Épicerie", 218.30m, TransactionType.Expense, "Alimentation", DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), now)
        ];
    }

    private static Transaction NewSeed(string description, decimal amount, TransactionType type, string category, DateOnly date, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(), Description = description, Amount = amount, Type = type,
            Category = category, Date = date, CreatedAt = now, UpdatedAt = now
        };
}

