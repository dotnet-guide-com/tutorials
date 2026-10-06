using FirstMcpServer.Models;

namespace FirstMcpServer.Stores;

public sealed class DeploymentStore
{
    private readonly IReadOnlyList<DeploymentRecord> _records;

    public DeploymentStore()
        : this(JsonStoreLoader.Load<DeploymentRecord>("deployments.json"))
    {
    }

    private DeploymentStore(IReadOnlyList<DeploymentRecord> records)
    {
        _records = records;
    }

    public static DeploymentStore FromRecords(IEnumerable<DeploymentRecord> records)
        => new(records.ToArray());

    public DeploymentRecord? GetRecent(string serviceName)
    {
        string normalized = Normalize(serviceName);

        return _records
            .Where(x => string.Equals(x.Service, normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CompletedAt, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static string Normalize(string value)
        => value?.Trim() ?? string.Empty;
}
