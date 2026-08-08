namespace OpenTelemetrySignalsMinimal.Models;

public sealed record SampleInfo(
    string Sample,
    string Exporter,
    string ActivitySource,
    string Meter);

public sealed record CheckoutRequest(
    string? Channel,
    int ItemCount);

public sealed record CheckoutResponse(
    string Status,
    string Channel,
    int ItemCount,
    string TraceId);

public sealed record ApiError(
    string Code,
    string Message);