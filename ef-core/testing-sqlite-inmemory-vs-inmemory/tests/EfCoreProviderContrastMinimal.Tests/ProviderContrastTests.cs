using EfCoreProviderContrastMinimal.Services;

namespace EfCoreProviderContrastMinimal.Tests;

public sealed class
    ProviderContrastTests
{
    [Fact]
    public async Task
        Sqlite_rejects_duplicate_unique_key()
    {
        ConstraintContrast result =
            await ProviderContrast
                .CheckUniqueConstraintAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.True(
            result.SqliteRejected);
    }

    [Fact]
    public async Task
        InMemory_accepts_duplicate_unique_key()
    {
        ConstraintContrast result =
            await ProviderContrast
                .CheckUniqueConstraintAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.True(
            result.InMemoryAccepted);
    }

    [Fact]
    public async Task
        Sqlite_rejects_orphan_foreign_key()
    {
        ConstraintContrast result =
            await ProviderContrast
                .CheckForeignKeyAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.True(
            result.SqliteRejected);
    }

    [Fact]
    public async Task
        InMemory_accepts_orphan_foreign_key()
    {
        ConstraintContrast result =
            await ProviderContrast
                .CheckForeignKeyAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.True(
            result.InMemoryAccepted);
    }

    [Fact]
    public async Task
        Sqlite_transaction_rolls_back()
    {
        TransactionContrast result =
            await ProviderContrast
                .CheckTransactionAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.True(
            result.SqliteRolledBack);
    }

    [Fact]
    public async Task
        InMemory_transaction_is_unsupported_by_default()
    {
        TransactionContrast result =
            await ProviderContrast
                .CheckTransactionAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.True(
            result.InMemoryUnsupported);
    }

    [Fact]
    public async Task
        Sqlite_database_cascade_removes_untracked_child()
    {
        CascadeContrast result =
            await ProviderContrast
                .CheckCascadeAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.True(
            result.SqliteChildRemoved);
    }

    [Fact]
    public async Task
        InMemory_does_not_apply_database_cascade_to_untracked_child()
    {
        CascadeContrast result =
            await ProviderContrast
                .CheckCascadeAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.True(
            result.InMemoryOrphanRemains);
    }
}