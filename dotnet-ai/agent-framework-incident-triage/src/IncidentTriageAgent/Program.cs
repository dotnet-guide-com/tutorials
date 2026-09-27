using System.ClientModel;
using IncidentTriageAgent.Agents;
using IncidentTriageAgent.Evidence;
using IncidentTriageAgent.Guidance;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

const string DefaultModel =
    "openai/gpt-4o-mini";

const string DefaultIncident =
    """
    Investigate the checkout-api incident.

    Using only the available operational evidence and reference guidance:

    1. state what the observed evidence establishes,
    2. identify reasonable inferences,
    3. list the relevant runbook guidance,
    4. identify unknown information,
    5. and state which root-cause claims remain unproven.

    Do not invent missing facts.
    Do not treat correlation as proof of causation.
    """;

const string DefaultFollowUp =
    """
    Can I truthfully write in the incident report:

    "deploy-1842 caused the checkout-api outage"?

    Give a concise evidence-based answer.
    """;


// -------------------------------------------------------------------
// Configuration
// -------------------------------------------------------------------

string? apiKey =
    Environment.GetEnvironmentVariable(
        "OPENROUTER_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine(
        "OPENROUTER_API_KEY is not configured.");

    Console.WriteLine();

    Console.WriteLine(
        "The project can build and test without an API key.");

    Console.WriteLine(
        "Run 'dotnet test' to verify the deterministic evidence and tool layers.");

    Console.WriteLine();

    Console.WriteLine(
        "Set OPENROUTER_API_KEY only when you want to run the live agent.");

    return;
}


string modelName =
    Environment.GetEnvironmentVariable(
        "OPENROUTER_MODEL")
    ?? DefaultModel;


// -------------------------------------------------------------------
// Provider
// -------------------------------------------------------------------
//
// Provider-specific configuration stops here.
//
// The rest of the sample works with IChatClient.
// -------------------------------------------------------------------

OpenAIClientOptions clientOptions =
    new()
    {
        Endpoint =
            new Uri(
                "https://openrouter.ai/api/v1")
    };

OpenAIClient openAiClient =
    new(
        new ApiKeyCredential(apiKey),
        clientOptions);

IChatClient chatClient =
    openAiClient
        .GetChatClient(modelName)
        .AsIChatClient();


// -------------------------------------------------------------------
// Deterministic data sources
// -------------------------------------------------------------------

EvidenceStore evidenceStore =
    new();

RunbookStore runbookStore =
    new();


// -------------------------------------------------------------------
// Agent
// -------------------------------------------------------------------

TriageAgentFactory agentFactory =
    new(
        evidenceStore,
        runbookStore);

AIAgent agent =
    agentFactory.Create(
        chatClient);

AgentSession session =
    await agent.CreateSessionAsync();


// -------------------------------------------------------------------
// Header
// -------------------------------------------------------------------

Console.WriteLine(
    "============================================");

Console.WriteLine(
    " Evidence-First Incident Triage Agent");

Console.WriteLine(
    "============================================");

Console.WriteLine();

Console.WriteLine(
    $"Model: {modelName}");

Console.WriteLine(
    $"Agent: {agent.GetType().Name}");

Console.WriteLine(
    $"Session: {session.GetType().Name}");

Console.WriteLine();


// -------------------------------------------------------------------
// Incident assessment
// -------------------------------------------------------------------

Console.WriteLine(
    "=== INCIDENT ASSESSMENT ===");

Console.WriteLine();

await StreamAgentResponseAsync(
    agent,
    session,
    DefaultIncident);


// -------------------------------------------------------------------
// Same-session follow-up
// -------------------------------------------------------------------

Console.WriteLine();
Console.WriteLine();

Console.WriteLine(
    "=== SAME SESSION FOLLOW-UP ===");

Console.WriteLine();

await StreamAgentResponseAsync(
    agent,
    session,
    DefaultFollowUp);

Console.WriteLine();


// -------------------------------------------------------------------
// Streaming helper
// -------------------------------------------------------------------

static async Task StreamAgentResponseAsync(
    AIAgent agent,
    AgentSession session,
    string prompt)
{
    await foreach (
        AgentResponseUpdate update
        in agent.RunStreamingAsync(
            prompt,
            session))
    {
        Console.Write(update);
    }
}