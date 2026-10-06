using System.Text.Json;

namespace FirstMcpServer.Stores;

internal static class JsonStoreLoader
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static IReadOnlyList<T> Load<T>(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Data", fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required deterministic data file was not found: {path}", path);
        }

        using FileStream stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<List<T>>(stream, Options) ?? [];
    }
}
