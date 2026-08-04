using Polly;
using PollyCatalogResilience.Models;
using PollyCatalogResilience.Resilience;
using PollyCatalogResilience.Services;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    CatalogCache>();

builder.Services.AddSingleton<
    CatalogHoldGate>();

builder.Services.AddSingleton<
    CatalogDependency>();

builder.Services.AddSingleton<
    ResilienceTelemetry>();

builder.Services.AddSingleton<
    CatalogPipelineFactory>();

builder.Services.AddSingleton(
    services =>
        services
            .GetRequiredService<
                CatalogPipelineFactory>()
            .Create());

builder.Services.AddSingleton<
    CatalogResilienceService>();

var app =
    builder.Build();

app.MapGet(
    "/",
    () =>
        TypedResults.Ok(
            new
            {
                name =
                    "PollyCatalogResilience",

                pipeline =
                    new[]
                    {
                        "fallback",
                        "timeout",
                        "concurrency-limiter"
                    },

                endpoints =
                    new[]
                    {
                        "GET /api/catalog",
                        "GET /resilience/status"
                    },

                note =
                    "The catalog dependency and stale cache are deterministic in-process teaching components."
            }));

app.MapGet(
    "/api/catalog",
    GetCatalogAsync);

app.MapGet(
    "/resilience/status",
    (
        ResilienceTelemetry telemetry) =>
            TypedResults.Ok(
                telemetry.Snapshot()));

app.Run();

static async Task<IResult>
    GetCatalogAsync(
        string? mode,
        int? delayMilliseconds,
        HttpContext httpContext,
        CatalogResilienceService service,
        CancellationToken cancellationToken)
{
    if (!TryParseMode(
            mode,
            out CatalogSimulationMode
                simulationMode))
    {
        return Results.ValidationProblem(
            new Dictionary<
                string,
                string[]>
            {
                [nameof(mode)] =
                [
                    "Use live, failure, slow, or hold."
                ]
            });
    }

    int delay =
        delayMilliseconds
        ?? (
            simulationMode
            == CatalogSimulationMode.Slow
                ? 1_000
                : 0);

    if (delay is < 0 or > 5_000)
    {
        return Results.ValidationProblem(
            new Dictionary<
                string,
                string[]>
            {
                [nameof(
                    delayMilliseconds)] =
                [
                    "Use a value from 0 through 5000."
                ]
            });
    }

    CatalogSnapshot snapshot =
        await service.GetSnapshotAsync(
            simulationMode,
            delay,
            cancellationToken);

    if (snapshot.IsStale)
    {
        httpContext.Response.Headers
            .TryAdd(
                "X-Resilience-Fallback",
                "true");

        httpContext.Response.Headers
            .TryAdd(
                "X-Resilience-Reason",
                snapshot.DegradedReason);
    }

    return TypedResults.Ok(
        snapshot);
}

static bool TryParseMode(
    string? value,
    out CatalogSimulationMode mode)
{
    string normalized =
        string.IsNullOrWhiteSpace(
            value)
                ? "live"
                : value.Trim()
                    .ToLowerInvariant();

    mode =
        normalized switch
        {
            "live" =>
                CatalogSimulationMode.Live,

            "failure" =>
                CatalogSimulationMode.Failure,

            "slow" =>
                CatalogSimulationMode.Slow,

            "hold" =>
                CatalogSimulationMode.Hold,

            _ =>
                default
        };

    return normalized
        is "live"
        or "failure"
        or "slow"
        or "hold";
}

public partial class Program
{
}