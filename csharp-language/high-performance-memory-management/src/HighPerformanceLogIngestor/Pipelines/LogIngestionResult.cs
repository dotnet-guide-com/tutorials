namespace HighPerformanceLogIngestor.Pipelines;

public sealed record LogIngestionResult(
    int TotalLines,
    int ValidLines,
    int InvalidLines,
    int PooledCopies);