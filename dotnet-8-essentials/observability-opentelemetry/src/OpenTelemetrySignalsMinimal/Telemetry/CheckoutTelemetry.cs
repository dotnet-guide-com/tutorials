using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OpenTelemetrySignalsMinimal.Telemetry;

public sealed class CheckoutTelemetry : IDisposable
{
    public const string ActivitySourceName =
        "DotNetGuide.Observability.Checkout";

    public const string MeterName =
        "DotNetGuide.Observability.Checkout";

    private readonly ActivitySource _activitySource =
        new(ActivitySourceName, "1.0.0");

    private readonly Meter _meter =
        new(MeterName, "1.0.0");

    private readonly Counter<long> _requests;
    private readonly Histogram<double> _duration;

    public CheckoutTelemetry()
    {
        _requests = _meter.CreateCounter<long>(
            "checkout.requests",
            unit: "{request}",
            description: "Checkout requests processed by outcome.");

        _duration = _meter.CreateHistogram<double>(
            "checkout.duration",
            unit: "ms",
            description: "Checkout processing duration.");
    }

    public Activity? StartCheckout(
        string channel,
        int itemCount)
    {
        Activity? activity =
            _activitySource.StartActivity(
                "Checkout.Process",
                ActivityKind.Internal);

        activity?.SetTag(
            "checkout.channel",
            channel);

        activity?.SetTag(
            "checkout.item_count",
            itemCount);

        return activity;
    }

    public void Record(
        string channel,
        string outcome,
        double durationMilliseconds)
    {
        TagList tags = new()
        {
            { "checkout.channel", channel },
            { "checkout.outcome", outcome }
        };

        _requests.Add(1, tags);
        _duration.Record(
            durationMilliseconds,
            tags);
    }

    public void Dispose()
    {
        _activitySource.Dispose();
        _meter.Dispose();
    }
}