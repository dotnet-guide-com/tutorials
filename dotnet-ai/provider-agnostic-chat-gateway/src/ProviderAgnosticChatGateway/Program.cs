using System.Text.Json;
using Microsoft.Extensions.AI;
using ProviderAgnosticChatGateway.Conversations;
using ProviderAgnosticChatGateway.Gateway;
using ProviderAgnosticChatGateway.Providers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string defaultProvider =
    builder.Configuration["Gateway:DefaultProvider"] ?? "ollama";

bool allowProviderSelection =
    GetBoolean(
        builder.Configuration,
        "Gateway:AllowProviderSelection",
        defaultValue: true);

int maxMessages =
    GetInteger(
        builder.Configuration,
        "Gateway:MaxMessagesPerConversation",
        defaultValue: 20);

int maxInputCharacters =
    GetInteger(
        builder.Configuration,
        "Gateway:MaxInputCharacters",
        defaultValue: 8000);

ProviderRegistry registry =
    new(
        defaultProvider,
        allowProviderSelection);

RegisterConfiguredProviders(
    builder.Configuration,
    registry);

// Fail fast if the configured default provider is not enabled/registered.
_ = registry.Resolve();

ConversationStore conversationStore =
    new(maxMessages);

builder.Services.AddSingleton(_ => registry);
builder.Services.AddSingleton(_ => conversationStore);
builder.Services.AddSingleton(
    serviceProvider =>
        new ChatGateway(
            serviceProvider.GetRequiredService<ProviderRegistry>(),
            serviceProvider.GetRequiredService<ConversationStore>(),
            maxInputCharacters,
            serviceProvider.GetRequiredService<ILogger<ChatGateway>>()));

WebApplication app = builder.Build();

app.MapGet(
    "/",
    () => Results.Ok(
        new
        {
            sample = "ProviderAgnosticChatGateway",
            abstraction = nameof(IChatClient),
            endpoints = new[]
            {
                "GET /api/providers",
                "POST /api/chat",
                "POST /api/chat/stream",
                "GET /healthz"
            }
        }));

app.MapGet(
    "/healthz",
    () => Results.Ok(
        new
        {
            status = "ok"
        }));

app.MapGet(
    "/api/providers",
    (ProviderRegistry providers) =>
        Results.Ok(
            providers.Providers
                .OrderBy(p => p.Name)
                .Select(
                    p => new
                    {
                        name = p.Name,
                        isDefault = p.Name.Equals(
                            providers.DefaultProvider,
                            StringComparison.OrdinalIgnoreCase),
                        p.Capabilities
                    })));

app.MapPost(
    "/api/chat",
    HandleChatAsync);

app.MapPost(
    "/api/chat/stream",
    HandleStreamAsync);

app.Logger.LogInformation(
    "Registered AI providers: {Providers}. Default provider: {DefaultProvider}",
    string.Join(", ", registry.ProviderNames),
    registry.DefaultProvider);

app.Run();

static async Task<IResult> HandleChatAsync(
    ChatRequest request,
    ChatGateway gateway,
    CancellationToken cancellationToken)
{
    try
    {
        ChatResult result =
            await gateway.SendAsync(
                request,
                cancellationToken);

        return Results.Ok(result);
    }
    catch (Exception ex) when (IsClientError(ex))
    {
        return Results.BadRequest(
            new
            {
                error = ex.Message
            });
    }
}

static async Task HandleStreamAsync(
    ChatRequest request,
    ChatGateway gateway,
    HttpContext context,
    CancellationToken cancellationToken)
{
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers["Cache-Control"] = "no-cache";
    context.Response.Headers["X-Accel-Buffering"] = "no";

    try
    {
        await foreach (
            ChatStreamEvent item
            in gateway.StreamAsync(
                request,
                cancellationToken))
        {
            string payload = JsonSerializer.Serialize(item);
            await context.Response.WriteAsync(
                $"event: {item.Type}\ndata: {payload}\n\n",
                cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }
    catch (Exception ex) when (IsClientError(ex))
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                new { error = ex.Message },
                cancellationToken);
            return;
        }

        string payload = JsonSerializer.Serialize(
            new ChatStreamEvent(
                Type: "error",
                Text: ex.Message));

        await context.Response.WriteAsync(
            $"event: error\ndata: {payload}\n\n",
            cancellationToken);
    }
}

static bool IsClientError(Exception exception) =>
    exception is ArgumentException or
    KeyNotFoundException or
    InvalidOperationException;

