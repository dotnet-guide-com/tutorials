using EfCoreTenantGuardrailsMinimal.Context;
using EfCoreTenantGuardrailsMinimal.Data;
using EfCoreTenantGuardrailsMinimal.Models;
using EfCoreTenantGuardrailsMinimal.Queries;
using EfCoreTenantGuardrailsMinimal.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfCoreTenantGuardrailsMinimal.Tests;

public sealed class TenantGuardrailTests
{
    private static TenantExecutionContext Scope(string tenantId, string actorId, int hour) =>
        new(tenantId, actorId, new DateTimeOffset(2026, 8, 14, hour, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Tenant_filter_isolates_same_local_ids()
    {
        using TenantDatabase db = new();
        CancellationToken ct = TestContext.Current.CancellationToken;

        await using TenantDbContext alpha = db.CreateContext(Scope("alpha", "viewer", 10));
        await using TenantDbContext beta = db.CreateContext(Scope("beta", "viewer", 10));

        List<TenantTask> alphaRows = await TenantQueries.ListVisibleAsync(alpha, ct);
        List<TenantTask> betaRows = await TenantQueries.ListVisibleAsync(beta, ct);

        Assert.Equal("Alpha plan", alphaRows.Single(t => t.Id == 1).Title);
        Assert.Equal("Beta plan", betaRows.Single(t => t.Id == 1).Title);
        Assert.All(alphaRows, t => Assert.Equal("alpha", t.TenantId));
        Assert.DoesNotContain(alphaRows, t => t.TenantId == "beta");

        // Both rows physically coexist in the same database (named-filter proof).
        List<TenantTask> all = await alpha.Tasks
            .IgnoreQueryFilters(
                [TenantDbContext.TenantFilterName, TenantDbContext.SoftDeleteFilterName])
            .AsNoTracking()
            .Where(t => t.Id == 1)
            .ToListAsync(ct);

        Assert.Contains(all, t => t.TenantId == "alpha");
        Assert.Contains(all, t => t.TenantId == "beta");
    }

    [Fact]
    public async Task Named_soft_delete_bypass_preserves_tenant_filter()
    {
        using TenantDatabase db = new();
        CancellationToken ct = TestContext.Current.CancellationToken;

        await using TenantDbContext alpha = db.CreateContext(Scope("alpha", "viewer", 10));

        var normal = await TenantQueries.ListVisibleAsync(alpha, ct);
        Assert.Single(normal);

        var withDeleted = await TenantQueries.ListIncludingDeletedAsync(alpha, ct);
        Assert.Equal(2, withDeleted.Count);
        Assert.All(withDeleted, t => Assert.Equal("alpha", t.TenantId));
        Assert.Single(withDeleted, t => t.IsDeleted);
    }

    [Fact]
    public async Task Added_entity_overwrites_spoofed_tenant_and_stamps_audit()
    {
        using TenantDatabase db = new();
        CancellationToken ct = TestContext.Current.CancellationToken;
        DateTimeOffset fixedTime = new(2026, 8, 14, 11, 0, 0, TimeSpan.Zero);

        await using TenantDbContext alpha =
            db.CreateContext(new TenantExecutionContext("alpha", "alice", fixedTime));

        TenantTask entity = new()
        {
            Id = 3,
            TenantId = "beta",   // spoofed caller input
            Title = "Spoofed"
        };
        alpha.Tasks.Add(entity);
        await alpha.SaveChangesAsync(ct);

        Assert.Equal("alpha", entity.TenantId);
        Assert.Equal("alice", entity.CreatedBy);
        Assert.Equal("alice", entity.UpdatedBy);
        Assert.Equal(fixedTime, entity.CreatedAt);
        Assert.Equal(fixedTime, entity.UpdatedAt);
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public async Task Soft_delete_preserves_row_and_stamps_deletion()
    {
        using TenantDatabase db = new();
        CancellationToken ct = TestContext.Current.CancellationToken;
        DateTimeOffset fixedTime = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

        await using TenantDbContext alpha =
            db.CreateContext(new TenantExecutionContext("alpha", "deleter", fixedTime));

        List<TenantTask> targetRows = await alpha.Tasks
            .AsNoTracking()
            .Where(t => t.Id == 1)
            .ToListAsync(ct);
        TenantTask target = targetRows.Single();
        alpha.Remove(target);
        await alpha.SaveChangesAsync(ct);

        var normal = await TenantQueries.ListVisibleAsync(alpha, ct);
        Assert.DoesNotContain(normal, t => t.Id == 1);

        var withDeleted = await TenantQueries.ListIncludingDeletedAsync(alpha, ct);
        TenantTask deleted = withDeleted.Single(t => t.Id == 1);
        Assert.True(deleted.IsDeleted);
        Assert.Equal("deleter", deleted.DeletedBy);
        Assert.Equal(fixedTime, deleted.DeletedAt);
        Assert.Equal(2, withDeleted.Count);
    }

    [Fact]
    public async Task Modified_entity_preserves_creation_fields_and_stamps_update()
    {
        using TenantDatabase db = new();
        CancellationToken ct = TestContext.Current.CancellationToken;
        DateTimeOffset updateTime = new(2026, 8, 14, 14, 0, 0, TimeSpan.Zero);

        await using TenantDbContext writer =
            db.CreateContext(new TenantExecutionContext("alpha", "bob", updateTime));

        TenantTask task = (await ((IQueryable<TenantTask>)writer.Tasks)
            .Where(t => t.Id == 1)
            .ToListAsync(ct)).Single();
        task.Title = "Renamed alpha plan";
        await writer.SaveChangesAsync(ct);

        Assert.Equal(TenantDatabase.SeedCreatedAt, task.CreatedAt);
        Assert.Equal(TenantDatabase.SeedActor, task.CreatedBy);
        Assert.Equal(updateTime, task.UpdatedAt);
        Assert.Equal("bob", task.UpdatedBy);
        Assert.False(task.IsDeleted);
    }

    [Fact]
    public async Task Cross_tenant_detached_update_is_rejected()
    {
        using TenantDatabase db = new();
        CancellationToken ct = TestContext.Current.CancellationToken;

        await using TenantDbContext alpha = db.CreateContext(Scope("alpha", "bob", 14));

        TenantTask detached = new()
        {
            TenantId = "beta",
            Id = 1,
            Title = "Hijacked"
        };
        alpha.Tasks.Attach(detached);
        alpha.Entry(detached).State = EntityState.Modified;
        detached.Title = "Hijacked beta row";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => alpha.SaveChangesAsync(ct));
        Assert.Equal("Cross-tenant mutation is not allowed.", ex.Message);

        await using TenantDbContext beta = db.CreateContext(Scope("beta", "viewer", 10));
        List<TenantTask> betaRows = await beta.Tasks
            .Where(t => t.Id == 1)
            .ToListAsync(ct);
        TenantTask betaRow = betaRows.Single();
        Assert.Equal("Beta plan", betaRow.Title);
    }

    [Fact]
    public void Missing_or_blank_tenant_scope_is_rejected()
    {
        DateTimeOffset now = new(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(
            () => new TenantExecutionContext("", "alice", now));
        Assert.Throws<ArgumentException>(
            () => new TenantExecutionContext("   ", "alice", now));
    }

    [Fact]
    public async Task Workflow_returns_deterministic_summary()
    {
        WorkflowSummary summary =
            await TenantWorkflow.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, summary.VisibleAlpha);
        Assert.Equal(1, summary.VisibleBeta);
        Assert.Equal("Alpha plan", summary.SameIdAlphaTitle);
        Assert.Equal("Beta plan", summary.SameIdBetaTitle);
        Assert.Equal(2, summary.AlphaIncludingDeletedCount);
        Assert.Equal(0, summary.ForeignTenantsUnderBypass);
        Assert.Equal("alpha", summary.StampedTenantAfterSpoof);
        Assert.Equal(1, summary.VisibleAlphaAfterSoftDelete);
        Assert.Equal(3, summary.PhysicalAlphaAfterSoftDelete);
        Assert.Equal("alpha-admin", summary.SoftDeleteActor);
    }
}
