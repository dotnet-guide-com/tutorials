using FirstMcpServer.Stores;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace FirstMcpServer.Tools;

[McpServerToolType]
public static class RunbookTools
{
    [McpServerTool(
        Name = "get_runbook",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns deterministic runbook guidance for a known service. Guidance is not incident evidence.")]
    public static string GetRunbook(
        RunbookStore store,
        [Description("Service name, for example checkout-api.")] string serviceName)
    {
        string normalized = serviceName?.Trim() ?? string.Empty;
        var record = store.Get(normalized);

        return record is null
            ? ToolJson.Serialize(new { found = false, service = normalized })
            : ToolJson.Serialize(record);
    }
}
