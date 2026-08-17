using EfCoreProviderContrastMinimal.Services;

ProviderContrastResult result =
    await ProviderContrast
        .RunAsync();

Console.WriteLine(
    "EF Core Test Provider Contrast Lab");

Console.WriteLine(
    $"Unique index: InMemory="
    + $"{(result.UniqueConstraint.InMemoryAccepted ? "accepted" : "rejected")} | "
    + $"SQLite="
    + $"{(result.UniqueConstraint.SqliteRejected ? "rejected" : "accepted")}");

Console.WriteLine(
    $"Foreign key: InMemory="
    + $"{(result.ForeignKey.InMemoryAccepted ? "accepted" : "rejected")} | "
    + $"SQLite="
    + $"{(result.ForeignKey.SqliteRejected ? "rejected" : "accepted")}");

Console.WriteLine(
    $"Transaction: InMemory="
    + $"{(result.Transaction.InMemoryUnsupported ? "unsupported" : "supported")} | "
    + $"SQLite="
    + $"{(result.Transaction.SqliteRolledBack ? "rollback-ok" : "rollback-failed")}");

Console.WriteLine(
    $"Database cascade: InMemory="
    + $"{(result.Cascade.InMemoryOrphanRemains ? "orphan-remains" : "child-removed")} | "
    + $"SQLite="
    + $"{(result.Cascade.SqliteChildRemoved ? "child-removed" : "orphan-remains")}");

Console.WriteLine(
    "Guidance: relational behavior -> production provider when practical; SQLite remains a test double.");
