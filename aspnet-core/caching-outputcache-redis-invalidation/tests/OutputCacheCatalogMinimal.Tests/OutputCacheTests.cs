using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OutputCacheCatalogMinimal.Tests;

public sealed class OutputCacheTests
{
    [Fact]
    public async Task
        Identical_list_request_uses_cached_response()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        ProductListResponse? first =
            await client.GetFromJsonAsync<
                ProductListResponse>(
                "/products?sort=name",
                ct);

        ProductListResponse? second =
            await client.GetFromJsonAsync<
                ProductListResponse>(
                "/products?sort=name",
                ct);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(
            1,
            first.OriginExecution);
        Assert.Equal(
            1,
            second.OriginExecution);
        Assert.Equal(
            first.Items,
            second.Items);
    }

    [Fact]
    public async Task
        Relevant_query_values_create_distinct_entries()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        ProductListResponse? categoryOne =
            await client.GetFromJsonAsync<
                ProductListResponse>(
                "/products?category=1&sort=name",
                ct);

        ProductListResponse? categoryTwo =
            await client.GetFromJsonAsync<
                ProductListResponse>(
                "/products?category=2&sort=name",
                ct);

        ProductListResponse? categoryOneAgain =
            await client.GetFromJsonAsync<
                ProductListResponse>(
                "/products?category=1&sort=name",
                ct);

        Assert.NotNull(categoryOne);
        Assert.NotNull(categoryTwo);
        Assert.NotNull(categoryOneAgain);

        Assert.Equal(
            1,
            categoryOne.OriginExecution);

        Assert.Equal(
            2,
            categoryTwo.OriginExecution);

        Assert.Equal(
            1,
            categoryOneAgain.OriginExecution);
    }

    [Fact]
    public async Task
        Unrelated_query_value_does_not_create_new_entry()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        ProductListResponse? first =
            await client.GetFromJsonAsync<
                ProductListResponse>(
                "/products?category=1&sort=name&utm_source=first",
                ct);

        ProductListResponse? second =
            await client.GetFromJsonAsync<
                ProductListResponse>(
                "/products?category=1&sort=name&utm_source=second",
                ct);

        Assert.NotNull(first);
        Assert.NotNull(second);

        Assert.Equal(
            1,
            first.OriginExecution);

        Assert.Equal(
            1,
            second.OriginExecution);
    }

    [Fact]
    public async Task
        Route_ids_create_distinct_detail_entries()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        ProductDetailResponse? first =
            await client.GetFromJsonAsync<
                ProductDetailResponse>(
                "/products/1",
                ct);

        ProductDetailResponse? firstAgain =
            await client.GetFromJsonAsync<
                ProductDetailResponse>(
                "/products/1",
                ct);

        ProductDetailResponse? second =
            await client.GetFromJsonAsync<
                ProductDetailResponse>(
                "/products/2",
                ct);

        Assert.NotNull(first);
        Assert.NotNull(firstAgain);
        Assert.NotNull(second);

        Assert.Equal(
            1,
            first.OriginExecution);

        Assert.Equal(
            1,
            firstAgain.OriginExecution);

        Assert.Equal(
            2,
            second.OriginExecution);
    }

    [Fact]
    public async Task
        Create_evicts_cached_product_lists()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        ProductListResponse? before =
            await client.GetFromJsonAsync<
                ProductListResponse>(
                "/products?sort=name",
                ct);

        HttpResponseMessage createResponse =
            await client.PostAsJsonAsync(
                "/products",
                new CreateProductRequest(
                    "Monitor Arm",
                    79.00m,
                    2),
                ct);

        ProductListResponse? after =
            await client.GetFromJsonAsync<
                ProductListResponse>(
                "/products?sort=name",
                ct);

        Assert.NotNull(before);
        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        Assert.NotNull(after);
        Assert.Equal(
            1,
            before.OriginExecution);
        Assert.Equal(
            2,
            after.OriginExecution);

        Assert.Contains(
            after.Items,
            item =>
                item.Name ==
                "Monitor Arm");
    }

    [Fact]
    public async Task
        Update_evicts_cached_product_detail()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        ProductDetailResponse? before =
            await client.GetFromJsonAsync<
                ProductDetailResponse>(
                "/products/1",
                ct);

        HttpResponseMessage updateResponse =
            await client.PutAsJsonAsync(
                "/products/1",
                new UpdateProductRequest(
                    "Low-Profile Keyboard",
                    99.00m,
                    1),
                ct);

        ProductDetailResponse? after =
            await client.GetFromJsonAsync<
                ProductDetailResponse>(
                "/products/1",
                ct);

        Assert.NotNull(before);
        Assert.Equal(
            HttpStatusCode.NoContent,
            updateResponse.StatusCode);

        Assert.NotNull(after);
        Assert.Equal(
            1,
            before.OriginExecution);
        Assert.Equal(
            2,
            after.OriginExecution);
        Assert.Equal(
            "Low-Profile Keyboard",
            after.Item.Name);
    }

    [Fact]
    public async Task
        Delete_evicts_cached_product_detail()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        ProductDetailResponse? before =
            await client.GetFromJsonAsync<
                ProductDetailResponse>(
                "/products/2",
                ct);

        HttpResponseMessage deleteResponse =
            await client.DeleteAsync(
                "/products/2",
                ct);

        HttpResponseMessage after =
            await client.GetAsync(
                "/products/2",
                ct);

        Assert.NotNull(before);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            after.StatusCode);
    }

    [Fact]
    public async Task
        Invalid_product_is_rejected()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/products",
                new CreateProductRequest(
                    " ",
                    0,
                    0),
                ct);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}