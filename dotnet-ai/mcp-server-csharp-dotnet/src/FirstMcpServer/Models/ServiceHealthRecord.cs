namespace FirstMcpServer.Models;

public sealed record ServiceHealthRecord
{
    public string EvidenceId { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string ObservedAt { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double ErrorRate { get; init; }
    public string ErrorSpikeStarted { get; init; } = string.Empty;
    public double DatabasePoolUtilization { get; init; }
}
