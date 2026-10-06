namespace FirstMcpServer.Models;

public sealed record RunbookRecord
{
    public string RunbookId { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<string> Guidance { get; init; } = [];
    public string Caution { get; init; } = string.Empty;
}
