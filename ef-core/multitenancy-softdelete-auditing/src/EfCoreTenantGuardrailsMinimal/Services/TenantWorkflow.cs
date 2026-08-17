using EfCoreTenantGuardrailsMinimal.Context;
using EfCoreTenantGuardrailsMinimal.Data;
using EfCoreTenantGuardrailsMinimal.Models;
using EfCoreTenantGuardrailsMinimal.Queries;
using Microsoft.EntityFrameworkCore;

namespace EfCoreTenantGuardrailsMinimal.Services;

public sealed record WorkflowSummary(
    int VisibleAlpha,
    int VisibleBeta,
    string SameIdAlphaTitle,
    string SameIdBetaTitle,
    int AlphaIncludingDeletedCount,
    int ForeignTenantsUnderBypass,
    string StampedTenantAfterSpoof,
    int VisibleAlphaAfterSoftDelete,
    int PhysicalAlphaAfterSoftDelete,
    string? SoftDeleteActor);

public static class TenantWorkflow
{
    private static DateTimeOffset Fixed(int hour) =>
        new(2026, 8, 14, hour, 0, 0, TimeSpan.Zero);

    public static async Task<WorkflowSummary> RunAsync(
        CancellationToken cancellationToken = default)
    {
        using TenantDatabase database = new();

        TenantExecutionContext alphaView = new("alpha", "viewer", Fixed(10));
        TenantExecutionContext betaView = new("beta", "viewer", Fixed(10));

        List<TenantTask> visibleAlpha;
        List<TenantTask> visibleBeta;
        List<TenantTask> alphaIncludingDeleted;

        await using (TenantDbContext alpha = database.CreateContext(alphaView))
        {
            visibleAlpha = await TenantQueries.ListVisibleAsync(alpha, cancellationToken);
            alphaIncludingDeleted =
                await TenantQueries.ListIncludingDeletedAsync(alpha, cancellationToken);
        }

        await using (TenantDbContext beta = database.CreateContext(betaView))
        {
            visibleBeta = await TenantQueries.ListVisibleAsync(beta, cancellationToken);
        }

        string spoofedTenant;
        await using (TenantDbContext alpha =
                     database.CreateContext(new TenantExecutionContext("alpha", "alice", Fixed(11))))
        {
            TenantTask spoofed = new() { Id = 3, TenantId = "beta", Title = "Spoofed" };
            alpha.Tasks.Add(spoofed);
            await alpha.SaveChangesAsync(cancellationToken);
            spoofedTenant = spoofed.TenantId;
        }

        await using (TenantDbContext alpha =
                     database.CreateContext(new TenantExecutionContext("alpha", "alpha-admin", Fixed(12))))
        {
            List<TenantTask> matches = await alpha.Tasks
                .AsNoTracking()
                .Where(t => t.Id == 1)
                .ToListAsync(cancellationToken);
            TenantTask target = matches.Single();
            alpha.Remove(target);
            await alpha.SaveChangesAsync(cancellationToken);
        }

        int visibleAfter;
        int physicalAfter;
        string? softDeleteActor;
        await using (TenantDbContext alpha = database.CreateContext(alphaView))
        {
            List<TenantTask> afterVisible =
                await TenantQueries.ListVisibleAsync(alpha, cancellationToken);
            List<TenantTask> afterDeleted =
                await TenantQueries.ListIncludingDeletedAsync(alpha, cancellationToken);

            visibleAfter = afterVisible.Count;
            physicalAfter = afterDeleted.Count;
            softDeleteActor = afterDeleted.Single(t => t.Id == 1).DeletedBy;
        }

        return new WorkflowSummary(
            visibleAlpha.Count,
            visibleBeta.Count,
            visibleAlpha.Single(t => t.Id == 1).Title,
            visibleBeta.Single(t => t.Id == 1).Title,
            alphaIncludingDeleted.Count,
            alphaIncludingDeleted.Count(t => t.TenantId != "alpha"),
            spoofedTenant,
            visibleAfter,
            physicalAfter,
            softDeleteActor);
    }
}
