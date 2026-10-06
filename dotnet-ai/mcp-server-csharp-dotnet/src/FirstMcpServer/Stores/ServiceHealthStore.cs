using FirstMcpServer.Models;

namespace FirstMcpServer.Stores;

public sealed class ServiceHealthStore
{
    private readonly IReadOnlyList<ServiceHealthRecord> _records;

    public ServiceHealthStore()
        : this(JsonStoreLoader.Load<ServiceHealthRecord>("services.json"))
    {
    }

    private ServiceHealthStore(IReadOnlyList<ServiceHealthRecord> records)
    {
        _records = records;
    }

    public static ServiceHealthStore FromRecords(IEnumerable<ServiceHealthRecord> records)
        => new(records.ToArray());

    public ServiceHealthRecord? Get(string serviceName)
    {
        string normalized = Normalize(serviceName);

        return _records
            .Where(x => string.Equals(x.Service, normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.ObservedAt, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static string Normalize(string value)
        => value?.Trim() ?? string.Empty;
}
