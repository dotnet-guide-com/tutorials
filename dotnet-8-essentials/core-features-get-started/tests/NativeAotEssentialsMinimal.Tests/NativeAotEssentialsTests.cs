using System.Net;
using System.Net.Http.Json;
using NativeAotEssentialsMinimal.Models;
using NativeAotEssentialsMinimal.Serialization;
using NativeAotEssentialsMinimal.Services;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NativeAotEssentialsMinimal.Tests;

public sealed class NativeAotEssentialsTests
{
    [Fact]
    public void
        Json_context_contains_all_api_types()
    {
        Assert.NotNull(
            AppJsonSerializerContext
                .Default
                .SampleInfo);

        Assert.NotNull(
            AppJsonSerializerContext
                .Default
                .RuntimeInfo);

        Assert.NotNull(
            AppJsonSerializerContext
                .Default
                .EchoRequest);

        Assert.NotNull(
            AppJsonSerializerContext
                .Default
                .EchoResponse);

        Assert.NotNull(
            AppJsonSerializerContext
                .Default
                .ApiError);
    }

    [Fact]
    public void
        Transformer_normalizes_spacing_and_case()
    {
        var transformer =
            new TextTransformer();

        string result =
            transformer.Normalize(
                "  hello   native aot  ");

        Assert.Equal(
            "HELLO NATIVE AOT",
            result);
    }

    [Fact]
    public void
        Transformer_rejects_null()
    {
        var transformer =
            new TextTransformer();

        Assert.Throws<
            ArgumentNullException>(
                () =>
                    transformer.Normalize(
                        null!));
    }

    [Fact]
    public async Task
        Root_returns_fixed_sample_metadata()
    {
        await using var factory =
            new WebApplicationFactory<
                Program>();

        HttpClient client =
            factory.CreateClient();

        SampleInfo? response =
            await client
                .GetFromJsonAsync<
                    SampleInfo>(
                        "/",
                        TestContext.Current
                            .CancellationToken);

        Assert.NotNull(
            response);

        Assert.Equal(
            "native-aot-essentials",
            response.Sample);

        Assert.Equal(
            "CreateSlimBuilder",
            response.Builder);

        Assert.Equal(
            "source-generated",
            response.Json);
    }

    [Fact]
    public async Task
        Runtime_endpoint_returns_process_metadata()
    {
        await using var factory =
            new WebApplicationFactory<
                Program>();

        HttpClient client =
            factory.CreateClient();

        RuntimeInfo? response =
            await client
                .GetFromJsonAsync<
                    RuntimeInfo>(
                        "/runtime",
                        TestContext.Current
                            .CancellationToken);

        Assert.NotNull(
            response);

        Assert.NotEmpty(
            response.Framework);

        Assert.NotEmpty(
            response.Architecture);
    }

    [Fact]
    public async Task
        Echo_returns_normalized_typed_response()
    {
        await using var factory =
            new WebApplicationFactory<
                Program>();

        HttpClient client =
            factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/echo",
                new EchoRequest(
                    "  hello   native aot  "),
                TestContext.Current
                    .CancellationToken);

        response
            .EnsureSuccessStatusCode();

        EchoResponse? payload =
            await response.Content
                .ReadFromJsonAsync<
                    EchoResponse>(
                        TestContext.Current
                            .CancellationToken);

        Assert.NotNull(
            payload);

        Assert.Equal(
            "HELLO NATIVE AOT",
            payload.Message);

        Assert.Equal(
            16,
            payload.Length);
    }

    [Fact]
    public async Task
        Echo_rejects_blank_message()
    {
        await using var factory =
            new WebApplicationFactory<
                Program>();

        HttpClient client =
            factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/echo",
                new EchoRequest(
                    "   "),
                TestContext.Current
                    .CancellationToken);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        ApiError? error =
            await response.Content
                .ReadFromJsonAsync<
                    ApiError>(
                        TestContext.Current
                            .CancellationToken);

        Assert.NotNull(
            error);

        Assert.Equal(
            "MESSAGE_REQUIRED",
            error.Code);

        Assert.Equal(
            "A non-empty message is required.",
            error.Message);
    }

    [Fact]
    public async Task
        Unknown_route_returns_not_found()
    {
        await using var factory =
            new WebApplicationFactory<
                Program>();

        HttpClient client =
            factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync(
                "/this-route-does-not-exist",
                TestContext.Current
                    .CancellationToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}