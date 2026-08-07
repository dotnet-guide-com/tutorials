using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.HttpResults;
using NativeAotEssentialsMinimal.Models;
using NativeAotEssentialsMinimal.Serialization;
using NativeAotEssentialsMinimal.Services;

WebApplicationBuilder builder =
    WebApplication.CreateSlimBuilder(
        args);

builder.Services
    .ConfigureHttpJsonOptions(
        options =>
        {
            options.SerializerOptions
                .TypeInfoResolverChain
                .Insert(
                    0,
                    AppJsonSerializerContext
                        .Default);
        });

builder.Services
    .AddSingleton<
        ITextTransformer,
        TextTransformer>();

WebApplication app =
    builder.Build();

app.MapGet(
    "/",
    static () =>
        TypedResults.Ok(
            new SampleInfo(
                Sample:
                    "native-aot-essentials",

                Builder:
                    "CreateSlimBuilder",

                Json:
                    "source-generated")));

app.MapGet(
    "/runtime",
    static () =>
        TypedResults.Ok(
            new RuntimeInfo(
                Framework:
                    RuntimeInformation
                        .FrameworkDescription,

                Architecture:
                    RuntimeInformation
                        .ProcessArchitecture
                        .ToString(),

                DynamicCodeSupported:
                    RuntimeFeature
                        .IsDynamicCodeSupported,

                DynamicCodeCompiled:
                    RuntimeFeature
                        .IsDynamicCodeCompiled)));

app.MapPost(
    "/echo",
    static (
        EchoRequest request,
        ITextTransformer transformer) =>
            HandleEcho(
                request,
                transformer));

app.Run();

static Results<
    Ok<EchoResponse>,
    BadRequest<ApiError>>
    HandleEcho(
        EchoRequest request,
        ITextTransformer transformer)
{
    if (string.IsNullOrWhiteSpace(
            request.Message))
    {
        return TypedResults.BadRequest(
            new ApiError(
                Code:
                    "MESSAGE_REQUIRED",

                Message:
                    "A non-empty message is required."));
    }

    string message =
        transformer.Normalize(
            request.Message);

    return TypedResults.Ok(
        new EchoResponse(
            Message:
                message,

            Length:
                message.Length));
}

public partial class Program;