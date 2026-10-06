using FirstMcpServer.Stores;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace FirstMcpServer.Tools;

[McpServerToolType]
public static class ServiceHealthTools
{
    [McpServerTool(
        Name = "get_service_health",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns deterministic synthetic service-health evidence for a known service.")]
    public static string GetServiceHealth(
        ServiceHealthStore store,
        [Description("Service name, for example checkout-api.")] string serviceName)
    {
        string normalized = serviceName?.Trim() ?? string.Empty;
        var record = store.Get(normalized);

        return record is null
            ? ToolJson.Serialize(new { found = false, service = normalized })
            : ToolJson.Serialize(record);
    }
}
