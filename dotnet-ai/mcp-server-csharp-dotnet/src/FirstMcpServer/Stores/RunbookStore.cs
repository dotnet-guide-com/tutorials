using FirstMcpServer.Models;

namespace FirstMcpServer.Stores;

public sealed class RunbookStore
{
    private readonly IReadOnlyList<RunbookRecord> _records;

    public RunbookStore()
        : this(JsonStoreLoader.Load<RunbookRecord>("runbooks.json"))
    {
    }

    private RunbookStore(IReadOnlyList<RunbookRecord> records)
    {
        _records = records;
    }

    public static RunbookStore FromRecords(IEnumerable<RunbookRecord> records)
        => new(records.ToArray());

    public RunbookRecord? Get(string serviceName)
    {
        string normalized = Normalize(serviceName);

        return _records.FirstOrDefault(
            x => string.Equals(x.Service, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value)
        => value?.Trim() ?? string.Empty;
}
