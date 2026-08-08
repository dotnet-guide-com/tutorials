using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetrySignalsMinimal.Models;
using OpenTelemetrySignalsMinimal.Telemetry;

const string serviceName =
    "DotNetGuide.ObservabilityLab";

const string serviceVersion =
    "1.0.0";

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CheckoutTelemetry>();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(
            serviceName: serviceName,
            serviceVersion: serviceVersion))
    .WithTracing(tracing =>
        tracing
            .AddSource(
                CheckoutTelemetry.ActivitySourceName)
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter())
    .WithMetrics(metrics =>
        metrics
            .AddMeter(
                CheckoutTelemetry.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter((_, readerOptions) =>
            {
                readerOptions
                    .PeriodicExportingMetricReaderOptions
                    .ExportIntervalMilliseconds = 1_000;
            }));

builder.Logging.ClearProviders();

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(
        ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion));

    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddConsoleExporter();
});

WebApplication app =
    builder.Build();

app.MapGet("/", () =>
    TypedResults.Ok(
        new SampleInfo(
            Sample: "correlated-opentelemetry-signals",
            Exporter: "console",
            ActivitySource: CheckoutTelemetry.ActivitySourceName,
            Meter: CheckoutTelemetry.MeterName)));

app.MapPost(
    "/checkout",
    (
        CheckoutRequest request,
        CheckoutTelemetry telemetry,
        ILogger<Program> logger) =>
        HandleCheckout(
            request,
            telemetry,
            logger));

app.Run();

static Results<Ok<CheckoutResponse>, BadRequest<ApiError>>
    HandleCheckout(
        CheckoutRequest request,
        CheckoutTelemetry telemetry,
        ILogger logger)
{
    string channel =
        request.Channel?
            .Trim()
            .ToLowerInvariant()
        ?? "";

    if (channel is not ("web" or "mobile"))
    {
        telemetry.Record(
            "unknown",
            "rejected",
            0);

        logger.CheckoutRejected(
            "invalid_channel");

        return TypedResults.BadRequest(
            new ApiError(
                "CHANNEL_INVALID",
                "Channel must be 'web' or 'mobile'."));
    }

    if (request.ItemCount is < 1 or > 10)
    {
        telemetry.Record(
            channel,
            "rejected",
            0);

        logger.CheckoutRejected(
            "invalid_item_count");

        return TypedResults.BadRequest(
            new ApiError(
                "ITEM_COUNT_INVALID",
                "ItemCount must be between 1 and 10."));
    }

    long started =
        Stopwatch.GetTimestamp();

    using Activity? activity =
        telemetry.StartCheckout(
            channel,
            request.ItemCount);

    activity?.AddEvent(
        new ActivityEvent(
            "Checkout validated"));

    logger.CheckoutAccepted(
        channel,
        request.ItemCount);

    double elapsedMilliseconds =
        Stopwatch
            .GetElapsedTime(started)
            .TotalMilliseconds;

    telemetry.Record(
        channel,
        "accepted",
        elapsedMilliseconds);

    activity?.SetTag(
        "checkout.outcome",
        "accepted");

    activity?.SetStatus(
        ActivityStatusCode.Ok);

    string traceId =
        activity?.TraceId.ToString()
        ?? Activity.Current?.TraceId.ToString()
        ?? "";

    return TypedResults.Ok(
        new CheckoutResponse(
            "accepted",
            channel,
            request.ItemCount,
            traceId));
}

public partial class Program;