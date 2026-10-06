using System.Text.Json;

namespace FirstMcpServer.Tools;

internal static class ToolJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, Options);
}
