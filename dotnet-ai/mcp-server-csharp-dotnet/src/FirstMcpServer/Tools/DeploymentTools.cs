using FirstMcpServer.Stores;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace FirstMcpServer.Tools;

[McpServerToolType]
public static class DeploymentTools
{
    [McpServerTool(
        Name = "get_recent_deployment",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns deterministic synthetic evidence for the most recent known deployment of a service.")]
    public static string GetRecentDeployment(
        DeploymentStore store,
        [Description("Service name, for example checkout-api.")] string serviceName)
    {
        string normalized = serviceName?.Trim() ?? string.Empty;
        var record = store.GetRecent(normalized);

        return record is null
            ? ToolJson.Serialize(new { found = false, service = normalized })
            : ToolJson.Serialize(record);
    }
}
