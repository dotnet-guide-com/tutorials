namespace FirstMcpServer.Models;

public sealed record DeploymentRecord
{
    public string EvidenceId { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Deployment { get; init; } = string.Empty;
    public string DeploymentStatus { get; init; } = string.Empty;
    public string CompletedAt { get; init; } = string.Empty;
    public string PreviousDeployment { get; init; } = string.Empty;
    public string ChangeSummary { get; init; } = string.Empty;
    public bool RollbackPerformed { get; init; }
}