static void RegisterConfiguredProviders(
    IConfiguration configuration,
    ProviderRegistry registry)
{
    RegisterOllama(configuration, registry);
    RegisterOpenAI(configuration, registry);
    RegisterOpenRouter(configuration, registry);
    RegisterAzureOpenAI(configuration, registry);

    if (registry.ProviderNames.Count == 0)
    {
        throw new InvalidOperationException(
            "No AI providers are enabled in configuration.");
    }
}

static void RegisterOllama(
    IConfiguration configuration,
    ProviderRegistry registry)
{
    const string root = "Providers:Ollama";
    if (!GetBoolean(configuration, $"{root}:Enabled", false))
    {
        return;
    }

    string endpoint =
        RequireConfiguration(configuration, $"{root}:Endpoint");

    string model =
        RequireConfiguration(configuration, $"{root}:Model");

    registry.Add(
        new ProviderDescriptor(
            "ollama",
            ProviderClientFactory.CreateOllama(endpoint, model),
            ReadCapabilities(configuration, root)));
}

static void RegisterOpenAI(
    IConfiguration configuration,
    ProviderRegistry registry)
{
    const string root = "Providers:OpenAI";
    if (!GetBoolean(configuration, $"{root}:Enabled", false))
    {
        return;
    }

    string model =
        RequireConfiguration(configuration, $"{root}:Model");

    string apiKey =
        RequireEnvironmentSecret(configuration, root);

    registry.Add(
        new ProviderDescriptor(
            "openai",
            ProviderClientFactory.CreateOpenAI(apiKey, model),
            ReadCapabilities(configuration, root)));
}

static void RegisterOpenRouter(
    IConfiguration configuration,
    ProviderRegistry registry)
{
    const string root = "Providers:OpenRouter";
    if (!GetBoolean(configuration, $"{root}:Enabled", false))
    {
        return;
    }

    string endpoint =
        RequireConfiguration(configuration, $"{root}:Endpoint");

    string model =
        RequireConfiguration(configuration, $"{root}:Model");

    string apiKey =
        RequireEnvironmentSecret(configuration, root);

    registry.Add(
        new ProviderDescriptor(
            "openrouter",
            ProviderClientFactory.CreateOpenAICompatible(
                endpoint,
                apiKey,
                model),
            ReadCapabilities(configuration, root)));
}

static void RegisterAzureOpenAI(
    IConfiguration configuration,
    ProviderRegistry registry)
{
    const string root = "Providers:AzureOpenAI";
    if (!GetBoolean(configuration, $"{root}:Enabled", false))
    {
        return;
    }

    string endpoint =
        RequireConfiguration(configuration, $"{root}:Endpoint");

    string deployment =
        RequireConfiguration(configuration, $"{root}:Deployment");

    bool useDefaultCredential =
        GetBoolean(
            configuration,
            $"{root}:UseDefaultAzureCredential",
            defaultValue: true);

    IChatClient client =
        useDefaultCredential
            ? ProviderClientFactory.CreateAzureOpenAIWithDefaultCredential(
                endpoint,
                deployment)
            : ProviderClientFactory.CreateAzureOpenAIWithApiKey(
                endpoint,
                RequireEnvironmentSecret(configuration, root),
                deployment);

    registry.Add(
        new ProviderDescriptor(
            "azure-openai",
            client,
            ReadCapabilities(configuration, root)));
}

static ProviderCapabilities ReadCapabilities(
    IConfiguration configuration,
    string providerRoot) =>
    new(
        Streaming: GetBoolean(
            configuration,
            $"{providerRoot}:Capabilities:Streaming",
            defaultValue: true),
        Tools: GetBoolean(
            configuration,
            $"{providerRoot}:Capabilities:Tools",
            defaultValue: false),
        ProviderManagedConversation: GetBoolean(
            configuration,
            $"{providerRoot}:Capabilities:ProviderManagedConversation",
            defaultValue: false));

static string RequireEnvironmentSecret(
    IConfiguration configuration,
    string providerRoot)
{
    string variableName =
        RequireConfiguration(
            configuration,
            $"{providerRoot}:ApiKeyEnvironmentVariable");

    string? value =
        Environment.GetEnvironmentVariable(variableName);

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Provider is enabled, but environment variable " +
            $"'{variableName}' is not set.");
    }

    return value;
}

static string RequireConfiguration(
    IConfiguration configuration,
    string key)
{
    string? value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Required configuration value '{key}' is missing.");
    }

    return value.Trim();
}

static bool GetBoolean(
    IConfiguration configuration,
    string key,
    bool defaultValue)
{
    string? value = configuration[key];
    return bool.TryParse(value, out bool parsed)
        ? parsed
        : defaultValue;
}

static int GetInteger(
    IConfiguration configuration,
    string key,
    int defaultValue)
{
    string? value = configuration[key];
    return int.TryParse(value, out int parsed)
        ? parsed
        : defaultValue;
}
