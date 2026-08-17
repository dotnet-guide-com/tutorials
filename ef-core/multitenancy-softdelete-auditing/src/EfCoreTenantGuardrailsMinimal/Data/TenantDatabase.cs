using EfCoreTenantGuardrailsMinimal.Context;
using EfCoreTenantGuardrailsMinimal.Interceptors;
using EfCoreTenantGuardrailsMinimal.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfCoreTenantGuardrailsMinimal.Data;

public sealed class TenantDatabase : IDisposable
{
    public static DateTimeOffset SeedCreatedAt { get; } = new(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);
    public static DateTimeOffset SeedDeletedAt { get; } = new(2026, 8, 14, 9, 0, 0, TimeSpan.Zero);
    public const string SeedActor = "seeder";

    private readonly SqliteConnection _connection;

    public TenantDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SeedAsync().GetAwaiter().GetResult();
    }

    public TenantDbContext CreateContext(TenantExecutionContext executionContext)
    {
        DbContextOptions<TenantDbContext> options =
            new DbContextOptionsBuilder<TenantDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(new TenantAuditInterceptor(executionContext))
                .Options;

        return new TenantDbContext(options, executionContext);
    }

    private async Task SeedAsync()
    {
        await using (TenantDbContext ensure = CreateContext(SeedScope("alpha")))
        {
            await ensure.Database.EnsureCreatedAsync();
        }

        await using (TenantDbContext alpha = CreateContext(SeedScope("alpha", SeedCreatedAt)))
        {
            alpha.Tasks.Add(SeedTask(1, "Alpha plan"));
            alpha.Tasks.Add(SeedTask(2, "Alpha archive"));
            await alpha.SaveChangesAsync();
        }

        await SoftDeleteRow("alpha", 2, SeedDeletedAt);

        await using (TenantDbContext beta = CreateContext(SeedScope("beta", SeedCreatedAt)))
        {
            beta.Tasks.Add(SeedTask(1, "Beta plan"));
            beta.Tasks.Add(SeedTask(2, "Beta archive"));
            await beta.SaveChangesAsync();
        }

        await SoftDeleteRow("beta", 2, SeedDeletedAt);
    }

    private TenantExecutionContext SeedScope(string tenantId) =>
        new(tenantId, SeedActor, SeedCreatedAt);

    private TenantExecutionContext SeedScope(string tenantId, DateTimeOffset time) =>
        new(tenantId, SeedActor, time);

    private static TenantTask SeedTask(int id, string title) =>
        new() { Id = id, Title = title };

    private async Task SoftDeleteRow(string tenantId, int id, DateTimeOffset deletedAt)
    {
        await using (TenantDbContext context =
                     CreateContext(new TenantExecutionContext(tenantId, SeedActor, deletedAt)))
        {
            TenantTask? row = await context.Tasks
                .SingleOrDefaultAsync(t => t.Id == id);
            if (row is not null)
            {
                context.Remove(row);
                await context.SaveChangesAsync();
            }
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
