namespace HighPerformanceLogIngestor.Models;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    byte Level,
    int EventId,
    string Message);