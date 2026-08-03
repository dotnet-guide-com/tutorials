using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResilientOrdersMinimal.Hosting;
using ResilientOrdersMinimal.Payments;

namespace ResilientOrdersMinimal.Tests;

public sealed class
    HealthAndResilienceTests
{
    private static async Task<
        JsonDocument>
        ParseHealthBodyAsync(
            HttpResponseMessage response,
            CancellationToken ct)
    {
        string body =
            await response.Content
                .ReadAsStringAsync(
                    ct);

        return JsonDocument.Parse(
            body);
    }

    private static bool
        CheckExists(
            JsonElement checks,
            string name)
    {
        return checks
            .EnumerateArray()
            .Any(
                check =>
                    check.GetProperty(
                            "name")
                        .GetString()
                    == name);
    }

    [Fact]
    public async Task
        Root_describes_the_sample()
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
                "/",
                ct);

        string body =
            await response.Content
                .ReadAsStringAsync(
                    ct);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains(
            "ResilientOrdersMinimal",
            body,
            StringComparison.Ordinal);

        Assert.Contains(
            "/health/ready",
            body,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task
        Health_endpoints_are_healthy_and_structured()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage liveResponse =
            await client.GetAsync(
                "/health/live",
                ct);

        HttpResponseMessage readyResponse =
            await client.GetAsync(
                "/health/ready",
                ct);

        HttpResponseMessage combinedResponse =
            await client.GetAsync(
                "/health",
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            liveResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            readyResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            combinedResponse.StatusCode);

        Assert.Equal(
            "application/json",
            readyResponse.Content.Headers
                .ContentType?
                .MediaType);

        // -- /health/live: exactly one check, name is self
        using JsonDocument liveBody =
            await ParseHealthBodyAsync(
                liveResponse,
                ct);

        Assert.Equal(
            "Healthy",
            liveBody.RootElement
                .GetProperty("status")
                .GetString());

        JsonElement liveChecks =
            liveBody.RootElement
                .GetProperty("checks");

        Assert.Equal(
            1,
            liveChecks.GetArrayLength());

        Assert.True(
            CheckExists(
                liveChecks,
                "self"));

        Assert.False(
            CheckExists(
                liveChecks,
                "traffic-readiness"));

        // -- /health/ready: exactly one check, name is traffic-readiness
        using JsonDocument readyBody =
            await ParseHealthBodyAsync(
                readyResponse,
                ct);

        Assert.Equal(
            "Healthy",
            readyBody.RootElement
                .GetProperty("status")
                .GetString());

        JsonElement readyChecks =
            readyBody.RootElement
                .GetProperty("checks");

        Assert.Equal(
            1,
            readyChecks.GetArrayLength());

        Assert.True(
            CheckExists(
                readyChecks,
                "traffic-readiness"));

        Assert.False(
            CheckExists(
                readyChecks,
                "self"));

        // -- /health: exactly two checks, both present
        using JsonDocument combinedBody =
            await ParseHealthBodyAsync(
                combinedResponse,
                ct);

        Assert.Equal(
            "Healthy",
            combinedBody.RootElement
                .GetProperty("status")
                .GetString());

        JsonElement combinedChecks =
            combinedBody.RootElement
                .GetProperty("checks");

        Assert.Equal(
            2,
            combinedChecks.GetArrayLength());

        Assert.True(
            CheckExists(
                combinedChecks,
                "self"));

        Assert.True(
            CheckExists(
                combinedChecks,
                "traffic-readiness"));
    }

    [Fact]
    public async Task
        Shutdown_service_drains_readiness_but_not_liveness()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        // Confirm the concrete service resolved from DI is the same
        // instance registered as the hosted service.
        ShutdownReadinessService
            concreteService =
                factory.Services
                    .GetRequiredService<
                        ShutdownReadinessService>();

        IEnumerable<IHostedService>
            hostedServices =
                factory.Services
                    .GetRequiredService<
                        IEnumerable<
                            IHostedService>>();

        IHostedService
            shutdownHostedService =
                hostedServices
                    .OfType<
                        ShutdownReadinessService>()
                    .Single();

        Assert.Same(
            concreteService,
            shutdownHostedService);

        await concreteService.StopAsync(
            ct);

        // -- /health/live: 200, self remains Healthy
        HttpResponseMessage liveResponse =
            await client.GetAsync(
                "/health/live",
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            liveResponse.StatusCode);

        using JsonDocument liveBody =
            await ParseHealthBodyAsync(
                liveResponse,
                ct);

        Assert.Equal(
            "Healthy",
            liveBody.RootElement
                .GetProperty("status")
                .GetString());

        JsonElement liveChecks =
            liveBody.RootElement
                .GetProperty("checks");

        Assert.True(
            CheckExists(
                liveChecks,
                "self"));

        // -- /health/ready: 503, overall Unhealthy, traffic-readiness Unhealthy
        HttpResponseMessage readyResponse =
            await client.GetAsync(
                "/health/ready",
                ct);

        Assert.Equal(
            HttpStatusCode
                .ServiceUnavailable,
            readyResponse.StatusCode);

        using JsonDocument readyBody =
            await ParseHealthBodyAsync(
                readyResponse,
                ct);

        Assert.Equal(
            "Unhealthy",
            readyBody.RootElement
                .GetProperty("status")
                .GetString());

        JsonElement readyChecks =
            readyBody.RootElement
                .GetProperty("checks");

        Assert.True(
            CheckExists(
                readyChecks,
                "traffic-readiness"));

        Assert.Equal(
            "Unhealthy",
            readyChecks
                .EnumerateArray()
                .First(
                    c =>
                        c.GetProperty("name")
                            .GetString()
                        == "traffic-readiness")
                .GetProperty("status")
                .GetString());

        // -- /health: 503, overall Unhealthy, self Healthy, traffic-readiness Unhealthy
        HttpResponseMessage combinedResponse =
            await client.GetAsync(
                "/health",
                ct);

        Assert.Equal(
            HttpStatusCode
                .ServiceUnavailable,
            combinedResponse.StatusCode);

        using JsonDocument combinedBody =
            await ParseHealthBodyAsync(
                combinedResponse,
                ct);

        Assert.Equal(
            "Unhealthy",
            combinedBody.RootElement
                .GetProperty("status")
                .GetString());

        JsonElement combinedChecks =
            combinedBody.RootElement
                .GetProperty("checks");

        Assert.Equal(
            2,
            combinedChecks.GetArrayLength());

        Assert.Equal(
            "Healthy",
            combinedChecks
                .EnumerateArray()
                .First(
                    c =>
                        c.GetProperty("name")
                            .GetString()
                        == "self")
                .GetProperty("status")
                .GetString());

        Assert.Equal(
            "Unhealthy",
            combinedChecks
                .EnumerateArray()
                .First(
                    c =>
                        c.GetProperty("name")
                            .GetString()
                        == "traffic-readiness")
                .GetProperty("status")
                .GetString());
    }

    [Fact]
    public async Task
        Temporary_service_unavailability_is_retried()
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
            await client.PostAsync(
                "/api/orders/42/authorize?failuresBeforeSuccess=2&failureStatusCode=503&delayMilliseconds=0",
                content:
                    null,
                ct);

        PaymentAuthorizationResult? result =
            await response.Content
                .ReadFromJsonAsync<
                    PaymentAuthorizationResult>(
                        cancellationToken:
                            ct);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(
            result);

        Assert.True(
            result.Succeeded);

        Assert.False(
            result.CircuitOpen);

        Assert.Equal(
            3,
            result.Attempts);
    }

    [Fact]
    public async Task
        Bad_request_and_timeout_are_not_retried()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        // -- HTTP 400: non-retriable, one attempt
        HttpResponseMessage badRequestResponse =
            await client.PostAsync(
                "/api/orders/43/authorize?failuresBeforeSuccess=10&failureStatusCode=400&delayMilliseconds=0",
                content:
                    null,
                ct);

        PaymentAuthorizationResult? badRequestResult =
            await badRequestResponse.Content
                .ReadFromJsonAsync<
                    PaymentAuthorizationResult>(
                        cancellationToken:
                            ct);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            badRequestResponse.StatusCode);

        Assert.NotNull(
            badRequestResult);

        Assert.False(
            badRequestResult.Succeeded);

        Assert.False(
            badRequestResult.CircuitOpen);

        Assert.Equal(
            1,
            badRequestResult.Attempts);

        // -- Attempt timeout: delay exceeds 2-second timeout, one attempt, 504
        HttpResponseMessage timeoutResponse =
            await client.PostAsync(
                "/api/orders/45/authorize?failuresBeforeSuccess=0&failureStatusCode=503&delayMilliseconds=2500",
                content:
                    null,
                ct);

        PaymentAuthorizationResult? timeoutResult =
            await timeoutResponse.Content
                .ReadFromJsonAsync<
                    PaymentAuthorizationResult>(
                        cancellationToken:
                            ct);

        Assert.Equal(
            HttpStatusCode.GatewayTimeout,
            timeoutResponse.StatusCode);

        Assert.NotNull(
            timeoutResult);

        Assert.False(
            timeoutResult.Succeeded);

        Assert.False(
            timeoutResult.CircuitOpen);

        Assert.Equal(
            1,
            timeoutResult.Attempts);
    }

    [Fact]
    public async Task
        Repeated_service_unavailability_opens_the_circuit()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        string path =
            "/api/orders/44/authorize?failuresBeforeSuccess=20&failureStatusCode=503&delayMilliseconds=0";

        HttpResponseMessage firstResponse =
            await client.PostAsync(
                path,
                content:
                    null,
                ct);

        PaymentAuthorizationResult? first =
            await firstResponse.Content
                .ReadFromJsonAsync<
                    PaymentAuthorizationResult>(
                        cancellationToken:
                            ct);

        HttpResponseMessage secondResponse =
            await client.PostAsync(
                path,
                content:
                    null,
                ct);

        PaymentAuthorizationResult? second =
            await secondResponse.Content
                .ReadFromJsonAsync<
                    PaymentAuthorizationResult>(
                        cancellationToken:
                            ct);

        Assert.Equal(
            HttpStatusCode
                .ServiceUnavailable,
            firstResponse.StatusCode);

        Assert.NotNull(
            first);

        Assert.Equal(
            3,
            first.Attempts);

        Assert.False(
            first.CircuitOpen);

        Assert.Equal(
            HttpStatusCode
                .ServiceUnavailable,
            secondResponse.StatusCode);

        Assert.NotNull(
            second);

        Assert.True(
            second.CircuitOpen);

        Assert.Equal(
            0,
            second.Attempts);
    }
}