using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PollyCatalogResilience.Models;
using PollyCatalogResilience.Resilience;
using PollyCatalogResilience.Services;

namespace PollyCatalogResilience.Tests;

public sealed class CatalogResilienceTests
{
    [Fact]
    public async Task
        Root_describes_the_pipeline()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        string body =
            await client.GetStringAsync(
                "/",
                ct);

        Assert.Contains(
            "PollyCatalogResilience",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "concurrency-limiter",
            body,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task
        Live_catalog_returns_fresh_data()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage response =
            await client.GetAsync(
                "/api/catalog",
                ct);

        CatalogSnapshot? snapshot =
            await response.Content
                .ReadFromJsonAsync<
                    CatalogSnapshot>(
                        cancellationToken:
                            ct);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(
            snapshot);

        Assert.False(
            snapshot.IsStale);

        Assert.Equal(
            "live-dependency",
            snapshot.Source);

        Assert.Equal(
            3,
            snapshot.Products.Length);

        Assert.False(
            response.Headers.Contains(
                "X-Resilience-Fallback"));
    }

    [Fact]
    public async Task
        Dependency_failure_uses_marked_stale_fallback()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage response =
            await client.GetAsync(
                "/api/catalog?mode=failure",
                ct);

        CatalogSnapshot? snapshot =
            await response.Content
                .ReadFromJsonAsync<
                    CatalogSnapshot>(
                        cancellationToken:
                            ct);

        ResilienceStatus status =
            factory.Services
                .GetRequiredService<
                    ResilienceTelemetry>()
                .Snapshot();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(
            snapshot);

        Assert.True(
            snapshot.IsStale);

        Assert.Equal(
            "dependency-failure",
            snapshot.DegradedReason);

        Assert.Equal(
            "true",
            response.Headers
                .GetValues(
                    "X-Resilience-Fallback")
                .Single());

        Assert.Equal(
            "dependency-failure",
            response.Headers
                .GetValues(
                    "X-Resilience-Reason")
                .Single());

        Assert.Equal(
            1,
            status.Fallbacks);

        Assert.Equal(
            0,
            status.Timeouts);

        Assert.Equal(
            0,
            status.Rejections);
    }

    [Fact]
    public async Task
        Slow_dependency_times_out_and_uses_fallback()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage response =
            await client.GetAsync(
                "/api/catalog?mode=slow&delayMilliseconds=1000",
                ct);

        CatalogSnapshot? snapshot =
            await response.Content
                .ReadFromJsonAsync<
                    CatalogSnapshot>(
                        cancellationToken:
                            ct);

        ResilienceStatus status =
            factory.Services
                .GetRequiredService<
                    ResilienceTelemetry>()
                .Snapshot();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(
            snapshot);

        Assert.True(
            snapshot.IsStale);

        Assert.Equal(
            "timeout",
            snapshot.DegradedReason);

        Assert.Equal(
            1,
            status.Timeouts);

        Assert.Equal(
            1,
            status.Fallbacks);

        Assert.Equal(
            "timeout",
            status.LastFallbackReason);
    }

    [Fact]
    public async Task
        Concurrent_operation_is_rejected_and_falls_back()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CatalogHoldGate gate =
            factory.Services
                .GetRequiredService<
                    CatalogHoldGate>();

        ResilienceTelemetry telemetry =
            factory.Services
                .GetRequiredService<
                    ResilienceTelemetry>();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        Task<HttpResponseMessage>
            heldRequest =
                client.GetAsync(
                    "/api/catalog?mode=hold",
                    ct);

        try
        {
            await gate.Entered
                .WaitAsync(
                    TimeSpan.FromSeconds(
                        5),
                    ct);

            HttpResponseMessage rejected =
                await client.GetAsync(
                    "/api/catalog?mode=live",
                    ct);

            CatalogSnapshot? fallback =
                await rejected.Content
                    .ReadFromJsonAsync<
                        CatalogSnapshot>(
                            cancellationToken:
                                ct);

            gate.Release();

            HttpResponseMessage accepted =
                await heldRequest;

            CatalogSnapshot? live =
                await accepted.Content
                    .ReadFromJsonAsync<
                        CatalogSnapshot>(
                            cancellationToken:
                                ct);

            ResilienceStatus status =
                telemetry.Snapshot();

            Assert.Equal(
                HttpStatusCode.OK,
                rejected.StatusCode);

            Assert.NotNull(
                fallback);

            Assert.True(
                fallback.IsStale);

            Assert.Equal(
                "bulkhead-rejected",
                fallback.DegradedReason);

            Assert.Equal(
                HttpStatusCode.OK,
                accepted.StatusCode);

            Assert.NotNull(
                live);

            Assert.False(
                live.IsStale);

            Assert.Equal(
                1,
                status.Rejections);

            Assert.Equal(
                1,
                status.Fallbacks);
        }
        finally
        {
            gate.Release();
        }
    }

    [Fact]
    public async Task
        Invalid_mode_and_delay_return_validation_errors()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage mode =
            await client.GetAsync(
                "/api/catalog?mode=unknown",
                ct);

        HttpResponseMessage delay =
            await client.GetAsync(
                "/api/catalog?mode=slow&delayMilliseconds=6000",
                ct);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            mode.StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            delay.StatusCode);
    }
}