using Microsoft.Extensions.AI;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

var providers = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase);

var ollamaEndpoint = new Uri(
    builder.Configuration["Ollama:Endpoint"]
    ?? "http://localhost:11434/");

var ollamaModel =
    builder.Configuration["Ollama:Model"]
    ?? "qwen3.6:27b";

builder.Services.AddKeyedChatClient(
    "ollama",
    _ => new OllamaApiClient(
        ollamaEndpoint,
        ollamaModel));

providers.Add("ollama");

var openAiKey =
    builder.Configuration["OpenAI:ApiKey"];

if (!string.IsNullOrWhiteSpace(openAiKey))
{
    var openAiModel =
        builder.Configuration["OpenAI:Model"]
        ?? "gpt-4o-mini";

    builder.Services.AddKeyedChatClient(
        "openai",
        _ => new OpenAI.Chat.ChatClient(
                openAiModel,
                openAiKey)
            .AsIChatClient());

    providers.Add("openai");
}

var app = builder.Build();

app.MapGet("/", () =>
    Results.Ok(new
    {
        sample = "Provider-agnostic chat gateway",
        endpoints = new[]
        {
            "GET /providers",
            "POST /chat"
        }
    }));

app.MapGet("/providers", () =>
    Results.Ok(new
    {
        providers = providers.OrderBy(x => x)
    }));

app.MapPost(
    "/chat",
    async (
        ChatRequest request,
        IServiceProvider services,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest(new
            {
                error = "Message is required."
            });
        }

        var provider =
            string.IsNullOrWhiteSpace(request.Provider)
                ? "ollama"
                : request.Provider.Trim();

        if (!providers.Contains(provider))
        {
            return Results.BadRequest(new
            {
                error =
                    $"Unknown provider '{provider}'.",
                available =
                    providers.OrderBy(x => x)
            });
        }

        IChatClient client =
            services.GetRequiredKeyedService<IChatClient>(
                provider);

        ChatResponse response =
            await client.GetResponseAsync(
                [
                    new ChatMessage(
                        ChatRole.User,
                        request.Message)
                ],
                cancellationToken:
                    cancellationToken);

        return Results.Ok(new
        {
            provider,
            model = response.ModelId,
            content = response.Text,
            finishReason =
                response.FinishReason?.ToString(),
            inputTokens =
                response.Usage?.InputTokenCount,
            outputTokens =
                response.Usage?.OutputTokenCount
        });
    });

app.Run();

internal sealed record ChatRequest(
    string Message,
    string? Provider);