using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MinimalApiPipeline.Tests;

public sealed class
    MinimalApiPipelineTests
{
    [Fact]
    public async Task
        V1_returns_the_flat_order_contract()
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
                "/api/v1/orders",
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        OrderV1[]? orders =
            await response.Content
                .ReadFromJsonAsync<
                    OrderV1[]>(
                    cancellationToken:
                        ct);

        Assert.NotNull(
            orders);

        Assert.Equal(
            3,
            orders.Length);

        Assert.Equal(
            2,
            orders[0].Items.Length);

        Assert.True(
            response.Headers.TryGetValues(
                "api-supported-versions",
                out IEnumerable<string>?
                    values));

        string supported =
            string.Join(
                ",",
                values);

        Assert.Contains(
            "1.0",
            supported,
            StringComparison.Ordinal);

        Assert.Contains(
            "2.0",
            supported,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task
        V2_returns_a_paginated_contract()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        PagedOrdersV2? result =
            await client.GetFromJsonAsync<
                PagedOrdersV2>(
                "/api/v2/orders?page=1&pageSize=2",
                ct);

        Assert.NotNull(
            result);

        Assert.Equal(
            1,
            result.Page);

        Assert.Equal(
            2,
            result.PageSize);

        Assert.Equal(
            3,
            result.TotalCount);

        Assert.Equal(
            2,
            result.Data.Length);
    }

    [Fact]
    public async Task
        Versions_return_different_detail_shapes()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        OrderV1? v1 =
            await client.GetFromJsonAsync<
                OrderV1>(
                "/api/v1/orders/1",
                ct);

        OrderV2? v2 =
            await client.GetFromJsonAsync<
                OrderV2>(
                "/api/v2/orders/1",
                ct);

        Assert.NotNull(
            v1);

        Assert.NotNull(
            v2);

        Assert.Equal(
            2,
            v1.Items.Length);

        Assert.Equal(
            2,
            v2.ItemCount);
    }

    [Fact]
    public async Task
        Valid_create_returns_created_and_location()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Client-Id",
            "create-test");

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/v1/orders",
                new CreateOrderRequest(
                    "customer-400",
                    [
                        "microphone"
                    ]),
                ct);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        OrderV1? created =
            await response.Content
                .ReadFromJsonAsync<
                    OrderV1>(
                    cancellationToken:
                        ct);

        Assert.NotNull(
            created);

        Assert.Equal(
            "customer-400",
            created.CustomerId);

        Assert.Equal(
            $"/api/v1/orders/{created.Id}",
            response.Headers.Location?
                .ToString());
    }

    [Fact]
    public async Task
        Invalid_create_returns_validation_problem()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Client-Id",
            "validation-test");

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/v1/orders",
                new CreateOrderRequest(
                    "",
                    []),
                ct);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await using Stream stream =
            await response.Content
                .ReadAsStreamAsync(
                    ct);

        using JsonDocument document =
            await JsonDocument.ParseAsync(
                stream,
                cancellationToken:
                    ct);

        JsonElement errors =
            document.RootElement
                .GetProperty(
                    "errors");

        Assert.True(
            errors.TryGetProperty(
                "CustomerId",
                out _));

        Assert.True(
            errors.TryGetProperty(
                "Items",
                out _));
    }

    [Fact]
    public async Task
        Timing_filter_adds_elapsed_header()
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
                "/api/v1/orders",
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues(
                "X-Endpoint-Elapsed-Ms",
                out IEnumerable<string>?
                    values));

        string elapsed =
            Assert.Single(
                values);

        Assert.True(
            long.TryParse(
                elapsed,
                out long milliseconds));

        Assert.True(
            milliseconds >= 0);
    }

    [Fact]
    public async Task
        Third_write_from_same_partition_is_rejected()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Client-Id",
            "limited-client");

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage first =
            await PostOrderAsync(
                client,
                "customer-a",
                ct);

        HttpResponseMessage second =
            await PostOrderAsync(
                client,
                "customer-b",
                ct);

        HttpResponseMessage third =
            await PostOrderAsync(
                client,
                "customer-c",
                ct);

        Assert.Equal(
            HttpStatusCode.Created,
            first.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            second.StatusCode);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            third.StatusCode);

        Assert.Equal(
            "application/problem+json",
            third.Content.Headers.ContentType?
                .MediaType);

        ProblemDetails? problem =
            await third.Content
                .ReadFromJsonAsync<
                    ProblemDetails>(
                    cancellationToken:
                        ct);

        Assert.NotNull(
            problem);

        Assert.Equal(
            429,
            problem.Status);

        Assert.Equal(
            "Rate limit exceeded",
            problem.Title);
    }

    [Fact]
    public async Task
        Rate_limit_partitions_are_independent()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient clientA =
            factory.CreateClient();

        using HttpClient clientB =
            factory.CreateClient();

        clientA.DefaultRequestHeaders.Add(
            "X-Client-Id",
            "client-a");

        clientB.DefaultRequestHeaders.Add(
            "X-Client-Id",
            "client-b");

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        await PostOrderAsync(
            clientA,
            "customer-a1",
            ct);

        await PostOrderAsync(
            clientA,
            "customer-a2",
            ct);

        HttpResponseMessage rejected =
            await PostOrderAsync(
                clientA,
                "customer-a3",
                ct);

        HttpResponseMessage independent =
            await PostOrderAsync(
                clientB,
                "customer-b1",
                ct);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            rejected.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            independent.StatusCode);
    }

    private static Task<
        HttpResponseMessage>
        PostOrderAsync(
            HttpClient client,
            string customerId,
            CancellationToken ct) =>
                client.PostAsJsonAsync(
                    "/api/v1/orders",
                    new CreateOrderRequest(
                        customerId,
                        [
                            "item"
                        ]),
                    ct);
}