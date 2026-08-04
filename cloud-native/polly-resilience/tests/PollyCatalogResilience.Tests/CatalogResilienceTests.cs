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

        Assert.False(
            response.Headers.Contains(
                "X-Resilience-Reason"));

        ResilienceStatus status =
            (await client.GetFromJsonAsync<
                ResilienceStatus>(
                    "/resilience/status",
                    cancellationToken:
                        ct))!;

        Assert.NotNull(
            status);

        Assert.Equal(
            0,
            status.Fallbacks);

        Assert.Equal(
            0,
            status.Timeouts);

        Assert.Equal(
            0,
            status.Rejections);

        Assert.Equal(
            "none",
            status.LastFallbackReason);
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
            "stale-cache",
            snapshot.Source);

        Assert.Equal(
            "timeout",
            snapshot.DegradedReason);

        Assert.Equal(
            "true",
            response.Headers
                .GetValues(
                    "X-Resilience-Fallback")
                .Single());

        Assert.Equal(
            "timeout",
            response.Headers
                .GetValues(
                    "X-Resilience-Reason")
                .Single());

        Assert.Equal(
            1,
            status.Timeouts);

        Assert.Equal(
            1,
            status.Fallbacks);

        Assert.Equal(
            0,
            status.Rejections);

        Assert.Equal(
            "timeout",
            status.LastFallbackReason);

        ResilienceStatus publicStatus =
            (await client.GetFromJsonAsync<
                ResilienceStatus>(
                    "/resilience/status",
                    cancellationToken:
                        ct))!;

        Assert.NotNull(
            publicStatus);

        Assert.Equal(
            status.Fallbacks,
            publicStatus.Fallbacks);

        Assert.Equal(
            status.Timeouts,
            publicStatus.Timeouts);

        Assert.Equal(
            status.Rejections,
            publicStatus.Rejections);

        Assert.Equal(
            status.LastFallbackReason,
            publicStatus.LastFallbackReason);
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

            Assert.Equal(
                "true",
                rejected.Headers
                    .GetValues(
                        "X-Resilience-Fallback")
                    .Single());

            Assert.Equal(
                "bulkhead-rejected",
                rejected.Headers
                    .GetValues(
                        "X-Resilience-Reason")
                    .Single());

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

            Assert.Equal(
                HttpStatusCode.OK,
                rejected.StatusCode);

            Assert.NotNull(
                fallback);

            Assert.True(
                fallback.IsStale);

            Assert.Equal(
                "stale-cache",
                fallback.Source);

            Assert.Equal(
                "bulkhead-rejected",
                fallback.DegradedReason);

            Assert.Equal(
                HttpStatusCode.OK,
                accepted.StatusCode);

            Assert.NotNull(
                live);

            Assert.Equal(
                "live-dependency",
                live.Source);

            Assert.False(
                live.IsStale);

            Assert.False(
                accepted.Headers.Contains(
                    "X-Resilience-Fallback"));

            ResilienceStatus status =
                (await client.GetFromJsonAsync<
                    ResilienceStatus>(
                        "/resilience/status",
                        cancellationToken:
                            ct))!;

            Assert.NotNull(
                status);

            Assert.Equal(
                1,
                status.Fallbacks);

            Assert.Equal(
                1,
                status.Rejections);

            Assert.Equal(
                0,
                status.Timeouts);

            Assert.Equal(
                "bulkhead-rejected",
                status.LastFallbackReason);

            ResilienceStatus inMemory =
                telemetry.Snapshot();

            Assert.Equal(
                status.Fallbacks,
                inMemory.Fallbacks);

            Assert.Equal(
                status.Timeouts,
                inMemory.Timeouts);

            Assert.Equal(
                status.Rejections,
                inMemory.Rejections);

            Assert.Equal(
                status.LastFallbackReason,
                inMemory.LastFallbackReason);
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

        ResilienceTelemetry telemetry =
            factory.Services
                .GetRequiredService<
                    ResilienceTelemetry>();

        ResilienceStatus beforeCancellation =
            telemetry.Snapshot();

        using var cts =
            new CancellationTokenSource();

        cts.Cancel();

        CatalogResilienceService service =
            factory.Services
                .GetRequiredService<
                    CatalogResilienceService>();

        await Assert.ThrowsAsync<
            OperationCanceledException>(
                async () =>
                    await service
                        .GetSnapshotAsync(
                            CatalogSimulationMode
                                .Slow,
                            1000,
                            cts.Token)
                        .AsTask());

        ResilienceStatus afterCancellation =
            telemetry.Snapshot();

        Assert.Equal(
            beforeCancellation.Fallbacks,
            afterCancellation.Fallbacks);

        Assert.Equal(
            beforeCancellation.Timeouts,
            afterCancellation.Timeouts);

        Assert.Equal(
            beforeCancellation.Rejections,
            afterCancellation.Rejections);

        Assert.Equal(
            beforeCancellation
                .LastFallbackReason,
            afterCancellation
                .LastFallbackReason);
    }
}