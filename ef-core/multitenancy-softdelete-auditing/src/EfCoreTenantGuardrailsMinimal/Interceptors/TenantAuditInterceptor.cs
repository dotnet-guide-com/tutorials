using EfCoreTenantGuardrailsMinimal.Context;
using EfCoreTenantGuardrailsMinimal.Data;
using EfCoreTenantGuardrailsMinimal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreTenantGuardrailsMinimal.Interceptors;

public sealed class TenantAuditInterceptor(
    TenantExecutionContext executionContext)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Apply(DbContext? dbContext)
    {
        if (dbContext is not TenantDbContext context)
            return;

        context.ChangeTracker.DetectChanges();

        foreach (EntityEntry<TenantTask> entry in
                 context.ChangeTracker.Entries<TenantTask>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    StampAdded(entry);
                    break;

                case EntityState.Modified:
                    GuardTenant(entry);
                    StampModified(entry);
                    break;

                case EntityState.Deleted:
                    GuardTenant(entry);
                    ConvertToSoftDelete(entry);
                    break;
            }
        }
    }

    private void StampAdded(EntityEntry<TenantTask> entry)
    {
        // Caller input never chooses the tenant partition.
        entry.Entity.TenantId = executionContext.TenantId;
        entry.Entity.IsDeleted = false;
        entry.Entity.DeletedAt = null;
        entry.Entity.DeletedBy = null;

        entry.Entity.CreatedAt = executionContext.UtcNow;
        entry.Entity.CreatedBy = executionContext.ActorId;
        entry.Entity.UpdatedAt = executionContext.UtcNow;
        entry.Entity.UpdatedBy = executionContext.ActorId;
    }

    private void GuardTenant(EntityEntry<TenantTask> entry)
    {
        if (!string.Equals(
                entry.Entity.TenantId,
                executionContext.TenantId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Cross-tenant mutation is not allowed.");
        }
    }

    private void StampModified(EntityEntry<TenantTask> entry)
    {
        entry.Property(task => task.TenantId).IsModified = false;
        entry.Property(task => task.CreatedAt).IsModified = false;
        entry.Property(task => task.CreatedBy).IsModified = false;

        // Normal modifications cannot impersonate the soft-delete path.
        Reset(entry, task => task.IsDeleted);
        Reset(entry, task => task.DeletedAt);
        Reset(entry, task => task.DeletedBy);

        entry.Entity.UpdatedAt = executionContext.UtcNow;
        entry.Entity.UpdatedBy = executionContext.ActorId;
    }

    private static void Reset<TProperty>(
        EntityEntry<TenantTask> entry,
        System.Linq.Expressions.Expression<Func<TenantTask, TProperty>> property)
    {
        PropertyEntry<TenantTask, TProperty> value = entry.Property(property);
        value.CurrentValue = value.OriginalValue;
        value.IsModified = false;
    }

    private void ConvertToSoftDelete(EntityEntry<TenantTask> entry)
    {
        // Avoid turning the entire row into a broad UPDATE.
        entry.State = EntityState.Unchanged;

        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedAt = executionContext.UtcNow;
        entry.Entity.DeletedBy = executionContext.ActorId;

        entry.Property(task => task.IsDeleted).IsModified = true;
        entry.Property(task => task.DeletedAt).IsModified = true;
        entry.Property(task => task.DeletedBy).IsModified = true;
    }
}
