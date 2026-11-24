using System.Text.Json.Serialization;
using TransactFlow.Api.Models;
using TransactFlow.Api.Services;

Console.WriteLine("TransactFlow API — initialisation…");
var packagedWebRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
{
    Args = args,
    ApplicationName = typeof(Program).Assembly.GetName().Name,
    ContentRootPath = Directory.GetCurrentDirectory(),
    EnvironmentName = Environments.Production,
    WebRootPath = Directory.Exists(packagedWebRoot) ? packagedWebRoot : null
});
builder.Configuration
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);
builder.WebHost
    .UseKestrel()
    .UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5090");
builder.Services.AddRouting();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var storageProvider = builder.Configuration["Storage:Provider"] ?? "Json";
if (storageProvider.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<OracleConnectionFactory>();
    builder.Services.AddSingleton<ITransactionRepository, OracleTransactionRepository>();
    builder.Services.AddSingleton<ITransactionSummaryProcedure, OracleTransactionSummaryProcedure>();
}
else
{
    builder.Services.AddSingleton<ITransactionRepository, JsonTransactionRepository>();
    builder.Services.AddSingleton<ITransactionSummaryProcedure, LocalTransactionSummaryProcedure>();
}
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
Console.WriteLine("TransactFlow API — configuration chargée.");
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    service = "TransactFlow.Api",
    storage = storageProvider,
    timestamp = DateTimeOffset.UtcNow
}));

var transactions = app.MapGroup("/api/transactions");

transactions.MapGet("/", async (
    ITransactionRepository repository,
    string? search,
    TransactionType? type,
    string? category,
    CancellationToken cancellationToken) =>
{
    var result = (await repository.GetAllAsync(cancellationToken)).AsEnumerable();
    if (!string.IsNullOrWhiteSpace(search))
        result = result.Where(item => item.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
    if (type.HasValue)
        result = result.Where(item => item.Type == type.Value);
    if (!string.IsNullOrWhiteSpace(category))
        result = result.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    return Results.Ok(result.OrderByDescending(item => item.Date).ThenByDescending(item => item.CreatedAt));
});

transactions.MapGet("/{id:guid}", async (Guid id, ITransactionRepository repository, CancellationToken cancellationToken) =>
    await repository.GetAsync(id, cancellationToken) is { } item ? Results.Ok(item) : Results.NotFound());

transactions.MapPost("/", async (TransactionRequest request, ITransactionRepository repository, CancellationToken cancellationToken) =>
{
    var validation = Validate(request);
    if (validation.Count > 0) return Results.ValidationProblem(validation);
    var created = await repository.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/transactions/{created.Id}", created);
});

transactions.MapPut("/{id:guid}", async (Guid id, TransactionRequest request, ITransactionRepository repository, CancellationToken cancellationToken) =>
{
    var validation = Validate(request);
    if (validation.Count > 0) return Results.ValidationProblem(validation);
    return await repository.UpdateAsync(id, request, cancellationToken) is { } updated
        ? Results.Ok(updated)
        : Results.NotFound();
});

transactions.MapDelete("/{id:guid}", async (Guid id, ITransactionRepository repository, CancellationToken cancellationToken) =>
    await repository.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());

transactions.MapGet("/summary", async (
    DateOnly? from,
    DateOnly? to,
    ITransactionRepository repository,
    CancellationToken cancellationToken) =>
{
    if (from.HasValue && to.HasValue && from > to)
        return Results.BadRequest(new { error = "La date de début doit précéder la date de fin." });
    return Results.Ok(TransactionCalculator.Calculate(await repository.GetAllAsync(cancellationToken), from, to));
});

transactions.MapPost("/recalculate", async (
    DateOnly? from,
    DateOnly? to,
    ITransactionSummaryProcedure procedure,
    CancellationToken cancellationToken) =>
{
    if (from.HasValue && to.HasValue && from > to)
        return Results.BadRequest(new { error = "La date de début doit précéder la date de fin." });
    return Results.Ok(await procedure.ExecuteAsync(from, to, cancellationToken));
});

app.MapFallbackToFile("index.html");

Console.WriteLine("TransactFlow API — démarrage du serveur HTTP.");
app.Run();

static Dictionary<string, string[]> Validate(TransactionRequest request)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 120)
        errors["description"] = ["La description est requise et ne peut pas dépasser 120 caractères."];
    if (request.Amount <= 0 || request.Amount > 999_999_999m)
        errors["amount"] = ["Le montant doit être supérieur à zéro."];
    if (string.IsNullOrWhiteSpace(request.Category) || request.Category.Trim().Length > 60)
        errors["category"] = ["La catégorie est requise et ne peut pas dépasser 60 caractères."];
    if (request.Date == default)
        errors["date"] = ["La date est requise."];
    if (request.Note?.Length > 500)
        errors["note"] = ["La note ne peut pas dépasser 500 caractères."];
    return errors;
}

public partial class Program;

