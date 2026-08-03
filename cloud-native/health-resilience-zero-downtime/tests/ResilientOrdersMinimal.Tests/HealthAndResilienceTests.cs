using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ResilientOrdersMinimal.Hosting;
using ResilientOrdersMinimal.Payments;

namespace ResilientOrdersMinimal.Tests;

public sealed class
    HealthAndResilienceTests
{
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

        HttpResponseMessage live =
            await client.GetAsync(
                "/health/live",
                ct);

        HttpResponseMessage ready =
            await client.GetAsync(
                "/health/ready",
                ct);

        HttpResponseMessage combined =
            await client.GetAsync(
                "/health",
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            live.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            ready.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            combined.StatusCode);

        Assert.Equal(
            "application/json",
            ready.Content.Headers
                .ContentType?
                .MediaType);

        using JsonDocument body =
            JsonDocument.Parse(
                await ready.Content
                    .ReadAsStringAsync(
                        ct));

        Assert.Equal(
            "Healthy",
            body.RootElement
                .GetProperty(
                    "status")
                .GetString());

        JsonElement checks =
            body.RootElement
                .GetProperty(
                    "checks");

        Assert.Contains(
            checks.EnumerateArray(),
            check =>
                check.GetProperty(
                        "name")
                    .GetString()
                ==
                "traffic-readiness");
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

        ShutdownReadinessService service =
            factory.Services
                .GetRequiredService<
                    ShutdownReadinessService>();

        await service.StopAsync(
            ct);

        HttpResponseMessage live =
            await client.GetAsync(
                "/health/live",
                ct);

        HttpResponseMessage ready =
            await client.GetAsync(
                "/health/ready",
                ct);

        HttpResponseMessage combined =
            await client.GetAsync(
                "/health",
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            live.StatusCode);

        Assert.Equal(
            HttpStatusCode
                .ServiceUnavailable,
            ready.StatusCode);

        Assert.Equal(
            HttpStatusCode
                .ServiceUnavailable,
            combined.StatusCode);
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
                "/api/orders/42/authorize?failuresBeforeSuccess=2&failureStatusCode=503",
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
        Bad_request_is_not_retried()
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
                "/api/orders/43/authorize?failuresBeforeSuccess=10&failureStatusCode=400",
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
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.NotNull(
            result);

        Assert.False(
            result.Succeeded);

        Assert.False(
            result.CircuitOpen);

        Assert.Equal(
            1,
            result.Attempts);
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
            "/api/orders/44/authorize?failuresBeforeSuccess=20&failureStatusCode=503";

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