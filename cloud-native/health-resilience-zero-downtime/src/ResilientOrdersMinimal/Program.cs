using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using ResilientOrdersMinimal.Health;
using ResilientOrdersMinimal.Hosting;
using ResilientOrdersMinimal.Payments;

var builder =
    WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(
    options =>
    {
        options.ShutdownTimeout =
            TimeSpan.FromSeconds(
                30);
    });

builder.Services.AddSingleton<
    TrafficReadinessState>();

builder.Services.AddSingleton<
    PaymentSimulationState>();

builder.Services.AddSingleton<
    ShutdownReadinessService>();

builder.Services.AddHostedService(
    services =>
        services.GetRequiredService<
            ShutdownReadinessService>());

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () =>
            HealthCheckResult.Healthy(
                "The process is responding."),
        tags:
        [
            "live"
        ])
    .AddCheck<
        TrafficReadinessHealthCheck>(
        "traffic-readiness",
        tags:
        [
            "ready"
        ]);

builder.Services
    .AddHttpClient<
        PaymentGatewayClient>(
        client =>
        {
            client.BaseAddress =
                new Uri(
                    "https://payment.example.invalid");
        })
    .ConfigurePrimaryHttpMessageHandler(
        services =>
            new SimulatedPaymentHandler(
                services
                    .GetRequiredService<
                        PaymentSimulationState>()))
    .AddResilienceHandler(
        "payment-pipeline",
        resilience =>
        {
            resilience
                .AddRetry(
                    new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts =
                            2,

                        Delay =
                            TimeSpan
                                .FromMilliseconds(
                                    20),

                        BackoffType =
                            DelayBackoffType
                                .Exponential,

                        UseJitter =
                            true,

                        ShouldHandle =
                            static arguments =>
                                ValueTask
                                    .FromResult(
                                        arguments
                                            .Outcome
                                            .Result?
                                            .StatusCode
                                        ==
                                        HttpStatusCode
                                            .ServiceUnavailable)
                    })
                .AddCircuitBreaker(
                    new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio =
                            1.0,

                        MinimumThroughput =
                            3,

                        SamplingDuration =
                            TimeSpan
                                .FromSeconds(
                                    30),

                        BreakDuration =
                            TimeSpan
                                .FromSeconds(
                                    10),

                        ShouldHandle =
                            static arguments =>
                                ValueTask
                                    .FromResult(
                                        arguments
                                            .Outcome
                                            .Result?
                                            .StatusCode
                                        ==
                                        HttpStatusCode
                                            .ServiceUnavailable)
                    })
                .AddTimeout(
                    TimeSpan.FromSeconds(
                        2));
        });

var app =
    builder.Build();

app.MapGet(
    "/",
    () =>
        TypedResults.Ok(
            new
            {
                name =
                    "ResilientOrdersMinimal",

                endpoints =
                    new[]
                    {
                        "GET /health/live",
                        "GET /health/ready",
                        "GET /health",
                        "POST /api/orders/{id}/authorize"
                    },

                note =
                    "The payment dependency is simulated in process for deterministic resilience testing."
            }));

app.MapPost(
    "/api/orders/{id:int:min(1)}/authorize",
    AuthorizeOrderAsync);

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate =
            registration =>
                registration.Tags
                    .Contains(
                        "live"),

        ResponseWriter =
            HealthResponseWriter
                .WriteAsync
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate =
            registration =>
                registration.Tags
                    .Contains(
                        "ready"),

        ResponseWriter =
            HealthResponseWriter
                .WriteAsync
    });

app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        ResponseWriter =
            HealthResponseWriter
                .WriteAsync
    });

app.Run();

static async Task<IResult>
    AuthorizeOrderAsync(
        int id,
        int failuresBeforeSuccess,
        int failureStatusCode,
        PaymentGatewayClient client,
        CancellationToken cancellationToken)
{
    if (failuresBeforeSuccess
        is < 0
        or > 20)
    {
        return Results.ValidationProblem(
            new Dictionary<
                string,
                string[]>
            {
                [nameof(
                    failuresBeforeSuccess)] =
                [
                    "Use a value from 0 through 20."
                ]
            });
    }

    if (failureStatusCode
        is not 400
        and not 503)
    {
        return Results.ValidationProblem(
            new Dictionary<
                string,
                string[]>
            {
                [nameof(
                    failureStatusCode)] =
                [
                    "Use 400 for a non-retriable failure or 503 for a retriable failure."
                ]
            });
    }

    PaymentAuthorizationResult result =
        await client.AuthorizeAsync(
            id,
            failuresBeforeSuccess,
            (HttpStatusCode)
                failureStatusCode,
            cancellationToken);

    int responseStatus =
        result.Succeeded
            ? StatusCodes.Status200OK
            : result.CircuitOpen
                ? StatusCodes
                    .Status503ServiceUnavailable
                : result.StatusCode;

    return Results.Json(
        result,
        statusCode:
            responseStatus);
}

public partial class Program
{
}