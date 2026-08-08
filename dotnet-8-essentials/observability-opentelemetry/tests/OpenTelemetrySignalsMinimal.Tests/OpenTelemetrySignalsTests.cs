using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenTelemetrySignalsMinimal.Models;
using OpenTelemetrySignalsMinimal.Telemetry;
using Xunit;

namespace OpenTelemetrySignalsMinimal.Tests;

public sealed class OpenTelemetrySignalsTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenTelemetrySignalsTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Custom_activity_uses_stable_name_and_attributes()
    {
        Activity? captured = null;

        using ActivityListener listener = new()
        {
            ShouldListenTo = source =>
                source.Name == CheckoutTelemetry.ActivitySourceName,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                if (activity.OperationName == "Checkout.Process")
                    captured = activity;
            }
        };

        ActivitySource.AddActivityListener(listener);

        using ActivitySource source = new(
            CheckoutTelemetry.ActivitySourceName);

        using Activity? activity = source.StartActivity(
            "Checkout.Process",
            ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag("checkout.channel", "web");
            activity.SetTag("checkout.item_count", 2);
            activity.SetTag("checkout.outcome", "accepted");
            activity.SetStatus(ActivityStatusCode.Ok);
        }

        Assert.NotNull(captured);
        Assert.Equal("Checkout.Process", captured.DisplayName);
        Assert.Equal("web", captured.GetTagItem("checkout.channel"));
        Assert.Equal(2, captured.GetTagItem("checkout.item_count"));
        Assert.Equal("accepted", captured.GetTagItem("checkout.outcome"));
        Assert.Equal(ActivityStatusCode.Ok, captured.Status);
    }

    [Fact]
    public void Counter_uses_only_bounded_metric_tag_keys()
    {
        List<KeyValuePair<string, object?>> capturedTags = new();

        using MeterListener listener = new();
        listener.InstrumentPublished =
            (instrument, meterListener) =>
            {
                if (instrument.Meter.Name ==
                    CheckoutTelemetry.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };

        listener.SetMeasurementEventCallback<long>(
            (instrument, measurement, tags, state) =>
            {
                capturedTags.AddRange(tags.ToArray());
            });

        using Meter meter = new(
            CheckoutTelemetry.MeterName, "1.0.0");

        Counter<long> counter = meter.CreateCounter<long>(
            "checkout.requests");

        listener.Start();

        counter.Add(1,
            new("checkout.channel", "web"),
            new("checkout.outcome", "accepted"));

        Assert.NotEmpty(capturedTags);

        HashSet<string> keys = new(
            capturedTags.Select(kvp => kvp.Key));

        Assert.Equal(2, keys.Count);
        Assert.Contains("checkout.channel", keys);
        Assert.Contains("checkout.outcome", keys);

        foreach (string key in keys)
        {
            Assert.DoesNotContain("id", key,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Root_returns_fixed_sample_metadata()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        HttpClient client = _factory.CreateClient();

        SampleInfo? result =
            await client.GetFromJsonAsync<SampleInfo>(
                "/", ct);

        Assert.NotNull(result);
        Assert.Equal(
            "correlated-opentelemetry-signals",
            result.Sample);
        Assert.Equal("console", result.Exporter);
        Assert.Equal(
            CheckoutTelemetry.ActivitySourceName,
            result.ActivitySource);
        Assert.Equal(
            CheckoutTelemetry.MeterName,
            result.Meter);
    }

    [Fact]
    public async Task Checkout_returns_trace_id_for_valid_request()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/checkout",
                new CheckoutRequest(
                    Channel: "WEB",
                    ItemCount: 2),
                ct);

        CheckoutResponse? result =
            await response.Content
                .ReadFromJsonAsync<CheckoutResponse>(
                    cancellationToken: ct);

        Assert.NotNull(result);
        Assert.Equal("accepted", result.Status);
        Assert.Equal("web", result.Channel);
        Assert.Equal(2, result.ItemCount);
        Assert.Matches(
            "^[0-9a-fA-F]{32}$",
            result.TraceId);
    }

    [Fact]
    public async Task Checkout_rejects_missing_channel()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/checkout",
                new CheckoutRequest(
                    Channel: null,
                    ItemCount: 2),
                ct);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        ApiError? error =
            await response.Content
                .ReadFromJsonAsync<ApiError>(
                    cancellationToken: ct);

        Assert.NotNull(error);
        Assert.Equal("CHANNEL_INVALID", error.Code);
    }

    [Fact]
    public async Task Checkout_rejects_unsupported_channel()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/checkout",
                new CheckoutRequest(
                    Channel: "customer-92823",
                    ItemCount: 2),
                ct);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        ApiError? error =
            await response.Content
                .ReadFromJsonAsync<ApiError>(
                    cancellationToken: ct);

        Assert.NotNull(error);
        Assert.Equal("CHANNEL_INVALID", error.Code);
    }

    [Fact]
    public async Task Checkout_rejects_out_of_range_item_count()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/checkout",
                new CheckoutRequest(
                    Channel: "mobile",
                    ItemCount: 11),
                ct);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        ApiError? error =
            await response.Content
                .ReadFromJsonAsync<ApiError>(
                    cancellationToken: ct);

        Assert.NotNull(error);
        Assert.Equal("ITEM_COUNT_INVALID", error.Code);
    }

    [Fact]
    public async Task Unknown_route_returns_not_found()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync("/nonexistent", ct);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}