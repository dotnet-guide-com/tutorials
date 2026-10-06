using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

static void Check(bool condition, string key, string detail)
{
    if (!condition)
    {
        Console.WriteLine($"VERIFY|{key}|FAIL|{detail}");
        throw new InvalidOperationException($"Verification failed: {key}: {detail}");
    }

    Console.WriteLine($"VERIFY|{key}|PASS|{detail}");
}

static string TextOf(CallToolResult result)
    => result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: McpClientDemo <path-to-FirstMcpServer.dll>");
    return 2;
}

string serverDll = Path.GetFullPath(args[0]);
Check(File.Exists(serverDll), "SERVER_DLL", serverDll);

var safeEnvironment = StdioClientTransportOptions.GetDefaultEnvironmentVariables();

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "DOTNET-GUIDE-First-MCP-Server",
    Command = "dotnet",
    Arguments = [serverDll],
    WorkingDirectory = Path.GetDirectoryName(serverDll),
    InheritEnvironmentVariables = false,
    EnvironmentVariables = safeEnvironment,
    ShutdownTimeout = TimeSpan.FromSeconds(10),
    StandardErrorLines = line => Console.WriteLine($"SERVER_STDERR|{line}")
});

Console.WriteLine($"RUNTIME|TransportType|{transport.GetType().FullName}");

await using McpClient client = await McpClient.CreateAsync(transport);
Console.WriteLine($"RUNTIME|ClientType|{client.GetType().FullName}");

IList<McpClientTool> tools = await client.ListToolsAsync();
Console.WriteLine($"RUNTIME|ToolCount|{tools.Count}");

foreach (var tool in tools.OrderBy(t => t.Name, StringComparer.Ordinal))
{
    Console.WriteLine($"TOOL|{tool.Name}|{tool.GetType().FullName}|{tool.Description}");
}

string[] expectedTools =
[
    "get_service_health",
    "get_recent_deployment",
    "get_runbook"
];

string[] discovered = tools.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

Check(
    tools.Count == expectedTools.Length &&
    expectedTools.All(name => tools.Any(t => string.Equals(t.Name, name, StringComparison.Ordinal))),
    "TOOL_DISCOVERY",
    string.Join(",", discovered));

Check(
    typeof(AIFunction).IsAssignableFrom(typeof(McpClientTool)),
    "AIFUNCTION_ASSIGNABILITY",
    "McpClientTool derives from AIFunction");

CallToolResult healthResult = await client.CallToolAsync(
    "get_service_health",
    new Dictionary<string, object?> { ["serviceName"] = "checkout-api" },
    cancellationToken: CancellationToken.None);
string healthText = TextOf(healthResult);
Check(
    healthResult.IsError is not true &&
    healthText.Contains("\"evidenceId\":\"E-101\"", StringComparison.Ordinal) &&
    healthText.Contains("\"status\":\"degraded\"", StringComparison.Ordinal) &&
    healthText.Contains("\"errorRate\":18.7", StringComparison.Ordinal),
    "SERVICE_HEALTH_CALL",
    healthText);

CallToolResult deploymentResult = await client.CallToolAsync(
    "get_recent_deployment",
    new Dictionary<string, object?> { ["serviceName"] = "checkout-api" },
    cancellationToken: CancellationToken.None);
string deploymentText = TextOf(deploymentResult);
Check(
    deploymentResult.IsError is not true &&
    deploymentText.Contains("\"evidenceId\":\"E-201\"", StringComparison.Ordinal) &&
    deploymentText.Contains("\"deployment\":\"deploy-1842\"", StringComparison.Ordinal) &&
    deploymentText.Contains("\"previousDeployment\":\"deploy-1839\"", StringComparison.Ordinal),
    "DEPLOYMENT_CALL",
    deploymentText);

CallToolResult runbookResult = await client.CallToolAsync(
    "get_runbook",
    new Dictionary<string, object?> { ["serviceName"] = "checkout-api" },
    cancellationToken: CancellationToken.None);
string runbookText = TextOf(runbookResult);
Check(
    runbookResult.IsError is not true &&
    runbookText.Contains("\"runbookId\":\"RB-CHECKOUT-03\"", StringComparison.Ordinal) &&
    runbookText.Contains("does not establish root cause", StringComparison.Ordinal),
    "RUNBOOK_CALL",
    runbookText);

bool unknownRejected = false;
string unknownDetail = string.Empty;

try
{
    CallToolResult unknown = await client.CallToolAsync(
        "does_not_exist",
        new Dictionary<string, object?>(),
        cancellationToken: CancellationToken.None);

    if (unknown.IsError is true)
    {
        unknownRejected = true;
        unknownDetail = TextOf(unknown);
    }
    else
    {
        unknownDetail = "Unknown tool unexpectedly returned a non-error result.";
    }
}
catch (Exception ex)
{
    unknownRejected = true;
    unknownDetail = $"{ex.GetType().Name}: {ex.Message}";
}

Check(unknownRejected, "UNKNOWN_TOOL_REJECTION", unknownDetail);

Console.WriteLine("VERIFY|STDIO_END_TO_END|PASS|Client launched server, discovered exactly three tools, and invoked all three over MCP stdio");
Console.WriteLine("FINAL|PASS");
return 0;
