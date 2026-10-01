using HybridSearch.Api.Data;
using HybridSearch.Api.Models;
using HybridSearch.Api.Options;
using HybridSearch.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("SearchDb")
    ?? throw new InvalidOperationException("ConnectionStrings:SearchDb is required.");

builder.Services.Configure<EmbeddingOptions>(
    builder.Configuration.GetSection(EmbeddingOptions.SectionName));

builder.Services.AddDbContext<SearchDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(services =>
{
    EmbeddingOptions options = services
        .GetRequiredService<IOptions<EmbeddingOptions>>()
        .Value;

    if (!options.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "The live-verified runtime provider for this sample is Ollama. " +
            "See docs/provider-options.md for compile-verified cloud adapter examples.");
    }

    return EmbeddingProviderFactory.CreateOllama(options.Endpoint, options.Model);
});

builder.Services.AddSingleton<TextChunker>();
builder.Services.AddScoped<DocumentIngestionService>();
builder.Services.AddScoped<HybridSearchService>();

WebApplication app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services, connectionString);

app.MapGet("/", () => Results.Ok(new
{
    sample = "DOTNET GUIDE Hybrid Search",
    endpoints = new[] { "GET /health", "POST /api/documents", "POST /api/search" }
}));

app.MapGet("/health", async (
    SearchDbContext db,
    IOptions<EmbeddingOptions> embedding,
    CancellationToken cancellationToken) =>
{
    bool databaseOk = await db.Database.CanConnectAsync(cancellationToken);
    var payload = new
    {
        status = databaseOk ? "ok" : "degraded",
        database = databaseOk ? "reachable" : "unreachable",
        embeddingProvider = embedding.Value.Provider,
        embeddingModel = embedding.Value.Model,
        embeddingDimensions = EmbeddingDimensions.Value
    };

    return databaseOk
        ? Results.Ok(payload)
        : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapPost("/api/documents", async (
    IngestDocumentRequest request,
    DocumentIngestionService ingestion,
    CancellationToken cancellationToken) =>
{
    try
    {
        IngestDocumentResponse result = await ingestion.IngestAsync(
            request,
            cancellationToken);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/search", async (
    SearchRequest request,
    HybridSearchService search,
    CancellationToken cancellationToken) =>
{
    try
    {
        IReadOnlyList<SearchHit> result = await search.SearchAsync(
            request,
            cancellationToken);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

public partial class Program { }